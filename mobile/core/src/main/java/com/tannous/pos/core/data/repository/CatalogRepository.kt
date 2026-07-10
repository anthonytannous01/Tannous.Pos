package com.tannous.pos.core.data.repository

import com.tannous.pos.core.data.local.dao.CategoryDao
import com.tannous.pos.core.data.local.dao.MenuItemDao
import com.tannous.pos.core.data.local.dao.AddOnDao
import com.tannous.pos.core.data.local.entity.CategoryEntity
import com.tannous.pos.core.data.local.entity.MenuItemEntity
import com.tannous.pos.core.data.local.entity.AddOnEntity
import com.tannous.pos.core.data.model.CreateCategoryRequest
import com.tannous.pos.core.data.model.CreateMenuItemRequest
import com.tannous.pos.core.data.model.UpdateCategoryRequest
import com.tannous.pos.core.data.model.UpdateMenuItemRequest
import com.tannous.pos.core.data.remote.CatalogService
import kotlinx.coroutines.flow.Flow
import timber.log.Timber
import javax.inject.Inject
import javax.inject.Singleton
import java.io.IOException
import java.math.BigDecimal
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
                    nameAr = dto.nameAr,
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
    
    suspend fun createCategory(name: String, description: String? = null, displayOrder: Int = 0): CategoryEntity {
        val dto = catalogService.createCategory(
            CreateCategoryRequest(name = name, description = description, displayOrder = displayOrder)
        )
        val entity = CategoryEntity(
            id = dto.id, name = dto.name, description = dto.description,
            displayOrder = dto.displayOrder, isActive = dto.isActive,
            updatedAt = dto.updatedAt?.let { Instant.parse(it) } ?: Instant.now(), isDeleted = false
        )
        categoryDao.insertAll(listOf(entity)) // insertAll uses REPLACE — safe for single-entity write
        return entity
    }

    suspend fun updateCategory(id: String, name: String, description: String? = null, displayOrder: Int = 0): CategoryEntity {
        val dto = catalogService.updateCategory(id, UpdateCategoryRequest(name = name, description = description, displayOrder = displayOrder))
        val entity = CategoryEntity(
            id = dto.id, name = dto.name, description = dto.description,
            displayOrder = dto.displayOrder, isActive = dto.isActive,
            updatedAt = dto.updatedAt?.let { Instant.parse(it) } ?: Instant.now(), isDeleted = false
        )
        categoryDao.insertAll(listOf(entity)) // insertAll uses REPLACE — safe for single-entity write
        return entity
    }

    suspend fun deleteCategory(id: String) {
        val response = catalogService.deleteCategory(id)
        if (!response.isSuccessful) {
            // deleteCategory returns Response<Unit> — Retrofit does NOT throw on 4xx/5xx for
            // Response<T>-typed calls, so this check is required or failures (e.g. 409 Conflict
            // "category has menu items") get silently swallowed and look like a successful delete.
            val errorMessage = try {
                response.errorBody()?.string()?.takeIf { it.isNotBlank() }
                    ?: "Failed to delete category (HTTP ${response.code()})"
            } catch (ex: Exception) {
                "Failed to delete category (HTTP ${response.code()})"
            }
            Timber.e("Delete category failed: HTTP ${response.code()} - $errorMessage")
            throw IOException(errorMessage)
        }
        refreshCategories() // re-sync so Room reflects server state
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
                    nameAr = dto.nameAr,
                    description = dto.description,
                    descriptionAr = dto.descriptionAr,
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
    
    suspend fun createMenuItem(
        name: String, nameAr: String? = null, price: BigDecimal,
        categoryId: String, description: String? = null, displayOrder: Int = 0
    ): MenuItemEntity {
        val dto = catalogService.createMenuItem(
            CreateMenuItemRequest(
                name = name, nameAr = nameAr, price = price,
                categoryId = categoryId, description = description, displayOrder = displayOrder
            )
        )
        val entity = MenuItemEntity(
            id = dto.id, name = dto.name, nameAr = dto.nameAr,
            description = dto.description, descriptionAr = dto.descriptionAr, price = dto.price,
            categoryId = dto.categoryId, imageUrl = dto.imageUrl, isActive = dto.isActive,
            hasAddOns = dto.hasAddOns, updatedAt = dto.updatedAt?.let { Instant.parse(it) } ?: Instant.now(),
            isDeleted = false, version = dto.version
        )
        menuItemDao.upsertAll(listOf(entity))
        return entity
    }

    suspend fun updateMenuItem(
        id: String, name: String, nameAr: String? = null, price: BigDecimal,
        categoryId: String, description: String? = null, displayOrder: Int = 0
    ): MenuItemEntity {
        val dto = catalogService.updateMenuItem(
            id,
            UpdateMenuItemRequest(
                name = name, nameAr = nameAr, price = price,
                categoryId = categoryId, description = description, displayOrder = displayOrder
            )
        )
        val entity = MenuItemEntity(
            id = dto.id, name = dto.name, nameAr = dto.nameAr,
            description = dto.description, descriptionAr = dto.descriptionAr, price = dto.price,
            categoryId = dto.categoryId, imageUrl = dto.imageUrl, isActive = dto.isActive,
            hasAddOns = dto.hasAddOns, updatedAt = dto.updatedAt?.let { Instant.parse(it) } ?: Instant.now(),
            isDeleted = false, version = dto.version
        )
        menuItemDao.upsertAll(listOf(entity))
        return entity
    }

    suspend fun deleteMenuItem(id: String) {
        val response = catalogService.deleteMenuItem(id)
        if (!response.isSuccessful) {
            // deleteMenuItem returns Response<Unit> — Retrofit does NOT throw on 4xx/5xx for
            // Response<T>-typed calls, so this check is required or failures (most commonly a 409
            // Conflict when the item has order history — DeleteMenuItemCommandHandler blocks a hard
            // delete unless force=true) get silently swallowed and look like a successful delete.
            val errorMessage = try {
                response.errorBody()?.string()?.takeIf { it.isNotBlank() }
                    ?: "Failed to delete item (HTTP ${response.code()})"
            } catch (ex: Exception) {
                "Failed to delete item (HTTP ${response.code()})"
            }
            Timber.e("Delete menu item failed: HTTP ${response.code()} - $errorMessage")
            throw IOException(errorMessage)
        }
        refreshMenuItems() // re-sync so Room reflects server state
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
