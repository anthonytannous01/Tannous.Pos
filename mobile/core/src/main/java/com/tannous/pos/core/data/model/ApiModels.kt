package com.tannous.pos.core.data.model

import kotlinx.serialization.SerialName
import kotlinx.serialization.Serializable
import java.math.BigDecimal
import java.time.Instant
import com.tannous.pos.core.serialization.BigDecimalAsStringSerializer

// Auth
@Serializable
data class LoginRequest(
    val username: String,
    val password: String
)

@Serializable
data class LoginResponse(
    @SerialName("accessToken")
    val accessToken: String,
    @SerialName("refreshToken")
    val refreshToken: String,
    @SerialName("expiresIn")
    val expiresIn: Int, // seconds
    val user: UserDto
)

@Serializable
data class UserDto(
    val id: String,
    val username: String,
    val email: String,
    @SerialName("firstName")
    val firstName: String,
    @SerialName("lastName")
    val lastName: String,
    val role: String
)

@Serializable
data class RefreshTokenRequest(
    @SerialName("refreshToken")
    val refreshToken: String
)

@Serializable
data class RefreshTokenResponse(
    @SerialName("accessToken")
    val accessToken: String,
    @SerialName("refreshToken")
    val refreshToken: String,
    @SerialName("expiresIn")
    val expiresIn: Int, // seconds
    val user: UserDto
)

// Business Settings
@Serializable
data class BusinessSettingsDto(
    val id: String = "",
    @SerialName("storeName")
    val storeName: String = "",
    val address: String? = null,
    val phone: String? = null,
    val email: String? = null,
    val website: String? = null,
    val taxNumber: String? = null,
    @Serializable(with = BigDecimalAsStringSerializer::class)
    val taxRate: BigDecimal = BigDecimal.ZERO,
    val currency: String = "USD",
    val taxEnabled: Boolean = false,
    val receiptHeader: String? = null,
    val receiptFooter: String? = null,
    val requireCustomerInfo: Boolean = false,
    val enableInventoryTracking: Boolean = false,
    val enableRecipeManagement: Boolean = false,
    // Loyalty
    val loyaltyEnabled: Boolean = false,
    val loyaltyPointsPerDollar: Int = 10,
    @Serializable(with = BigDecimalAsStringSerializer::class)
    val loyaltyPointValueUsd: BigDecimal = BigDecimal("0.01"),
    val loyaltyMinRedeemPoints: Int = 100,
    // Lebanese market
    @Serializable(with = BigDecimalAsStringSerializer::class)
    val exchangeRateLbpPerUsd: BigDecimal = BigDecimal.ZERO,
    val showLbpOnReceipt: Boolean = false,
    val stampDutyEnabled: Boolean = false,
    @Serializable(with = BigDecimalAsStringSerializer::class)
    val stampDutyAmountUsd: BigDecimal = BigDecimal("2.00"),
    // Notifications
    val notifyOnLoyaltyEarn: Boolean = false,
    val notifyOnReservationConfirm: Boolean = false,
    // Arabic
    val businessNameAr: String? = null
)

@Serializable
data class UpdateSettingsRequest(
    val storeName: String,
    val address: String? = null,
    val phone: String? = null,
    val email: String? = null,
    val website: String? = null,
    val taxNumber: String? = null,
    @Serializable(with = BigDecimalAsStringSerializer::class)
    val taxRate: BigDecimal,
    val currency: String,
    val taxEnabled: Boolean,
    val receiptHeader: String? = null,
    val receiptFooter: String? = null,
    val requireCustomerInfo: Boolean,
    val enableInventoryTracking: Boolean,
    val enableRecipeManagement: Boolean,
    // Loyalty
    val loyaltyEnabled: Boolean = false,
    val loyaltyPointsPerDollar: Int = 10,
    @Serializable(with = BigDecimalAsStringSerializer::class)
    val loyaltyPointValueUsd: BigDecimal = BigDecimal("0.01"),
    val loyaltyMinRedeemPoints: Int = 100,
    // Lebanese market
    @Serializable(with = BigDecimalAsStringSerializer::class)
    val exchangeRateLbpPerUsd: BigDecimal = BigDecimal.ZERO,
    val showLbpOnReceipt: Boolean = false,
    val stampDutyEnabled: Boolean = false,
    @Serializable(with = BigDecimalAsStringSerializer::class)
    val stampDutyAmountUsd: BigDecimal = BigDecimal("2.00"),
    val notifyOnLoyaltyEarn: Boolean = false,
    val notifyOnReservationConfirm: Boolean = false
)

