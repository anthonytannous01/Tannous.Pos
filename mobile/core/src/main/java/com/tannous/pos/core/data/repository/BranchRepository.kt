package com.tannous.pos.core.data.repository

import com.tannous.pos.core.data.model.BranchDto
import com.tannous.pos.core.data.remote.BranchService
import timber.log.Timber
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class BranchRepository @Inject constructor(
    private val branchService: BranchService
) {
    /** In-memory cache for the current session. Refreshed on first load or explicit refresh. */
    private var cachedBranches: List<BranchDto> = emptyList()

    suspend fun getBranches(forceRefresh: Boolean = false): List<BranchDto> {
        if (!forceRefresh && cachedBranches.isNotEmpty()) return cachedBranches
        return try {
            branchService.getBranches(activeOnly = true).also { cachedBranches = it }
        } catch (e: Exception) {
            Timber.w(e, "Failed to load branches")
            cachedBranches // return stale cache on error
        }
    }

    fun getDefaultBranch(): BranchDto? = cachedBranches.firstOrNull { it.isDefault }
        ?: cachedBranches.firstOrNull()

    fun invalidateCache() { cachedBranches = emptyList() }
}
