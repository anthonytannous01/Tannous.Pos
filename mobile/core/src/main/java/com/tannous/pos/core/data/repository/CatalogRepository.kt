package com.tannous.pos.core.data.repository

import com.tannous.pos.core.data.local.dao.CategoryDao
import com.tannous.pos.core.data.local.dao.MenuItemDao
import com.tannous.pos.core.data.local.dao.AddOnDao
import com.tannous.pos.core.data.local.entity.CategoryEntity
import com.tannous.pos.core.data.local.entity.MenuItemEntity
import com.tannous.pos.core.data.local.entity.AddOnEntity
import com.tannous.pos.core.data.remote.CatalogService
import kotlinx.coroutines.flow.Flow
import timber.log.Timber
import javax.inject.Inject
import javax.inject.Singleton
import java.time.Instant

@Singleton
class CatalogRepository @Inject constructor(
    private val catalogService: CatalogService,
    private val categoryDao: CategoryDao,
    private val menuItemDao: MenuItemDao,
    private val addOnDao: AddOnDao
) {
    
    // Categories
    fun getAllCategories(): Flow<List<CategoryEntity>> {
        return categoryDao.getAllActive()
    }
    
    suspend fun refreshCategories() {
        try {
            val categories = catalogService.getCategories()
            val entities = categories.map { dto ->
                CategoryEntity(
                    id = dto.id,
                    name = dto.name,
                    description = dto.description,
                    displayOrder = dto.displayOrder ?: 0,
                    isActive = dto.isActive,
                    updatedAt = dto.updatedAt?.let { Instant.parse(it) } ?: Instant.now(),
                    isDeleted = false
                )
            }
            categoryDao.upsertAll(entities)
            Timber.d("Refreshed ${entities.size} categories")
        } catch (e: Exception) {
            Timber.e(e, "Error refreshing categories")
            throw e
        }
    }
    
    // Menu Items
    fun getAllMenuItems(): Flow<List<MenuItemEntity>> {
        return menuItemDao.getAllActive()
    }
    
    fun getMenuItemsByCategory(categoryId: String): Flow<List<MenuItemEntity>> {
        return menuItemDao.getByCategory(categoryId)
    }
    
    suspend fun getMenuItemById(id: String): MenuItemEntity? {
        return menuItemDao.getById(id)
    }
    
    suspend fun refreshMenuItems() {
        try {
            val menuItems = catalogService.getMenuItems()
            val entities = menuItems.map { dto ->
                MenuItemEntity(
                    id = dto.id,
                    name = dto.name,
                    description = dto.description,
                    price = dto.price,
                    categoryId = dto.categoryId,
                    imageUrl = dto.imageUrl,
                    isActive = dto.isActive,
                    hasAddOns = dto.hasAddOns,
                    updatedAt = dto.updatedAt?.let { Instant.parse(it) } ?: Instant.now(),
                    isDeleted = false,
                    version = dto.version
                )
            }
            menuItemDao.upsertAll(entities)
            Timber.d("Refreshed ${entities.size} menu items")
        } catch (e: Exception) {
            Timber.e(e, "Error refreshing menu items")
            throw e
        }
    }
    
    // Add-ons
    fun getAllAddOns(): Flow<List<AddOnEntity>> {
        return addOnDao.getAllActive()
    }
    
    suspend fun getAddOnById(id: String): AddOnEntity? {
        return addOnDao.getById(id)
    }
    
    suspend fun refreshAddOns() {
        try {
            val addOns = catalogService.getAddOns()
            val entities = addOns.map { dto ->
                AddOnEntity(
                    id = dto.id,
                    name = dto.name,
                    price = dto.price,
                    isActive = dto.isActive,
                    updatedAt = dto.updatedAt?.let { Instant.parse(it) } ?: Instant.now(),
                    isDeleted = false,
                    version = dto.version
                )
            }
            addOnDao.upsertAll(entities)
            Timber.d("Refreshed ${entities.size} add-ons")
        } catch (e: Exception) {
            Timber.e(e, "Error refreshing add-ons")
            throw e
        }
    }
    
    // Refresh all catalog data
    suspend fun refreshAllCatalogData() {
        try {
            refreshCategories()
            refreshMenuItems()
            refreshAddOns()
            Timber.d("Refreshed all catalog data")
        } catch (e: Exception) {
            Timber.e(e, "Error refreshing catalog data")
            throw e
        }
    }
}
