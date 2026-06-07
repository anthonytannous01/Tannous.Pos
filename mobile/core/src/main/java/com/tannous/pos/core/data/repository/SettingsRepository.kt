package com.tannous.pos.core.data.repository

import com.tannous.pos.core.data.local.dao.KeyValueDao
import com.tannous.pos.core.data.local.entity.KeyValueEntity
import com.tannous.pos.core.data.model.BusinessSettingsDto
import com.tannous.pos.core.data.model.UpdateSettingsRequest
import com.tannous.pos.core.data.remote.SettingsService
import retrofit2.HttpException
import timber.log.Timber
import java.io.IOException
import java.math.BigDecimal
import java.math.RoundingMode
import javax.inject.Inject
import javax.inject.Singleton

/**
 * Provides business settings (tax rate, currency) with an offline-first cache backed by the
 * key_value table. Display surfaces (sell/receipt) read the cached tax rate and currency so they
 * never make a blocking network call mid-sale; the server total remains authoritative on finalize.
 *
 * Tax rate on the wire and in [KEY_TAX_RATE] is a **percentage** (e.g. 10 for 10%), matching
 * backend [BusinessSettings.TaxRate] and receipt printing. [getTaxRate] returns a decimal fraction
 * for cart math (e.g. 0.10).
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
            cacheSettings(dto)
            dto
        } catch (e: IOException) {
            Timber.w(e, "Offline fetching settings; serving cached/default values")
            syntheticFromCache()
        } catch (e: Exception) {
            Timber.e(e, "Error fetching settings; serving cached/default values")
            syntheticFromCache()
        }
    }

    suspend fun updateSettings(request: UpdateSettingsRequest): Result<BusinessSettingsDto> {
        return try {
            val updated = settingsService.updateSettings(request)
            cacheSettings(updated)
            Result.success(updated)
        } catch (e: HttpException) {
            if (e.code() == 403) {
                Result.failure(SecurityException("Only owners can update settings"))
            } else {
                Result.failure(e)
            }
        } catch (e: IOException) {
            Result.failure(IOException("No connection. Settings not saved."))
        } catch (e: Exception) {
            Timber.e(e, "Error updating settings")
            Result.failure(e)
        }
    }

    /** Cached tax rate as a fraction (e.g. 0.10). Defaults to [DEFAULT_TAX_RATE] if absent. */
    suspend fun getTaxRate(): BigDecimal {
        return try {
            keyValueDao.get(KEY_TAX_RATE)?.let { fractionFromCachedPercent(BigDecimal(it)) }
                ?: DEFAULT_TAX_RATE
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

    /** Cached LBP/USD exchange rate for offline display. Defaults to 0 (rate not configured). */
    suspend fun getExchangeRate(): BigDecimal {
        return try {
            keyValueDao.get(KEY_EXCHANGE_RATE)?.let { BigDecimal(it) } ?: BigDecimal.ZERO
        } catch (e: Exception) {
            BigDecimal.ZERO
        }
    }

    private suspend fun cacheTaxAndCurrency(taxRatePercent: BigDecimal, currency: String) {
        keyValueDao.set(KeyValueEntity(KEY_TAX_RATE, taxRatePercent.toPlainString()))
        keyValueDao.set(KeyValueEntity(KEY_CURRENCY, currency))
    }

    private suspend fun cacheSettings(dto: com.tannous.pos.core.data.model.BusinessSettingsDto) {
        cacheTaxAndCurrency(dto.taxRate, dto.currency)
        keyValueDao.set(KeyValueEntity(KEY_EXCHANGE_RATE, dto.exchangeRateLbpPerUsd.toPlainString()))
    }

    suspend fun getLanguage(): String =
        try { keyValueDao.get(KEY_LANGUAGE) ?: LANG_EN } catch (e: Exception) { LANG_EN }

    suspend fun setLanguage(lang: String) =
        keyValueDao.set(KeyValueEntity(KEY_LANGUAGE, lang))

    fun isArabic(lang: String) = lang == LANG_AR

    private suspend fun syntheticFromCache(): BusinessSettingsDto {
        val percent = percentFromCachedFraction(getTaxRate())
        return BusinessSettingsDto(
            storeName = "",
            taxRate = percent,
            currency = getCurrency(),
            taxEnabled = percent > BigDecimal.ZERO
        )
    }

    companion object {
        const val KEY_TAX_RATE    = "settings_tax_rate"
        const val KEY_CURRENCY    = "settings_currency"
        const val KEY_EXCHANGE_RATE = "settings_exchange_rate"
        const val KEY_LANGUAGE    = "settings_language"
        const val LANG_EN         = "en"
        const val LANG_AR         = "ar"
        val DEFAULT_TAX_RATE: BigDecimal = BigDecimal("0.10")
        val DEFAULT_TAX_RATE_PERCENT: BigDecimal = BigDecimal("10")
        const val DEFAULT_CURRENCY = "USD"

        /** Legacy cache may store a fraction (&lt; 1); new writes store API percentage. */
        fun fractionFromCachedPercent(raw: BigDecimal): BigDecimal =
            if (raw < BigDecimal.ONE) {
                raw
            } else {
                raw.divide(BigDecimal.valueOf(100), 8, RoundingMode.HALF_UP)
            }

        fun percentFromCachedFraction(fraction: BigDecimal): BigDecimal =
            if (fraction < BigDecimal.ONE) {
                fraction.multiply(BigDecimal.valueOf(100))
            } else {
                fraction
            }
    }
}
