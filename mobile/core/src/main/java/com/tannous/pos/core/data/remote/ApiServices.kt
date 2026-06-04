package com.tannous.pos.core.data.remote

import com.tannous.pos.core.data.model.*
import okhttp3.ResponseBody
import retrofit2.Response
import retrofit2.http.*

interface AuthService {
    
    @POST("auth/login")
    suspend fun login(@Body request: LoginRequest): LoginResponse
    
    @POST("auth/refresh")
    suspend fun refreshToken(@Body request: RefreshTokenRequest): RefreshTokenResponse
    
    @POST("auth/logout")
    suspend fun logout(@Body request: RefreshTokenRequest)
}

interface CatalogService {
    
    @GET("catalog/categories")
    suspend fun getCategories(): List<CategoryDto>
    
    @GET("catalog/menu-items")
    suspend fun getMenuItems(): List<MenuItemDto>
    
    @GET("catalog/addons")
    suspend fun getAddOns(): List<AddOnDto>
}

interface CustomerService {
    
    @GET("customers")
    suspend fun getCustomers(
        @Query("page") page: Int = 1,
        @Query("pageSize") pageSize: Int = 20,
        @Query("search") search: String? = null
    ): PaginatedResponseDto<CustomerDto>
    
    @POST("customers")
    suspend fun createCustomer(@Body request: CreateCustomerRequest): CustomerDto
    
    @PUT("customers/{id}")
    suspend fun updateCustomer(
        @Path("id") id: String,
        @Body request: UpdateCustomerRequest
    ): CustomerDto
    
    @GET("customers/{id}")
    suspend fun getCustomer(@Path("id") id: String): CustomerDto
}

interface OrderService {
    
    @POST("orders")
    suspend fun createOrder(@Body request: CreateOrderRequest): OrderDto
    
    @GET("orders/{id}")
    suspend fun getOrder(@Path("id") id: String): OrderDto

    @GET("orders")
    suspend fun getOrders(
        @Query("startDate") startDate: String? = null,
        @Query("endDate") endDate: String? = null,
        @Query("status") status: String? = null
    ): List<OrderDto>
    
    @POST("orders/{id}/finalize")
    suspend fun finalizeOrder(
        @Path("id") id: String,
        @Body request: FinalizeOrderRequest
    ): OrderDto

    @POST("orders/{id}/void")
    suspend fun voidOrder(
        @Path("id") id: String,
        @Body request: VoidOrderRequest
    ): OrderDto
}

interface ShiftService {
    
    @GET("shifts/current")
    suspend fun getCurrentShift(): ShiftDto
    
    @POST("shifts/open")
    suspend fun openShift(@Body request: OpenShiftRequest): ShiftDto
    
    @POST("shifts/{id}/cash-drop")
    suspend fun cashDrop(
        @Path("id") id: String,
        @Body request: CashDropRequest
    ): Response<Unit>
    
    @POST("shifts/{id}/close")
    suspend fun closeShift(
        @Path("id") id: String,
        @Body request: CloseShiftRequest
    ): ShiftDto
}

interface SyncService {
    
    @GET("sync/pull")
    suspend fun pull(
        @Query("since") since: String? = null,
        @Query("limit") limit: Int = 500,
        @Query("token") token: String? = null
    ): SyncPullResponse
    
    @POST("sync/push")
    suspend fun push(@Body request: SyncPushRequest): SyncPushResponse
}

interface PrintingService {
    
    @POST("print/receipt/render")
    suspend fun renderReceipt(@Body request: PrintReceiptRequest): PrintReceiptResponse
    
    @POST("print/kitchen/render")
    suspend fun renderKitchen(@Body request: PrintReceiptRequest): PrintReceiptResponse
}

interface SettingsService {

    @GET("settings")
    suspend fun getSettings(): BusinessSettingsDto

    @PUT("settings")
    suspend fun updateSettings(@Body request: UpdateSettingsRequest): BusinessSettingsDto
}

interface KdsService {

    /** Poll for active tickets (Pending + InProgress by default). */
    @GET("v1/kds/tickets")
    suspend fun getTickets(
        @Query("status") status: Int? = null
    ): List<KdsTicketDto>

    /** Update the KDS status of a single order line. */
    @PATCH("v1/kds/tickets/{orderLineId}/status")
    suspend fun updateStatus(
        @Path("orderLineId") orderLineId: String,
        @Body request: UpdateKdsStatusRequest
    ): KdsTicketDto
}

interface InventoryService {

    @GET("inventory")
    suspend fun getInventoryItems(): List<InventoryItemDto>

    @GET("inventory/low-stock")
    suspend fun getLowStockItems(): List<InventoryItemDto>

    @GET("inventory/ingredients")
    suspend fun getIngredients(): List<IngredientDto>

    @POST("inventory/ingredients")
    suspend fun createIngredient(@Body body: CreateIngredientRequest): IngredientDto

    @PUT("inventory/ingredients/{id}")
    suspend fun updateIngredient(
        @Path("id") id: String,
        @Body body: UpdateIngredientRequest
    ): IngredientDto

    @DELETE("inventory/ingredients/{id}")
    suspend fun deleteIngredient(
        @Path("id") id: String,
        @Query("force") force: Boolean = false
    ): Response<Unit>

    @GET("inventory/recipes")
    suspend fun getRecipes(): List<RecipeDto>

    @POST("inventory/recipes")
    suspend fun createRecipe(@Body body: CreateRecipeRequest): RecipeDto

    @PUT("inventory/recipes/{id}")
    suspend fun updateRecipe(
        @Path("id") id: String,
        @Body body: UpdateRecipeRequest
    ): RecipeDto

    @DELETE("inventory/recipes/{id}")
    suspend fun deleteRecipe(
        @Path("id") id: String,
        @Query("force") force: Boolean = false
    ): Response<Unit>
}

interface ReportsService {

    @GET("reports/eod")
    suspend fun getEodReport(
        @Query("date") date: String? = null
    ): EodReportDto

    @GET("reports/cogs")
    suspend fun getCogsReport(
        @Query("from") from: String,
        @Query("to") to: String
    ): CogsReportDto

    @GET("reports/export/eod.csv")
    @Headers("Accept: text/csv")
    suspend fun getEodCsv(
        @Query("date") date: String? = null
    ): Response<ResponseBody>
}

interface HealthService {
    
    @GET("health/ready")
    suspend fun getHealth(): Map<String, Any>
}