// Reports
@Serializable
data class EodReportDto(
    val date: String = "",
    @Serializable(with = BigDecimalAsStringSerializer::class)
    val netSales: BigDecimal = BigDecimal.ZERO,
    val ordersCount: Int = 0,
    @Serializable(with = BigDecimalAsStringSerializer::class)
    val avgTicket: BigDecimal = BigDecimal.ZERO,
    @Serializable(with = BigDecimalAsStringSerializer::class)
    val cashDrops: BigDecimal = BigDecimal.ZERO,
    @Serializable(with = BigDecimalAsStringSerializer::class)
    val variance: BigDecimal? = null,
    val topItems: List<EodTopItemDto> = emptyList()
)

@Serializable
data class EodTopItemDto(
    val itemId: String = "",
    val name: String = "",
    val qty: Int = 0,
    @Serializable(with = BigDecimalAsStringSerializer::class)
    val sales: BigDecimal = BigDecimal.ZERO
)

@Serializable
data class CogsReportDto(
    val from: String = "",
    val to: String = "",
    @Serializable(with = BigDecimalAsStringSerializer::class)
    val salesTotal: BigDecimal = BigDecimal.ZERO,
    @Serializable(with = BigDecimalAsStringSerializer::class)
    val cogsTotal: BigDecimal = BigDecimal.ZERO,
    @Serializable(with = BigDecimalAsStringSerializer::class)
    val grossMargin: BigDecimal = BigDecimal.ZERO,
    val ingredientUsage: List<CogsItemDto> = emptyList()
)

@Serializable
data class CogsItemDto(
    val ingredientId: String = "",
    val name: String = "",
    @Serializable(with = BigDecimalAsStringSerializer::class)
    val qtyUsed: BigDecimal = BigDecimal.ZERO,
    @Serializable(with = BigDecimalAsStringSerializer::class)
    val cost: BigDecimal = BigDecimal.ZERO
)

// Inventory
@Serializable
data class InventoryItemDto(
    val id: String = "",
    val ingredientId: String = "",
    val ingredientName: String = "",
    val ingredientUnit: String = "",
    @Serializable(with = BigDecimalAsStringSerializer::class)
    val currentStock: BigDecimal = BigDecimal.ZERO,
    @Serializable(with = BigDecimalAsStringSerializer::class)
    val minimumStock: BigDecimal = BigDecimal.ZERO,
    @Serializable(with = BigDecimalAsStringSerializer::class)
    val maximumStock: BigDecimal = BigDecimal.ZERO,
    @Serializable(with = BigDecimalAsStringSerializer::class)
    val averageCost: BigDecimal = BigDecimal.ZERO,
    val lastUpdated: String = "",
    val createdAt: String = ""
)

@Serializable
data class AdjustInventoryPayload(
    val ingredientId: String,
    val quantity: String,
    val reason: String
)

@Serializable
data class RecordWastagePayload(
    val ingredientId: String,
    val quantity: String,
    val reason: String
)

@Serializable
data class IngredientDto(
    val id: String = "",
    val name: String = "",
    val description: String? = null,
    @Serializable(with = BigDecimalAsStringSerializer::class)
    val costPerUnit: BigDecimal = BigDecimal.ZERO,
    val unit: String = "",
    val isActive: Boolean = true,
    val createdAt: String = ""
)

@Serializable
data class RecipeDto(
    val id: String = "",
    val name: String = "",
    val description: String? = null,
    val menuItemId: String = "",
    val isActive: Boolean = true,
    val createdAt: String = "",
    val recipeLines: List<RecipeLineDto> = emptyList()
)

@Serializable
data class RecipeLineDto(
    val id: String = "",
    val ingredientId: String = "",
    val ingredientName: String = "",
    @Serializable(with = BigDecimalAsStringSerializer::class)
    val quantityPerItem: BigDecimal = BigDecimal.ZERO,
    val unit: String = ""
)

@Serializable
data class CreateRecipeRequest(
    val name: String,
    val description: String? = null,
    val menuItemId: String,
    val lines: List<CreateRecipeLineRequest>
)

@Serializable
data class CreateRecipeLineRequest(
    val ingredientId: String,
    @Serializable(with = BigDecimalAsStringSerializer::class)
    val quantityPerItem: BigDecimal
)

@Serializable
data class UpdateRecipeRequest(
    val name: String,
    val description: String? = null,
    val menuItemId: String,
    val lines: List<UpdateRecipeLineRequest>
)

@Serializable
data class UpdateRecipeLineRequest(
    val ingredientId: String,
    @Serializable(with = BigDecimalAsStringSerializer::class)
    val quantityPerItem: BigDecimal
)

@Serializable
data class CreateIngredientRequest(
    val name: String,
    val description: String? = null,
    @Serializable(with = BigDecimalAsStringSerializer::class)
    val costPerUnit: BigDecimal,
    val unit: String,
    val isActive: Boolean = true
)

@Serializable
data class UpdateIngredientRequest(
    val name: String,
    val description: String? = null,
    @Serializable(with = BigDecimalAsStringSerializer::class)
    val costPerUnit: BigDecimal,
    val unit: String,
    val isActive: Boolean
)

