package com.tannous.pos.core.data.local

import androidx.room.Database
import androidx.room.Room
import androidx.room.RoomDatabase
import androidx.room.TypeConverters
import androidx.room.migration.Migration
import androidx.sqlite.db.SupportSQLiteDatabase
import android.content.Context
import com.tannous.pos.core.data.local.converter.Converters
import com.tannous.pos.core.data.local.dao.*
import com.tannous.pos.core.data.local.entity.*

@Database(
    entities = [
        CategoryEntity::class,
        MenuItemEntity::class,
        AddOnEntity::class,
        CustomerEntity::class,
        OrderEntity::class,
        OrderLineEntity::class,
        OrderLineAddOnEntity::class,
        ShiftEntity::class,
        KeyValueEntity::class,
        OutboxOperationEntity::class
    ],
    version = 5,
    exportSchema = false
)
@TypeConverters(Converters::class)
abstract class AppDatabase : RoomDatabase() {
    
    abstract fun categoryDao(): CategoryDao
    abstract fun menuItemDao(): MenuItemDao
    abstract fun addOnDao(): AddOnDao
    abstract fun customerDao(): CustomerDao
    abstract fun orderDao(): OrderDao
    abstract fun orderLineDao(): OrderLineDao
    abstract fun orderLineAddOnDao(): OrderLineAddOnDao
    abstract fun shiftDao(): ShiftDao
    abstract fun keyValueDao(): KeyValueDao
    abstract fun outboxDao(): OutboxDao
    
    companion object {
        private val MIGRATION_1_2 = object : Migration(1, 2) {
            override fun migrate(database: SupportSQLiteDatabase) {
                // Add missing columns to shifts table
                database.execSQL("ALTER TABLE shifts ADD COLUMN openedAt TEXT")
                database.execSQL("ALTER TABLE shifts ADD COLUMN closedAt TEXT")
                database.execSQL("ALTER TABLE shifts ADD COLUMN isDeleted INTEGER NOT NULL DEFAULT 0")
                database.execSQL("ALTER TABLE shifts ADD COLUMN deletedAt TEXT")
            }
        }
        
        private val MIGRATION_2_3 = object : Migration(2, 3) {
            override fun migrate(database: SupportSQLiteDatabase) {
                // Add syncedAt column to shifts table
                database.execSQL("ALTER TABLE shifts ADD COLUMN syncedAt TEXT")
            }
        }

        private val MIGRATION_3_4 = object : Migration(3, 4) {
            override fun migrate(database: SupportSQLiteDatabase) {
                database.execSQL(
                    "ALTER TABLE menu_items ADD COLUMN hasAddOns INTEGER NOT NULL DEFAULT 0"
                )
            }
        }

        private val MIGRATION_4_5 = object : Migration(4, 5) {
            override fun migrate(database: SupportSQLiteDatabase) {
                // Arabic localisation fields
                database.execSQL("ALTER TABLE menu_items ADD COLUMN nameAr TEXT")
                database.execSQL("ALTER TABLE menu_items ADD COLUMN descriptionAr TEXT")
                database.execSQL("ALTER TABLE categories ADD COLUMN nameAr TEXT")
            }
        }
        
        @Volatile
        private var INSTANCE: AppDatabase? = null
        
        fun getDatabase(context: Context): AppDatabase {
            return INSTANCE ?: synchronized(this) {
                val instance = Room.databaseBuilder(
                    context.applicationContext,
                    AppDatabase::class.java,
                    "tannous_pos_database"
                )
                .addMigrations(MIGRATION_1_2, MIGRATION_2_3, MIGRATION_3_4, MIGRATION_4_5)
                .build()
                INSTANCE = instance
                instance
            }
        }
    }
}
