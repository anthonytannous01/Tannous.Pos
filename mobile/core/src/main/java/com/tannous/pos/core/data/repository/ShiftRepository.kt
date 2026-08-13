package com.tannous.pos.core.data.repository

import com.tannous.pos.core.data.local.dao.ShiftDao
import com.tannous.pos.core.data.local.entity.ShiftEntity
import com.tannous.pos.core.data.model.CashDropRequest
import com.tannous.pos.core.data.model.CloseShiftRequest
import com.tannous.pos.core.data.model.OpenShiftRequest
import com.tannous.pos.core.data.model.ShiftDto
import com.tannous.pos.core.data.remote.ShiftService
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.flowOf
import retrofit2.HttpException
import timber.log.Timber
import java.io.IOException
import java.math.BigDecimal
import java.time.Instant
import java.time.format.DateTimeFormatter
import javax.inject.Inject
import javax.inject.Singleton

/**
 * Repository for shift management operations.
 *
 * Read path (getActiveShift) is network-first with a Room fallback so that finalize works offline.
 * Freshness is a safety property: a shift closed on another terminal must not keep accepting sales,
 * so we never trust the cache while online. The cache can only serve a shift that was open at last
 * successful contact, and close/404 write-through removes it so a known-closed shift is never served.
 *
 * Write actions (open/cash-drop/close) still require internet connection and are NOT queued offline.
 * If the user is offline, these operations fail with a clear error message.
 */