// Catalog
@Serializable
data class CategoryDto(
    val id: String,
    val name: String,
    val nameAr: String? = null,
    val description: String?,
    val displayOrder: Int,
    val isActive: Boolean,
    val updatedAt: String? = null,
    val isDeleted: Boolean? = null
)

@Serializable
data class MenuItemDto(
    val id: String,
    val categoryId: String,
    val name: String,
    val nameAr: String? = null,
    val description: String?,
    val descriptionAr: String? = null,
    @Serializable(with = BigDecimalAsStringSerializer::class)
    val price: BigDecimal,
    val imageUrl: String?,
    val isActive: Boolean,
    @SerialName("hasAddOns")
    val hasAddOns: Boolean = false,
    val updatedAt: String? = null,
    val isDeleted: Boolean? = null,
    val version: String? = null
)

@Serializable
data class AddOnDto(
    val id: String,
    val name: String,
    @Serializable(with = BigDecimalAsStringSerializer::class)
    val price: BigDecimal,
    val isActive: Boolean,
    val updatedAt: String? = null,
    val isDeleted: Boolean? = null,
    val version: String? = null
)

// Customers
@Serializable
data class CustomerDto(
    val id: String,
    val firstName: String,
    val lastName: String,
    val email: String?,
    val phone: String?,
    val address: String?,
    val notes: String?,
    val allergies: String?,
    val isActive: Boolean,
    val lastVisitDate: String? = null,
    val totalOrders: Int,
    val isDeleted: Boolean? = null,
    val deletedAt: String? = null,
    val version: String? = null
)

@Serializable
data class CreateCustomerRequest(
    val firstName: String,
    val lastName: String,
    val email: String?,
    val phone: String?,
    val address: String?,
    val notes: String?,
    val allergies: String?
)

@Serializable
data class UpdateCustomerRequest(
    val firstName: String,
    val lastName: String,
    val email: String?,
    val phone: String?,
    val address: String?,
    val notes: String?,
    val allergies: String?,
    val version: String
)

// Orders
@Serializable
data class OrderDto(
    val id: String,
    val orderNumber: String?,
    val orderType: String,
    val status: String,
    val customerId: String?,
    val shiftId: String?,
    @Serializable(with = BigDecimalAsStringSerializer::class)
    val subTotal: BigDecimal,
    @Serializable(with = BigDecimalAsStringSerializer::class)
    val discount: BigDecimal,
    @Serializable(with = BigDecimalAsStringSerializer::class)
    val tax: BigDecimal,
    @Serializable(with = BigDecimalAsStringSerializer::class)
    val total: BigDecimal,
    val notes: String?,
    val customerPhone: String? = null,
    val createdAt: String,
    val receiptNumber: String?,
    val syncedAt: String?,
    val orderLines: List<OrderLineDto>? = null
)

@Serializable
data class OrderLineDto(
    val id: String,
    val orderId: String,
    val menuItemId: String,
    val quantity: Int,
    @Serializable(with = BigDecimalAsStringSerializer::class)
    val unitPrice: BigDecimal,
    @Serializable(with = BigDecimalAsStringSerializer::class)
    val totalPrice: BigDecimal,
    val notes: String?,
    val kdsStatus: Int = 0 // KdsStatus: 0=Pending,1=InProgress,2=Done,3=Cancelled
)

// KDS
@Serializable
data class KdsStationDto(
    val id: String,
    val name: String,
    val nameAr: String? = null,
    val color: String? = null,
    val displayOrder: Int = 0,
    val isActive: Boolean = true,
    val branchId: String? = null,
    val menuItemCount: Int = 0
)

@Serializable
data class KdsTicketDto(
    val orderLineId: String,
    val orderId: String,
    val orderNumber: String,
    val orderType: String,
    val menuItemName: String,
    val menuItemNameAr: String? = null,
    @Serializable(with = BigDecimalAsStringSerializer::class)
    val quantity: BigDecimal,
    val notes: String? = null,
    val addOns: List<String> = emptyList(),
    val kdsStatus: Int,         // 0=Pending,1=InProgress,2=Done,3=Cancelled
    val orderCreatedAt: String,
    val kdsAcknowledgedAt: String? = null,
    val kdsDoneAt: String? = null,
    val elapsedMinutes: Int = 0,
    val stationId: String? = null,
    val stationName: String? = null,
    val stationNameAr: String? = null,
    val stationColor: String? = null
)

@Serializable
data class UpdateKdsStatusRequest(
    val status: Int              // 0=Pending,1=InProgress,2=Done,3=Cancelled
)

@Serializable
data class OrderLineAddOnDto(
    val id: String,
    val orderLineId: String,
    val addOnId: String,
    @Serializable(with = BigDecimalAsStringSerializer::class)
    val price: BigDecimal
)

@Serializable
data class CreateOrderRequest(
    val orderType: String,
    val customerId: String?,
    val tableId: String? = null,
    val lines: List<CreateOrderLineRequest>,
    val notes: String?
)

