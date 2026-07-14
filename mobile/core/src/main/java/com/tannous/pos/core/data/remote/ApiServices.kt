package com.tannous.pos.core.data.remote

import com.tannous.pos.core.data.model.*
import okhttp3.ResponseBody
import retrofit2.Response
import retrofit2.http.Body
import retrofit2.http.DELETE
import retrofit2.http.GET
import retrofit2.http.Headers
import retrofit2.http.PATCH
import retrofit2.http.POST
import retrofit2.http.PUT
import retrofit2.http.Path
import retrofit2.http.Query

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

    @POST("catalog/categories")
    suspend fun createCategory(@Body request: CreateCategoryRequest): CategoryDto

    @PUT("catalog/categories/{id}")
    suspend fun updateCategory(
        @Path("id") id: String,
        @Body request: UpdateCategoryRequest
    ): CategoryDto

    @DELETE("catalog/categories/{id}")
    suspend fun deleteCategory(@Path("id") id: String): Response<Unit>

    @GET("catalog/menu-items")
    suspend fun getMenuItems(
        // includeInactive=true also returns archived items so the local cache mirrors
        // the full (non-deleted) catalog; ordering screens filter actives locally.
        @Query("includeInactive") includeInactive: Boolean = false
    ): List<MenuItemDto>

    @GET("catalog/menu-items/{id}")
    suspend fun getMenuItem(@Path("id") id: String): MenuItemDto

    @POST("catalog/menu-items")
    suspend fun createMenuItem(@Body request: CreateMenuItemRequest): MenuItemDto

    @PUT("catalog/menu-items/{id}")
    suspend fun updateMenuItem(
        @Path("id") id: String,
        @Body request: UpdateMenuItemRequest
    ): MenuItemDto

    @DELETE("catalog/menu-items/{id}")
    suspend fun deleteMenuItem(
        @Path("id") id: String,
        // force=true archives (deactivates) an item that has order history instead of hard-deleting;
        // without it the server returns 409 Conflict for such items.
        @Query("force") force: Boolean = false
    ): Response<Unit>

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

    @GET("orders/{id}/split")
    suspend fun getSplitBill(
        @Path("id") id: String,
        @Query("ways") ways: Int
    ): SplitBillDto

    @POST("orders/{id}/split/pay")
    suspend fun recordSplitPayment(
        @Path("id") id: String,
        @Body request: RecordSplitPaymentRequest
    ): SplitBillDto
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

/** Employee scheduling + time tracking (distinct from cash register shifts). */
interface ScheduleService {

    @GET("schedule/week")
    suspend fun getWeeklySchedule(
        @Query("weekStart") weekStart: String,
        @Query("branchId") branchId: String? = null
    ): WeeklyScheduleDto

    @POST("schedule/clock-in")
    suspend fun clockIn(@Body body: ClockInRequest): TimeEntryDto

    @POST("schedule/clock-out")
    suspend fun clockOut(@Body body: ClockOutRequest): TimeEntryDto

    /** 200 with body when clocked in; 204 → null when not. */
    @GET("schedule/my-clock-status")
    suspend fun myClockStatus(): TimeEntryDto?

    /** Current user's own entries — staff self-service, no manager policy needed. */
    @GET("schedule/my-time-entries")
    suspend fun getMyTimeEntries(
        @Query("from") from: String,
        @Query("to") to: String
    ): List<TimeEntryDto>

    /** Manager view across users. */
    @GET("schedule/time-entries")
    suspend fun getTimeEntries(
        @Query("from") from: String,
        @Query("to") to: String,
        @Query("userId") userId: String? = null,
        @Query("branchId") branchId: String? = null
    ): List<TimeEntryDto>

    /**
     * Lightweight staff list for the shift-picker.
     * Requires CanManageShifts (Owner + Manager) — NOT CanManageUsers.
     */
    @GET("schedule/staff")
    suspend fun listStaff(
        @Query("search") search: String? = null
    ): List<UserDto>

    /** Create a new shift (manager/owner only — CanManageShifts policy). */
    @POST("schedule")
    suspend fun createSchedule(@Body request: CreateScheduleRequest): EmployeeScheduleDto

    /** Cancel / delete a shift by id. */
    @DELETE("schedule/{id}")
    suspend fun cancelSchedule(@Path("id") id: String)

    /** Publish all draft shifts in the given id list. */
    @POST("schedule/publish")
    suspend fun publishSchedule(@Body request: PublishScheduleRequest): List<EmployeeScheduleDto>
}

/** User directory — manager/owner list employees for the shift picker. */
interface UserService {