@Singleton
class ShiftRepository @Inject constructor(
    private val shiftService: ShiftService,
    private val shiftDao: ShiftDao
) {
    
    /**
     * Gets the current active shift for the logged-in user.
     *
     * Network-first: on success the shift is written through to Room and returned.
     * On 404 the cached active shift is cleared and null is returned.
     * On network failure (offline) the last-known cached active shift is returned (or null).
     */
    suspend fun getActiveShift(): ShiftDto? {
        return try {
            val shift = shiftService.getCurrentShift()
            Timber.d("Retrieved active shift: ${shift.id}")
            cacheActiveShift(shift)
            shift
        } catch (e: HttpException) {
            if (e.code() == 404) {
                // No active shift found - clear any stale cache so offline fallback can't resurrect it
                Timber.d("No active shift found (404). Clearing cached active shift.")
                clearCachedActiveShift()
                null
            } else {
                Timber.e(e, "Error retrieving active shift: HTTP ${e.code()}")
                throw e
            }
        } catch (e: IOException) {
            // Offline - fall back to the last-known cached active shift
            Timber.w(e, "Network error retrieving active shift. Falling back to cached shift.")
            val cached = try {
                shiftDao.getActiveShift()
            } catch (ex: Exception) {
                Timber.e(ex, "Error reading cached active shift")
                null
            }
            cached?.toDto()
        } catch (e: Exception) {
            Timber.e(e, "Unexpected error retrieving active shift")
            throw e
        }
    }
    
    /**
     * Opens a new shift with the specified opening balance.
     * Requires internet connection - will fail if offline.
     * 
     * @param openingBalance The cash amount at shift start
     * @param notes Optional notes about the shift
     * @return Result with ShiftDto on success, or failure with error message
     */
    suspend fun openShift(
        openingBalance: BigDecimal,
        openingBalanceLbp: BigDecimal = BigDecimal.ZERO,
        notes: String? = null
    ): Result<ShiftDto> {
        return try {
            val request = OpenShiftRequest(
                openingBalance = openingBalance,
                openingBalanceLbp = openingBalanceLbp,
                notes = notes
            )
            val shift = shiftService.openShift(request)
            Timber.d("Shift opened successfully: ${shift.id}")
            cacheActiveShift(shift)
            Result.success(shift)
        } catch (e: IOException) {
            Timber.e(e, "Network error opening shift")
            Result.failure(IOException("Shift actions require internet connection. Please check your network and try again.", e))
        } catch (e: HttpException) {
            Timber.e(e, "HTTP error opening shift: ${e.code()}")
            val errorMessage = try {
                e.response()?.errorBody()?.string() ?: "Failed to open shift"
            } catch (ex: Exception) {
                "Failed to open shift (HTTP ${e.code()})"
            }
            Result.failure(IOException(errorMessage, e))
        } catch (e: Exception) {
            Timber.e(e, "Unexpected error opening shift")
            Result.failure(e)
        }
    }
    
    /**
     * Closes the specified shift with the actual cash count.
     * Requires internet connection - will fail if offline.
     * 
     * @param shiftId The ID of the shift to close
     * @param closingCount The actual cash count at shift end
     * @param note Optional note about the shift closure
     * @return Result with ShiftDto on success, or failure with error message
     */
    suspend fun closeShift(
        shiftId: String,
        closingCount: BigDecimal,
        closingCountLbp: BigDecimal = BigDecimal.ZERO,
        note: String? = null
    ): Result<ShiftDto> {
        return try {
            val request = CloseShiftRequest(
                closingCount = closingCount,
                closingCountLbp = closingCountLbp,
                note = note
            )
            val shift = shiftService.closeShift(shiftId, request)
            Timber.d("Shift closed successfully: ${shift.id}")
            // Remove from cache so the offline fallback can't serve a known-closed shift
            try {
                shiftDao.delete(shiftId)
            } catch (ex: Exception) {
                Timber.e(ex, "Error clearing cached shift after close")
            }
            Result.success(shift)
        } catch (e: IOException) {
            Timber.e(e, "Network error closing shift")
            Result.failure(IOException("Shift actions require internet connection. Please check your network and try again.", e))
        } catch (e: HttpException) {
            Timber.e(e, "HTTP error closing shift: ${e.code()}")
            val errorMessage = try {
                e.response()?.errorBody()?.string() ?: "Failed to close shift"
            } catch (ex: Exception) {
                "Failed to close shift (HTTP ${e.code()})"
            }
            Result.failure(IOException(errorMessage, e))
        } catch (e: Exception) {
            Timber.e(e, "Unexpected error closing shift")
            Result.failure(e)
        }
    }
    
    /**
     * Records a cash drop against the specified shift.
     * Requires internet connection - will fail if offline.
     *
     * The backend cash-drop endpoint returns a cash-drawer event (not a shift), so on success we
     * re-fetch the current shift to obtain fresh balances, write it through to Room, and return it.
     *
     * @param shiftId The ID of the shift to record the drop against
     * @param amount The cash amount removed from the drawer
     * @param note Optional note describing the drop
     * @return Result with the refreshed ShiftDto on success, or failure with error message
     */
    suspend fun cashDrop(
        shiftId: String,
        amount: BigDecimal,
        note: String? = null
    ): Result<ShiftDto> {
        return try {
            val request = CashDropRequest(amount = amount, note = note)
            val response = shiftService.cashDrop(shiftId, request)
            if (!response.isSuccessful) {
                val errorMessage = try {
                    response.errorBody()?.string() ?: "Failed to record cash drop (HTTP ${response.code()})"
                } catch (ex: Exception) {
                    "Failed to record cash drop (HTTP ${response.code()})"
                }
                Timber.e("Cash drop failed: HTTP ${response.code()} - $errorMessage")
                return Result.failure(IOException(errorMessage))
            }

            Timber.d("Cash drop recorded for shift $shiftId. Re-fetching shift.")
            val refreshed = shiftService.getCurrentShift()
            cacheActiveShift(refreshed)
            Result.success(refreshed)
        } catch (e: IOException) {
            Timber.e(e, "Network error recording cash drop")
            Result.failure(IOException("Shift actions require internet connection. Please check your network and try again.", e))
        } catch (e: HttpException) {
            Timber.e(e, "HTTP error recording cash drop: ${e.code()}")
            val errorMessage = try {
                e.response()?.errorBody()?.string() ?: "Failed to record cash drop"
            } catch (ex: Exception) {
                "Failed to record cash drop (HTTP ${e.code()})"
            }
            Result.failure(IOException(errorMessage, e))
        } catch (e: Exception) {
            Timber.e(e, "Unexpected error recording cash drop")
            Result.failure(e)
        }
    }

    /**
     * Legacy method for backward compatibility - returns empty flow.
     * Use getActiveShift() instead.
     */
    fun getAllShifts(): Flow<List<Nothing>> {
        return flowOf(emptyList())
    }
    
    /**
     * Legacy method for backward compatibility - returns null.
     * Use getActiveShift() instead which returns ShiftDto from API.
     */
    suspend fun getShiftById(shiftId: String): Nothing? {
        return null
    }
    
    /**
     * Legacy method for backward compatibility - no-op.
     * Shifts are managed server-side, no local sync needed.
     */
    suspend fun markShiftSynced(shiftId: String) {
        // No-op: shifts are managed server-side
    }

    /**
     * Best-effort write-through of a server shift into Room. Never throws: a cache failure
     * (including a timestamp parse failure) must not break the online path.
     */
    private suspend fun cacheActiveShift(dto: ShiftDto) {
        try {
            shiftDao.insert(dto.toEntity())
        } catch (e: Exception) {
            Timber.e(e, "Error caching active shift ${dto.id}")
        }
    }

    /** Best-effort removal of the cached active shift (soft-delete). Never throws. */
    private suspend fun clearCachedActiveShift() {
        try {
            shiftDao.getActiveShift()?.let { shiftDao.delete(it.id) }
        } catch (e: Exception) {
            Timber.e(e, "Error clearing cached active shift")
        }
    }

    private fun ShiftDto.toEntity(): ShiftEntity {
        val opened = Instant.parse(startTime)
        val closed = endTime?.let { Instant.parse(it) }
        return ShiftEntity(
            id = id,
            shiftNumber = shiftNumber,
            startTime = opened,
            endTime = closed,
            openedAt = opened,
            closedAt = closed,
            status = status,
            openingBalance = openingBalance,
            closingBalance = closingBalance,
            expectedCash = expectedCash,
            actualCash = actualCash,
            variance = cashDifference,
            isDeleted = false,
            deletedAt = null,
            syncedAt = Instant.now()
        )
    }

    /**
     * Reconstructs a ShiftDto from a cached entity for the offline path.
     * userId/createdAt/notes are not persisted locally; they are not read by finalize or the
     * shift UI, so safe defaults are used. LBP drawer figures are also not cached in Room —
     * the offline fallback shows USD only (LBP fields fall back to ShiftDto defaults).
     */
    private fun ShiftEntity.toDto(): ShiftDto {
        return ShiftDto(
            id = id,
            shiftNumber = shiftNumber,
            startTime = startTime.toString(),
            endTime = endTime?.toString(),
            status = status,
            openingBalance = openingBalance,
            closingBalance = closingBalance,
            expectedCash = expectedCash,
            actualCash = actualCash,
            cashDifference = variance,
            notes = null,
            userId = "",
            createdAt = openedAt.toString()
        )
    }
}