@Serializable
data class CreateOrderLineRequest(
    val menuItemId: String,
    val quantity: Int,
    val addOns: List<CreateOrderLineAddOnRequest>?
)

@Serializable
data class CreateOrderLineAddOnRequest(
    val addOnId: String
)

@Serializable
data class FinalizeOrderRequest(
    val payments: List<PaymentDto>
)

@Serializable
data class VoidOrderRequest(val reason: String)

@Serializable
data class PaymentDto(
    val paymentMethod: String,
    @Serializable(with = BigDecimalAsStringSerializer::class)
    val amount: BigDecimal,
    val transactionId: String? = null,
    val notes: String? = null
)

// Employee scheduling & time tracking (distinct from cash register shifts)
@Serializable
data class EmployeeScheduleDto(
    val id: String = "",
    val userId: String = "",
    val userFullName: String = "",
    val userRole: String = "",
    val branchId: String = "",
    val scheduledStart: String = "",   // ISO-8601 UTC
    val scheduledEnd: String = "",
    val position: String? = null,
    val notes: String? = null,
    val status: String = "",
    val durationMinutes: Int = 0
)

@Serializable
data class WeeklyScheduleDto(
    val weekStart: String = "",
    val weekEnd: String = "",
    val schedules: List<EmployeeScheduleDto> = emptyList()
)

@Serializable
data class TimeEntryDto(
    val id: String = "",
    val userId: String = "",
    val userFullName: String = "",
    val branchId: String = "",
    val clockIn: String = "",
    val clockOut: String? = null,
    val breakMinutes: Int? = null,
    val workedMinutes: Int? = null,
    val notes: String? = null,
    val status: String = ""
)

@Serializable
data class ClockInRequest(
    val branchId: String
)

@Serializable
data class ClockOutRequest(
    val branchId: String,
    val breakMinutes: Int? = null,
    val notes: String? = null
)

// Shifts
@Serializable
data class ShiftDto(
    val id: String,
    val shiftNumber: String,
    val startTime: String,
    val endTime: String?,
    val status: String,
    @Serializable(with = BigDecimalAsStringSerializer::class)
    val openingBalance: BigDecimal,
    @Serializable(with = BigDecimalAsStringSerializer::class)
    val closingBalance: BigDecimal?,
    @Serializable(with = BigDecimalAsStringSerializer::class)
    val expectedCash: BigDecimal?,
    @Serializable(with = BigDecimalAsStringSerializer::class)
    val actualCash: BigDecimal?,
    @Serializable(with = BigDecimalAsStringSerializer::class)
    @SerialName("cashDifference")
    val cashDifference: BigDecimal?,
    val notes: String? = null,
    val userId: String,
    val createdAt: String
)

@Serializable
data class OpenShiftRequest(
    @Serializable(with = BigDecimalAsStringSerializer::class)
    val openingBalance: BigDecimal,
    val notes: String? = null,
    val branchId: String? = null
)

@Serializable
data class CashDropRequest(
    @Serializable(with = BigDecimalAsStringSerializer::class)
    val amount: BigDecimal,
    @SerialName("note")
    val note: String? = null
)

@Serializable
data class CloseShiftRequest(
    @Serializable(with = BigDecimalAsStringSerializer::class)
    @SerialName("closingCount")
    val closingCount: BigDecimal,
    @SerialName("note")
    val note: String? = null
)

// Sync
@Serializable
data class SyncPullRequest(
    val since: String?,
    val limit: Int,
    val token: String?
)

@Serializable
data class SyncPullResponse(
    val cursor: String = "",
    val nextToken: String? = null,
    val hasMore: Boolean = false,
    val upserts: SyncUpserts = SyncUpserts(),
    val deletes: SyncDeletes = SyncDeletes()
)

// Only entity types the PullWorker materializes locally are declared here. Backend also returns
// settings/ingredients/recipes in upserts; those are skipped via ignoreUnknownKeys (no local tables).
@Serializable
data class SyncUpserts(
    val categories: List<CategoryDto>? = null,
    val items: List<MenuItemDto>? = null,
    val addOns: List<AddOnDto>? = null,
    val customers: List<CustomerDto>? = null
)

@Serializable
data class SyncDeletes(
    val items: List<String>? = null,
    val customers: List<String>? = null
)

@Serializable
data class SyncPushRequest(
    val operations: List<OutboxOperationDto>
)

@Serializable
data class OutboxOperationDto(
    val operationId: String,
    val type: String,
    val payload: String,
    val createdAt: String
)

@Serializable
data class SyncPushResponse(
    val results: List<OperationResultDto>
)

@Serializable
data class OperationResultDto(
    val operationId: String,
    val success: Boolean,
    val conflict: Boolean,
    val serverEntity: String?,
    val error: String?
)

