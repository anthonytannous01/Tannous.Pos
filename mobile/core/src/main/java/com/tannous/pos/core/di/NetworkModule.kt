package com.tannous.pos.core.di

import com.tannous.pos.core.BuildConfig
import com.tannous.pos.core.data.remote.*
import com.tannous.pos.core.data.remote.interceptor.*
import dagger.Module
import dagger.Provides
import dagger.hilt.InstallIn
import dagger.hilt.components.SingletonComponent
import kotlinx.serialization.json.Json
import okhttp3.OkHttpClient
import okhttp3.logging.HttpLoggingInterceptor
import retrofit2.Retrofit
import com.jakewharton.retrofit2.converter.kotlinx.serialization.asConverterFactory
import okhttp3.MediaType.Companion.toMediaType
import java.util.concurrent.TimeUnit
import javax.inject.Singleton

@Module
@InstallIn(SingletonComponent::class)
object NetworkModule {
    
    private val json = Json {
        ignoreUnknownKeys = true
        coerceInputValues = true
        isLenient = true
        encodeDefaults = false
    }
    
    @Provides
    @Singleton
    fun provideRetryAfterInterceptor(): RetryAfterInterceptor {
        return RetryAfterInterceptor()
    }
    
    // Separate OkHttpClient for auth endpoints (no authenticator to avoid circular dependency)
    @Provides
    @Singleton
    @javax.inject.Named("AuthClient")
    fun provideAuthOkHttpClient(
        deviceIdInterceptor: DeviceIdInterceptor,
        idempotencyKeyInterceptor: IdempotencyKeyInterceptor,
        etagInterceptor: EtagInterceptor,
        retryAfterInterceptor: RetryAfterInterceptor
    ): OkHttpClient {
        return OkHttpClient.Builder()
            .addInterceptor(deviceIdInterceptor)
            .addInterceptor(idempotencyKeyInterceptor)
            .addInterceptor(etagInterceptor)
            .addInterceptor(retryAfterInterceptor)
            .addInterceptor(HttpLoggingInterceptor().apply {
                level = if (BuildConfig.DEBUG) {
                    HttpLoggingInterceptor.Level.BODY
                } else {
                    HttpLoggingInterceptor.Level.BASIC
                }
            })
            .connectTimeout(30, TimeUnit.SECONDS)
            .readTimeout(30, TimeUnit.SECONDS)
            .writeTimeout(30, TimeUnit.SECONDS)
            .build()
    }
    
    @Provides
    @Singleton
    @javax.inject.Named("AuthRetrofit")
    fun provideAuthRetrofit(@javax.inject.Named("AuthClient") okHttpClient: OkHttpClient): Retrofit {
        val contentType = "application/json".toMediaType()
        return Retrofit.Builder()
            .baseUrl(BuildConfig.BASE_URL)
            .client(okHttpClient)
            .addConverterFactory(json.asConverterFactory(contentType))
            .build()
    }
    
    @Provides
    @Singleton
    @javax.inject.Named("AuthService")
    fun provideAuthServiceForAuthenticator(@javax.inject.Named("AuthRetrofit") retrofit: Retrofit): AuthService {
        return retrofit.create(AuthService::class.java)
    }
    
    // Unqualified AuthService for AuthRepository (uses same auth retrofit instance)
    @Provides
    @Singleton
    fun provideAuthService(@javax.inject.Named("AuthRetrofit") retrofit: Retrofit): AuthService {
        return retrofit.create(AuthService::class.java)
    }
    
    @Provides
    @Singleton
    fun provideOkHttpClient(
        authInterceptor: AuthInterceptor,
        tokenAuthenticator: TokenAuthenticator,
        deviceIdInterceptor: DeviceIdInterceptor,
        idempotencyKeyInterceptor: IdempotencyKeyInterceptor,
        etagInterceptor: EtagInterceptor,
        retryAfterInterceptor: RetryAfterInterceptor
    ): OkHttpClient {
        return OkHttpClient.Builder()
            .authenticator(tokenAuthenticator)
            .addInterceptor(authInterceptor)
            .addInterceptor(deviceIdInterceptor)
            .addInterceptor(idempotencyKeyInterceptor)
            .addInterceptor(etagInterceptor)
            .addInterceptor(retryAfterInterceptor)
            .addInterceptor(HttpLoggingInterceptor().apply {
                level = if (BuildConfig.DEBUG) {
                    HttpLoggingInterceptor.Level.BODY
                } else {
                    HttpLoggingInterceptor.Level.BASIC
                }
            })
            .connectTimeout(30, TimeUnit.SECONDS)
            .readTimeout(30, TimeUnit.SECONDS)
            .writeTimeout(30, TimeUnit.SECONDS)
            .build()
    }
    
    @Provides
    @Singleton
    fun provideRetrofit(okHttpClient: OkHttpClient): Retrofit {
        val contentType = "application/json".toMediaType()
        return Retrofit.Builder()
            .baseUrl(BuildConfig.BASE_URL)
            .client(okHttpClient)
            .addConverterFactory(json.asConverterFactory(contentType))
            .build()
    }
    
    @Provides
    @Singleton
    fun provideCatalogService(retrofit: Retrofit): CatalogService {
        return retrofit.create(CatalogService::class.java)
    }
    
    @Provides
    @Singleton
    fun provideCustomerService(retrofit: Retrofit): CustomerService {
        return retrofit.create(CustomerService::class.java)
    }
    
    @Provides
    @Singleton
    fun provideOrderService(retrofit: Retrofit): OrderService {
        return retrofit.create(OrderService::class.java)
    }
    
    @Provides
    @Singleton
    fun provideShiftService(retrofit: Retrofit): ShiftService {
        return retrofit.create(ShiftService::class.java)
    }
    
    @Provides
    @Singleton
    fun provideSyncService(retrofit: Retrofit): SyncService {
        return retrofit.create(SyncService::class.java)
    }
    
    @Provides
    @Singleton
    fun providePrintingService(retrofit: Retrofit): PrintingService {
        return retrofit.create(PrintingService::class.java)
    }
    
    @Provides
    @Singleton
    fun provideSettingsService(retrofit: Retrofit): SettingsService {
        return retrofit.create(SettingsService::class.java)
    }

    @Provides
    @Singleton
    fun provideReportsService(retrofit: Retrofit): ReportsService {
        return retrofit.create(ReportsService::class.java)
    }

    @Provides
    @Singleton
    fun provideInventoryService(retrofit: Retrofit): InventoryService {
        return retrofit.create(InventoryService::class.java)
    }
    
    @Provides
    @Singleton
    fun provideHealthService(retrofit: Retrofit): HealthService {
        return retrofit.create(HealthService::class.java)
    }

    @Provides
    @Singleton
    fun provideKdsService(retrofit: Retrofit): KdsService {
        return retrofit.create(KdsService::class.java)
    }

    @Provides
    @Singleton
    fun provideLoyaltyService(retrofit: Retrofit): LoyaltyService {
        return retrofit.create(LoyaltyService::class.java)
    }

    @Provides
    @Singleton
    fun provideTableService(retrofit: Retrofit): TableService {
        return retrofit.create(TableService::class.java)
    }
}