    @GET("users")
    suspend fun listUsers(
        @Query("page") page: Int = 1,
        @Query("pageSize") pageSize: Int = 100,
        @Query("search") search: String? = null
    ): PaginatedResponseDto<UserDto>
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

interface TableService {

    @GET("tables/floor-plans")
    suspend fun getFloorPlans(): List<FloorPlanDto>

    @POST("tables/floor-plans")
    suspend fun createFloorPlan(@Body request: CreateFloorPlanRequest): FloorPlanDto

    @POST("tables")
    suspend fun createTable(@Body request: CreateTableRequest): TableDto

    @DELETE("tables/{tableId}")
    suspend fun deleteTable(@Path("tableId") tableId: String)

    @PATCH("tables/{tableId}/status")
    suspend fun updateStatus(
        @Path("tableId") tableId: String,
        @Body request: UpdateTableStatusRequest
    ): TableDto
}

interface LoyaltyService {

    @GET("loyalty/customers/{customerId}")
    suspend fun getAccount(@Path("customerId") customerId: String): LoyaltyAccountDto

    @POST("loyalty/customers/{customerId}/earn")
    suspend fun earn(
        @Path("customerId") customerId: String,
        @Body request: EarnPointsRequest
    ): LoyaltyAccountDto

    @POST("loyalty/customers/{customerId}/redeem")
    suspend fun redeem(
        @Path("customerId") customerId: String,
        @Body request: RedeemPointsRequest
    ): LoyaltyAccountDto

    /** CRM analytics summary: segment counts, averages, and top customers. */
    @GET("loyalty/analytics")
    suspend fun getAnalytics(): CustomerAnalyticsDto

    /** Paginated list of customers in a behavioural segment (enum value 0..4). */
    @GET("loyalty/segments/{segment}")
    suspend fun getSegment(
        @Path("segment") segment: Int,
        @Query("page") page: Int = 1,
        @Query("pageSize") pageSize: Int = 50
    ): CustomerSegmentPageDto

    /** Dispatch a WhatsApp campaign to all customers in a target segment. */
    @POST("loyalty/campaigns")
    suspend fun sendCampaign(@Body request: SendCampaignRequest): LoyaltyCampaignDto
}

interface KdsService {

    @GET("kds/stations")
    suspend fun getStations(@Query("branchId") branchId: String? = null): List<KdsStationDto>

    /** Poll for active tickets (Pending + InProgress by default). */
    @GET("kds/tickets")
    suspend fun getTickets(
        @Query("status") status: Int? = null,
        @Query("stationId") stationId: String? = null
    ): List<KdsTicketDto>

    /** Update the KDS status of a single order line. */
    @PATCH("kds/tickets/{orderLineId}/status")
    suspend fun updateStatus(
        @Path("orderLineId") orderLineId: String,
        @Body request: UpdateKdsStatusRequest
    ): KdsTicketDto
}

interface AccountingService {

    @GET("accounting/quickbooks/connect")
    suspend fun getQuickBooksConnectUrl(@Query("branchId") branchId: String? = null): QuickBooksConnectResponse

    @GET("accounting/status")
    suspend fun getStatus(@Query("branchId") branchId: String? = null): List<AccountingConnectionStatusDto>

    @POST("accounting/sync")
    suspend fun triggerSync(
        @Query("date") date: String? = null,
        @Query("branchId") branchId: String? = null
    ): SyncTriggerResponse

    @DELETE("accounting/{provider}")
    suspend fun disconnect(
        @Path("provider") provider: String,
        @Query("branchId") branchId: String? = null
    )
}

interface WebhooksService {

    @GET("webhooks")
    suspend fun getSubscriptions(): List<WebhookSubscriptionDto>

    @DELETE("webhooks/{id}")
    suspend fun deleteSubscription(@Path("id") id: String)

    @POST("webhooks/{id}/test")
    suspend fun testSubscription(@Path("id") id: String)

    @GET("apikeys")
    suspend fun getApiKeys(): List<ApiKeyDto>

    @DELETE("apikeys/{id}")
    suspend fun revokeApiKey(@Path("id") id: String)
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

    /** Full sales export CSV — one row per paid order. */
    @GET("reports/export/sales.csv")
    @Headers("Accept: text/csv")
    suspend fun getSalesCsv(
        @Query("from")      from:     String,
        @Query("to")        to:       String,
        @Query("branchId")  branchId: String? = null
    ): Response<ResponseBody>

    /** Purchase orders export CSV. */
    @GET("reports/export/purchases.csv")
    @Headers("Accept: text/csv")
    suspend fun getPurchasesCsv(
        @Query("from") from: String,
        @Query("to")   to:   String
    ): Response<ResponseBody>