// Pagination
@Serializable
data class PaginatedResponseDto<T>(
    val items: List<T>,
    val totalCount: Int,
    val pageNumber: Int,
    val pageSize: Int,
    val totalPages: Int,
    val hasNextPage: Boolean
)

// Table Management
@Serializable
data class FloorPlanDto(
    val id: String = "",
    val name: String = "",
    val description: String? = null,
    val displayOrder: Int = 0,
    val isActive: Boolean = true,
    val tables: List<TableDto> = emptyList()
)

@Serializable
data class TableDto(
    val id: String = "",
    val tableNumber: String = "",
    val label: String? = null,
    val capacity: Int = 2,
    val status: Int = 0,
    val isActive: Boolean = true,
    val displayOrder: Int = 0,
    val floorPlanId: String = "",
    val floorPlanName: String = "",
    val activeOrderId: String? = null
)

@Serializable
data class UpdateTableStatusRequest(val status: Int)

// Printing
@Serializable
data class PrintReceiptRequest(
    val orderId: String,
    val lineWidth: Int = 42
)

@Serializable
data class PrintReceiptResponse(
    val content: String,
    val format: String = "text"
)

// Loyalty
@Serializable
data class LoyaltyAccountDto(
    val id: String = "",
    val customerId: String = "",
    val customerName: String = "",
    val pointBalance: Int = 0,
    val lifetimePointsEarned: Int = 0,
    val lifetimePointsRedeemed: Int = 0,
    val isActive: Boolean = true,
    val createdAt: String = "",
    val recentTransactions: List<LoyaltyTransactionDto> = emptyList()
)

@Serializable
data class LoyaltyTransactionDto(
    val id: String = "",
    val points: Int = 0,
    val transactionType: Int = 0,
    val orderId: String? = null,
    val notes: String? = null,
    val createdAt: String = ""
)

@Serializable
data class EarnPointsRequest(val points: Int, val orderId: String? = null, val notes: String? = null)

@Serializable
data class RedeemPointsRequest(val points: Int, val orderId: String? = null)

// Loyalty CRM — analytics, segmentation, campaigns
@Serializable
data class CustomerAnalyticsDto(
    val totalCustomers: Int = 0,
    val activeLast30Days: Int = 0,
    val atRiskCount: Int = 0,
    val lapsedCount: Int = 0,
    val newCount: Int = 0,
    val vipCount: Int = 0,
    @Serializable(with = BigDecimalAsStringSerializer::class)
    val averageOrderValue: BigDecimal = BigDecimal.ZERO,
    @Serializable(with = BigDecimalAsStringSerializer::class)
    val averagePointBalance: BigDecimal = BigDecimal.ZERO,
    val topCustomers: List<TopCustomerDto> = emptyList()
)

@Serializable
data class TopCustomerDto(
    val customerId: String = "",
    val name: String = "",
    val phone: String? = null,
    val lifetimePointsEarned: Int = 0,
    val pointBalance: Int = 0,
    val totalOrders: Int = 0,
    val lastVisitDate: String? = null,
    val segment: Int = 4 // CustomerSegment enum value (0=VIP,1=Active,2=AtRisk,3=Lapsed,4=New)
)

@Serializable
data class SendCampaignRequest(
    val name: String,
    val message: String,
    val targetSegment: Int
)

@Serializable
data class LoyaltyCampaignDto(
    val id: String = "",
    val name: String = "",
    val message: String = "",
    val targetSegment: Int = 0,
    val recipientCount: Int = 0,
    val sentCount: Int = 0,
    val status: Int = 0, // 0=Pending,1=Sending,2=Completed,3=Failed
    val createdAt: String = "",
    val sentAt: String? = null,
    val errorMessage: String? = null
)

/**
 * Paginated segment response. Field names mirror the backend JSON shape
 * (items/total/page/pageSize/totalPages/hasNextPage) and all carry defaults
 * so deserialization is resilient.
 */
@Serializable
data class CustomerSegmentPageDto(
    val items: List<TopCustomerDto> = emptyList(),
    val total: Int = 0,
    val page: Int = 1,
    val pageSize: Int = 50,
    val totalPages: Int = 0,
    val hasNextPage: Boolean = false
)

