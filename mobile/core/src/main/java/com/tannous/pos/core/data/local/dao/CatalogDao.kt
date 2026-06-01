package com.tannous.pos.core.data.local.dao

import androidx.paging.PagingSource
import androidx.room.*
import com.tannous.pos.core.data.local.entity.AddOnEntity
import com.tannous.pos.core.data.local.entity.CategoryEntity
import com.tannous.pos.core.data.local.entity.MenuItemEntity
import kotlinx.coroutines.flow.Flow

@Dao
interface CategoryDao {
    
    @Query("SELECT * FROM categories WHERE isDeleted = 0 ORDER BY displayOrder, name")
    fun getAllActive(): Flow<List<CategoryEntity>>
    
    @Query("SELECT * FROM categories WHERE isDeleted = 0 AND isActive = 1 ORDER BY displayOrder, name")
    fun getActiveCategories(): Flow<List<CategoryEntity>>
    
    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun insertAll(categories: List<CategoryEntity>)
    
    @Query("DELETE FROM categories")
    suspend fun deleteAll()
    
    @Transaction
    suspend fun upsertAll(categories: List<CategoryEntity>) {
        deleteAll()
        insertAll(categories)
    }
}

@Dao
interface MenuItemDao {
    
    @Query("SELECT * FROM menu_items WHERE isDeleted = 0 AND isActive = 1 ORDER BY name")
    fun getAllActive(): Flow<List<MenuItemEntity>>
    
    @Query("SELECT * FROM menu_items WHERE categoryId = :categoryId AND isDeleted = 0 AND isActive = 1 ORDER BY name")
    fun getByCategory(categoryId: String): Flow<List<MenuItemEntity>>
    
    @Query("SELECT * FROM menu_items WHERE id = :id")
    suspend fun getById(id: String): MenuItemEntity?
    
    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun insertAll(items: List<MenuItemEntity>)
    
    @Query("DELETE FROM menu_items")
    suspend fun deleteAll()
    
    @Query("UPDATE menu_items SET isDeleted = 1 WHERE id = :id")
    suspend fun markDeleted(id: String)
    
    @Transaction
    suspend fun upsertAll(items: List<MenuItemEntity>) {
        deleteAll()
        insertAll(items)
    }
}

@Dao
interface AddOnDao {
    
    @Query("SELECT * FROM addons WHERE isDeleted = 0 AND isActive = 1 ORDER BY name")
    fun getAllActive(): Flow<List<AddOnEntity>>
    
    @Query("SELECT * FROM addons WHERE id = :id")
    suspend fun getById(id: String): AddOnEntity?
    
    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun insertAll(addOns: List<AddOnEntity>)
    
    @Query("DELETE FROM addons")
    suspend fun deleteAll()
    
    @Transaction
    suspend fun upsertAll(addOns: List<AddOnEntity>) {
        deleteAll()
        insertAll(addOns)
    }
}