    /** Real-time owner dashboard summary. Defaults to today when from/to omitted. */
    @GET("reports/summary")
    suspend fun getSalesSummary(
        @Query("from") from: String? = null,
        @Query("to") to: String? = null,
        @Query("branchId") branchId: String? = null
    ): SalesSummaryDto

    /** Menu engineering matrix — Stars/Plowhorses/Puzzles/Dogs. */
    @GET("reports/menu-engineering")
    suspend fun getMenuEngineering(
        @Query("from") from: String,
        @Query("to") to: String
    ): MenuEngineeringReportDto

    /** Rule-based demand forecast (Smart Suggestions). Defaults to tomorrow when targetDate omitted. */
    @GET("reports/forecast")
    suspend fun getForecast(
        @Query("targetDate") targetDate: String? = null,
        @Query("branchId") branchId: String? = null
    ): DemandForecastDto

    /** Kitchen performance analytics over completed KDS tickets. */
    @GET("reports/kds-performance")
    suspend fun getKdsPerformance(
        @Query("from") from: String,
        @Query("to") to: String,
        @Query("branchId") branchId: String? = null
    ): KdsPerformanceDto

    @GET("reports/section-sales")
    suspend fun getSectionSales(
        @Query("from") from: String,
        @Query("to") to: String,
        @Query("branchId") branchId: String? = null
    ): SectionSalesReportDto

    @GET("receipts/{orderId}")
    suspend fun getReceipt(@Path("orderId") orderId: String): ReceiptDto
}

interface SupplierIntelligenceService {

    @GET("suppliers/intelligence")
    suspend fun getIntelligence(
        @Query("forecastDays") forecastDays: Int = 7,
        @Query("branchId") branchId: String? = null
    ): SupplierIntelligenceDto

    @POST("suppliers/intelligence/create-orders")
    suspend fun createSuggestedOrders(
        @Body request: CreateSuggestedOrdersRequest
    ): CreateSuggestedOrdersResult
}

interface KioskService {

    @GET("kiosk/menu")
    suspend fun getMenu(): PublicMenuDto

    @POST("kiosk/orders")
    suspend fun placeOrder(@Body request: KioskOrderRequest): KioskOrderResultDto
}

interface DeliveryService {

    @GET("delivery/queue")
    suspend fun getQueue(
        @Query("branchId") branchId: String? = null,
        @Query("status")   status:   Int?    = null,
        @Query("from")     from:     String? = null,
        @Query("to")       to:       String? = null
    ): List<DeliveryDto>

    @POST("delivery")
    suspend fun create(@Body request: CreateDeliveryInfoRequest): DeliveryDto

    @PATCH("delivery/{id}/status")
    suspend fun updateStatus(
        @Path("id") id: String,
        @Body request: UpdateDeliveryStatusRequest
    ): DeliveryDto
}

interface ReservationService {

    @GET("reservations")
    suspend fun getReservations(
        @Query("branchId") branchId: String? = null,
        @Query("from")     from:     String? = null,
        @Query("to")       to:       String? = null,
        @Query("status")   status:   Int?    = null
    ): List<ReservationDto>

    @GET("reservations/available-tables")
    suspend fun getAvailableTables(
        @Query("slot")      slot:      String,
        @Query("partySize") partySize: Int = 1,
        @Query("branchId")  branchId:  String? = null
    ): List<AvailableTableDto>

    @POST("reservations")
    suspend fun create(@Body request: CreateReservationRequest): ReservationDto

    @PATCH("reservations/{id}/status")
    suspend fun updateStatus(
        @Path("id") id: String,
        @Body request: UpdateReservationStatusRequest
    ): ReservationDto
}

interface FeedbackService {

    @POST("feedback")
    suspend fun submit(@Body request: SubmitFeedbackRequest): FeedbackDto

    @GET("feedback/summary")
    suspend fun getSummary(
        @Query("branchId")  branchId:  String? = null,
        @Query("from")      from:      String? = null,
        @Query("to")        to:        String? = null,
        @Query("recentMax") recentMax: Int = 20
    ): FeedbackSummaryDto
}

interface BranchService {

    @GET("branches")
    suspend fun getBranches(
        @Query("activeOnly") activeOnly: Boolean = true
    ): List<BranchDto>

    @POST("branches")
    suspend fun createBranch(@Body request: CreateBranchRequest): BranchDto
}

interface HealthService {
    
    @GET("health/ready")
    suspend fun getHealth(): Map<String, Any>
}