// Dashboard / Sales Summary
@Serializable
data class SalesSummaryDto(
    val from: String = "",
    val to: String = "",
    @Serializable(with = BigDecimalAsStringSerializer::class)
    val netSales: BigDecimal = BigDecimal.ZERO,
    @Serializable(with = BigDecimalAsStringSerializer::class)
    val taxCollected: BigDecimal = BigDecimal.ZERO,
    @Serializable(with = BigDecimalAsStringSerializer::class)
    val stampDutyCollected: BigDecimal = BigDecimal.ZERO,
    @Serializable(with = BigDecimalAsStringSerializer::class)
    val grossSales: BigDecimal = BigDecimal.ZERO,
    val ordersCount: Int = 0,
    val voidedOrdersCount: Int = 0,
    @Serializable(with = BigDecimalAsStringSerializer::class)
    val voidRate: BigDecimal = BigDecimal.ZERO,
    @Serializable(with = BigDecimalAsStringSerializer::class)
    val avgTicket: BigDecimal = BigDecimal.ZERO,
    @Serializable(with = BigDecimalAsStringSerializer::class)
    val avgItemsPerOrder: BigDecimal = BigDecimal.ZERO,
    val dineInCount: Int = 0,
    val takeawayCount: Int = 0,
    val deliveryCount: Int = 0,
    val paymentMethods: List<PaymentMethodSummaryDto> = emptyList(),
    val topItems: List<EodTopItemDto> = emptyList(),
    val hourlySales: List<HourlySalesDto> = emptyList()
)

@Serializable
data class PaymentMethodSummaryDto(
    val method: String = "",
    val currency: String = "USD",
    @Serializable(with = BigDecimalAsStringSerializer::class)
    val amount: BigDecimal = BigDecimal.ZERO,
    val count: Int = 0
)

@Serializable
data class HourlySalesDto(
    val hour: Int = 0,
    @Serializable(with = BigDecimalAsStringSerializer::class)
    val sales: BigDecimal = BigDecimal.ZERO,
    val orders: Int = 0
)

// Demand forecast (Smart Suggestions)
@Serializable
data class DemandForecastDto(
    val targetDate: String = "",
    val dayOfWeekName: String = "",
    val weeksOfDataUsed: Int = 0,
    val confidence: String = "Low",
    val estimatedOrders: Int = 0,
    @Serializable(with = BigDecimalAsStringSerializer::class)
    val estimatedRevenue: BigDecimal = BigDecimal.ZERO,
    val timeBlocks: List<TimeBlockForecastDto> = emptyList(),
    val topItems: List<ItemForecastDto> = emptyList(),
    val ingredientDemands: List<IngredientDemandDto> = emptyList(),
    val insufficientDataMessage: String? = null
)

@Serializable
data class TimeBlockForecastDto(
    val startHour: Int = 0,
    val label: String = "",
    val estimatedOrders: Int = 0,
    @Serializable(with = BigDecimalAsStringSerializer::class)
    val estimatedSales: BigDecimal = BigDecimal.ZERO,
    val isPeakBlock: Boolean = false
)

@Serializable
data class ItemForecastDto(
    val menuItemId: String = "",
    val name: String = "",
    val nameAr: String? = null,
    @Serializable(with = BigDecimalAsStringSerializer::class)
    val avgQty: BigDecimal = BigDecimal.ZERO,
    @Serializable(with = BigDecimalAsStringSerializer::class)
    val estimatedQty: BigDecimal = BigDecimal.ZERO
)

@Serializable
data class IngredientDemandDto(
    val ingredientId: String = "",
    val name: String = "",
    val nameAr: String? = null,
    val unit: String = "",
    @Serializable(with = BigDecimalAsStringSerializer::class)
    val estimatedQty: BigDecimal = BigDecimal.ZERO
)

// Kitchen performance analytics
@Serializable
data class KdsPerformanceDto(
    val from: String = "",
    val to: String = "",
    val totalTickets: Int = 0,
    val avgAcknowledgeSeconds: Double = 0.0,
    val p90AcknowledgeSeconds: Double = 0.0,
    val avgPrepSeconds: Double = 0.0,
    val p90PrepSeconds: Double = 0.0,
    val avgTotalTicketSeconds: Double = 0.0,
    val p90TotalTicketSeconds: Double = 0.0,
    val avgThroughputPerHour: Double = 0.0,
    val peakThroughputHour: Int? = null,
    val peakThroughputCount: Int? = null,
    val hourlyBreakdown: List<KdsHourlyDto> = emptyList(),
    val itemBreakdown: List<KdsItemPerformanceDto> = emptyList()
)

@Serializable
data class KdsHourlyDto(
    val hour: Int = 0,
    val ticketsCompleted: Int = 0,
    val avgTotalTicketSeconds: Double = 0.0
)

@Serializable
data class KdsItemPerformanceDto(
    val menuItemId: String = "",
    val name: String = "",
    val nameAr: String? = null,
    val ticketCount: Int = 0,
    val avgPrepSeconds: Double = 0.0,
    val p90PrepSeconds: Double = 0.0
)

// Kiosk / public menu
@Serializable
data class PublicMenuDto(
    val businessName: String = "",
    val businessNameAr: String? = null,
    val address: String? = null,
    val phone: String? = null,
    val currency: String = "USD",
    @Serializable(with = BigDecimalAsStringSerializer::class)
    val exchangeRateLbpPerUsd: BigDecimal = BigDecimal.ZERO,
    val categories: List<PublicMenuCategoryDto> = emptyList()
)

