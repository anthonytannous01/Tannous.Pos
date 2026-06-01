package com.tannous.pos.core.di

import android.content.Context
import androidx.room.Room
import com.tannous.pos.core.data.local.AppDatabase
import com.tannous.pos.core.data.local.dao.*
import dagger.Module
import dagger.Provides
import dagger.hilt.InstallIn
import dagger.hilt.android.qualifiers.ApplicationContext
import dagger.hilt.components.SingletonComponent
import javax.inject.Singleton

@Module
@InstallIn(SingletonComponent::class)
object DatabaseModule {
    
    @Provides
    @Singleton
    fun provideAppDatabase(@ApplicationContext context: Context): AppDatabase {
        return AppDatabase.getDatabase(context)
    }
    
    @Provides
    @Singleton
    fun provideCategoryDao(database: AppDatabase): CategoryDao {
        return database.categoryDao()
    }
    
    @Provides
    @Singleton
    fun provideMenuItemDao(database: AppDatabase): MenuItemDao {
        return database.menuItemDao()
    }
    
    @Provides
    @Singleton
    fun provideAddOnDao(database: AppDatabase): AddOnDao {
        return database.addOnDao()
    }
    
    @Provides
    @Singleton
    fun provideCustomerDao(database: AppDatabase): CustomerDao {
        return database.customerDao()
    }
    
    @Provides
    @Singleton
    fun provideOrderDao(database: AppDatabase): OrderDao {
        return database.orderDao()
    }
    
    @Provides
    @Singleton
    fun provideOrderLineDao(database: AppDatabase): OrderLineDao {
        return database.orderLineDao()
    }
    
    @Provides
    @Singleton
    fun provideOrderLineAddOnDao(database: AppDatabase): OrderLineAddOnDao {
        return database.orderLineAddOnDao()
    }
    
    @Provides
    @Singleton
    fun provideKeyValueDao(database: AppDatabase): KeyValueDao {
        return database.keyValueDao()
    }
    
    @Provides
    @Singleton
    fun provideOutboxDao(database: AppDatabase): OutboxDao {
        return database.outboxDao()
    }
}
