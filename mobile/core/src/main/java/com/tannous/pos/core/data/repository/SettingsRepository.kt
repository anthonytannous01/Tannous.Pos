package com.tannous.pos.core.data.repository

import com.tannous.pos.core.data.local.dao.KeyValueDao
import com.tannous.pos.core.data.local.entity.KeyValueEntity
import com.tannous.pos.core.data.model.BusinessSettingsDto
import com.tannous.pos.core.data.remote.SettingsService
import timber.log.Timber
import java.io.IOException
import java.math.BigDecimal
import javax.inject.Inject
import javax.inject.Singleton

/**
 * Provides business settings (tax rate, currency) with an offline-first cache backed by the
 * key_value table. Display surfaces (sell/receipt) read the cached tax rate and currency so they
 * never make a blocking network call mid-sale; the server total remains authoritative on finalize.
 */
@Singleton
class SettingsRepository @Inject constructor(
    private val settingsService: SettingsService,
    private val keyValueDao: KeyValueDao
) {

    /**
     * Fetches settings from the server and caches tax rate + currency. On network failure,
     * returns a synthetic DTO built from the cached values (or safe defaults). Never blocks
     * a caller into a crash: any failure degrades to cached/default values.
     */
    suspend fun getSettings(): BusinessSettingsDto {
        return try {
            val dto = settingsService.getSettings()
            keyValueDao.set(KeyValueEntity(KEY_TAX_RATE, dto.taxRate.toPlainString()))
            keyValueDao.set(KeyValueEntity(KEY_CURRENCY, dto.currency))
            dto
        } catch (e: IOException) {
            Timber.w(e, "Offline fetching settings; serving cached/default values")
            syntheticFromCache()
        } catch (e: Exception) {
            Timber.e(e, "Error fetching settings; serving cached/default values")
            syntheticFromCache()
        }
    }

    /** Cached tax rate as a fraction (e.g. 0.10). Defaults to [DEFAULT_TAX_RATE] if absent. */
    suspend fun getTaxRate(): BigDecimal {
        return try {
            keyValueDao.get(KEY_TAX_RATE)?.let { BigDecimal(it) } ?: DEFAULT_TAX_RATE
        } catch (e: Exception) {
            Timber.e(e, "Error reading cached tax rate")
            DEFAULT_TAX_RATE
        }
    }

    /** Cached ISO 4217 currency code (e.g. "USD", "LBP"). Defaults to [DEFAULT_CURRENCY]. */
    suspend fun getCurrency(): String {
        return try {
            keyValueDao.get(KEY_CURRENCY) ?: DEFAULT_CURRENCY
        } catch (e: Exception) {
            Timber.e(e, "Error reading cached currency")
            DEFAULT_CURRENCY
        }
    }

    private suspend fun syntheticFromCache(): BusinessSettingsDto {
        return BusinessSettingsDto(
            id = "",
            businessName = "",
            address = null,
            phone = null,
            email = null,
            website = null,
            taxNumber = null,
            taxRate = getTaxRate(),
            currency = getCurrency(),
            receiptHeader = null,
            receiptFooter = null,
            requireCustomerInfo = false,
            enableInventoryTracking = false,
            enableRecipeManagement = false
        )
    }

    companion object {
        const val KEY_TAX_RATE = "settings_tax_rate"
        const val KEY_CURRENCY = "settings_currency"
        val DEFAULT_TAX_RATE: BigDecimal = BigDecimal("0.10")
        const val DEFAULT_CURRENCY = "USD"
    }
}