@Serializable
data class PublicMenuCategoryDto(
    val id: String = "",
    val name: String = "",
    val nameAr: String? = null,
    val description: String? = null,
    val displayOrder: Int = 0,
    val items: List<PublicMenuItemDto> = emptyList()
)

@Serializable
data class PublicMenuItemDto(
    val id: String = "",
    val name: String = "",
    val nameAr: String? = null,
    val description: String? = null,
    val descriptionAr: String? = null,
    @Serializable(with = BigDecimalAsStringSerializer::class)
    val price: BigDecimal = BigDecimal.ZERO,
    val imageUrl: String? = null,
    val displayOrder: Int = 0
)

@Serializable
data class KioskOrderRequest(
    val customerName: String? = null,
    val notes: String? = null,
    val lines: List<KioskOrderLineRequest>
)

@Serializable
data class KioskOrderLineRequest(
    val menuItemId: String,
    val quantity: Int,
    @Serializable(with = BigDecimalAsStringSerializer::class)
    val unitPrice: BigDecimal
)

@Serializable
data class KioskOrderResultDto(
    val orderId: String = "",
    val orderNumber: String = "",
    @Serializable(with = BigDecimalAsStringSerializer::class)
    val totalAmount: BigDecimal = BigDecimal.ZERO,
    val currency: String = "USD",
    val message: String = ""
)

// Delivery
@Serializable
data class DeliveryDto(
    val id: String = "",
    val orderId: String = "",
    val orderNumber: String? = null,
    @Serializable(with = BigDecimalAsStringSerializer::class)
    val orderTotal: BigDecimal = BigDecimal.ZERO,
    val customerName: String? = null,
    val deliveryAddress: String = "",
    val apartmentDetails: String? = null,
    val customerPhone: String? = null,
    val channel: Int = 0,
    val channelName: String = "",
    val status: Int = 0,
    val statusName: String = "",
    @Serializable(with = BigDecimalAsStringSerializer::class)
    val deliveryFee: BigDecimal = BigDecimal.ZERO,
    val estimatedMinutes: Int? = null,
    val notes: String? = null,
    val driverName: String? = null,
    val driverPhone: String? = null,
    val assignedAt: String? = null,
    val pickedUpAt: String? = null,
    val deliveredAt: String? = null,
    val branchId: String? = null,
    val externalOrderId: String? = null,
    val externalOrderReference: String? = null,
    val createdAt: String = ""
)

@Serializable
data class CreateDeliveryInfoRequest(
    val orderId: String,
    val deliveryAddress: String,
    val apartmentDetails: String? = null,
    val customerPhone: String? = null,
    val channel: Int = 0,
    @Serializable(with = BigDecimalAsStringSerializer::class)
    val deliveryFee: BigDecimal = BigDecimal.ZERO,
    val estimatedMinutes: Int? = null,
    val notes: String? = null,
    val branchId: String? = null
)

@Serializable
data class UpdateDeliveryStatusRequest(
    val status: Int,
    val driverName: String? = null,
    val driverPhone: String? = null
)

// Reservations
@Serializable
data class ReservationDto(
    val id: String = "",
    val customerName: String = "",
    val customerPhone: String? = null,
    val partySize: Int = 2,
    val reservationDateTime: String = "",
    val notes: String? = null,
    val status: Int = 0,
    val statusName: String = "",
    val tableId: String? = null,
    val tableNumber: String? = null,
    val floorPlanName: String? = null,
    val branchId: String? = null,
    val createdAt: String = ""
)

@Serializable
data class CreateReservationRequest(
    val customerName: String,
    val customerPhone: String? = null,
    val partySize: Int = 2,
    val reservationDateTime: String,
    val notes: String? = null,
    val tableId: String? = null,
    val branchId: String? = null
)

@Serializable
data class UpdateReservationStatusRequest(
    val status: Int,
    val tableId: String? = null
)

@Serializable
data class AvailableTableDto(
    val id: String = "",
    val tableNumber: String = "",
    val label: String? = null,
    val capacity: Int = 2,
    val floorPlan: String = ""
)

// Feedback
@Serializable
data class SubmitFeedbackRequest(
    val rating: Int,
    val comment: String? = null,
    val category: Int = 0,
    val orderId: String? = null,
    val orderNumber: String? = null,
    val customerName: String? = null,
    val branchId: String? = null
)

@Serializable
data class FeedbackDto(
    val id: String = "",
    val rating: Int = 0,
    val comment: String? = null,
    val category: Int = 0,
    val categoryName: String = "",
    val orderId: String? = null,
    val orderNumber: String? = null,
    val customerName: String? = null,
    val branchId: String? = null,
    val createdAt: String = ""
)

@Serializable
data class FeedbackSummaryDto(
    val totalCount: Int = 0,
    val averageRating: Double = 0.0,
    val fiveStars: Int = 0,
    val fourStars: Int = 0,
    val threeStars: Int = 0,
    val twoStars: Int = 0,
    val oneStar: Int = 0,
    val complaints: Int = 0,
    val recent: List<FeedbackDto> = emptyList()
)

// Branches
@Serializable
data class BranchDto(
    val id: String = "",
    val name: String = "",
    val address: String? = null,
    val phone: String? = null,
    val isActive: Boolean = true,
    val isDefault: Boolean = false,
    val displayOrder: Int = 0,
    val createdAt: String = ""
)

@Serializable
data class CreateBranchRequest(
    val name: String,
    val address: String? = null,
    val phone: String? = null,
    val isDefault: Boolean = false,
    val displayOrder: Int = 0
)

// Menu Engineering
@Serializable
data class MenuEngineeringReportDto(
    val from: String = "",
    val to: String = "",
    val totalOrders: Int = 0,
    val items: List<MenuEngineeringItemDto> = emptyList()
)

@Serializable
data class MenuEngineeringItemDto(
    val menuItemId: String = "",
    val name: String = "",
    val categoryName: String = "",
    val unitsSold: Int = 0,
    @Serializable(with = BigDecimalAsStringSerializer::class)
    val popularityIndex: BigDecimal = BigDecimal.ZERO,
    val isHighPopularity: Boolean = false,
    @Serializable(with = BigDecimalAsStringSerializer::class)
    val revenue: BigDecimal = BigDecimal.ZERO,
    @Serializable(with = BigDecimalAsStringSerializer::class)
    val costOfGoods: BigDecimal = BigDecimal.ZERO,
    @Serializable(with = BigDecimalAsStringSerializer::class)
    val contributionMargin: BigDecimal = BigDecimal.ZERO,
    @Serializable(with = BigDecimalAsStringSerializer::class)
    val contributionMarginPct: BigDecimal = BigDecimal.ZERO,
    val isHighMargin: Boolean = false,
    val category: Int = 3
)

// Accounting integrations
@Serializable
data class AccountingConnectionStatusDto(
    val provider: String,
    val isConnected: Boolean = false,
    val companyName: String? = null,
    val lastSyncAt: String? = null,
    val lastSyncError: String? = null,
    val syncRecordCount: Int = 0
)

@Serializable
data class QuickBooksConnectResponse(
    val authorizationUrl: String = ""
)

@Serializable
data class SyncTriggerResponse(
    val synced: Int = 0,
    val errors: List<String> = emptyList()
)

// Open API / webhook integrations (Step 110)
@Serializable
data class WebhookSubscriptionDto(
    val id: String,
    val name: String,
    val endpointUrl: String,
    val isActive: Boolean,
    val branchId: String? = null,
    val events: List<String> = emptyList(),
    val createdAt: String,
    val lastDeliveryAt: String? = null,
    val lastDeliverySucceeded: Boolean? = null
)

@Serializable
data class ApiKeyDto(
    val id: String,
    val name: String,
    val keyPrefix: String,
    val isActive: Boolean,
    val expiresAt: String? = null,
    val lastUsedAt: String? = null,
    val createdAt: String
)

// Supplier intelligence (Step 111)
@Serializable
data class SupplierIntelligenceDto(
    val generatedAt: String,
    val forecastDays: Int,
    val confidence: String,
    val lowStockAlerts: List<LowStockAlertDto> = emptyList(),
    val orderSuggestions: List<SupplierOrderSuggestionDto> = emptyList(),
    val totalIngredientsAnalysed: Int = 0,
    val totalSuggestedLines: Int = 0,
    val totalEstimatedCost: Double = 0.0
)

@Serializable
data class LowStockAlertDto(
    val ingredientId: String,
    val name: String,
    val unit: String,
    val currentStock: Double,
    val minimumStock: Double,
    val deficit: Double,
    val supplierName: String? = null
)

@Serializable
data class SupplierOrderSuggestionDto(
    val supplierId: String? = null,
    val supplierName: String,
    val supplierPhone: String? = null,
    val supplierEmail: String? = null,
    val lines: List<OrderLineSuggestionDto> = emptyList(),
    val totalEstimatedCost: Double = 0.0
)

@Serializable
data class OrderLineSuggestionDto(
    val ingredientId: String,
    val name: String,
    val unit: String,
    val currentStock: Double,
    val minimumStock: Double,
    val projectedUsage: Double,
    val suggestedQty: Double,
    val unitCost: Double,
    val estimatedCost: Double,
    val isLowStock: Boolean = false
)

@Serializable
data class CreateSuggestedOrdersRequest(
    val forecastDays: Int = 7,
    val branchId: String? = null,
    val supplierIds: List<String>? = null
)

@Serializable
data class CreateSuggestedOrdersResult(
    val ordersCreated: Int = 0,
    val orderIds: List<String> = emptyList(),
    val orderNumbers: List<String> = emptyList(),
    val skippedReason: String? = null
)
