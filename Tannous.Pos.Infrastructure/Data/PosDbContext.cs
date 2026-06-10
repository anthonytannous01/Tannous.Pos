using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Common.ValueObjects;
using Tannous.Pos.Domain.Enums;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Infrastructure.Data;

public class PosDbContext : DbContext
{
    public PosDbContext(DbContextOptions<PosDbContext> options) : base(options)
    {
    }

    public DbSet<Branch> Branches { get; set; }
    public DbSet<DeliveryInfo> DeliveryInfos { get; set; }
    public DbSet<FeedbackSubmission> FeedbackSubmissions { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<MenuItem> MenuItems { get; set; }
    public DbSet<AddOn> AddOns { get; set; }
    public DbSet<Ingredient> Ingredients { get; set; }
    public DbSet<Recipe> Recipes { get; set; }
    public DbSet<RecipeLine> RecipeLines { get; set; }
    public DbSet<Supplier> Suppliers { get; set; }
    public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
    public DbSet<PurchaseOrderLine> PurchaseOrderLines { get; set; }
    public DbSet<GoodsReceipt> GoodsReceipts { get; set; }
    public DbSet<GoodsReceiptLine> GoodsReceiptLines { get; set; }
    public DbSet<InventoryItem> InventoryItems { get; set; }
    public DbSet<InventoryMovement> InventoryMovements { get; set; }
    public DbSet<WastageRecord> WastageRecords { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderLine> OrderLines { get; set; }
    public DbSet<OrderLineAddOn> OrderLineAddOns { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<PaymentRefund> PaymentRefunds { get; set; }
    public DbSet<Shift> Shifts { get; set; }
    public DbSet<CashDrawerEvent> CashDrawerEvents { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<LoyaltyAccount> LoyaltyAccounts { get; set; }
    public DbSet<LoyaltyTransaction> LoyaltyTransactions { get; set; }
    public DbSet<LoyaltyCampaign> LoyaltyCampaigns { get; set; }
    public DbSet<FloorPlan> FloorPlans { get; set; }
    public DbSet<Table> Tables { get; set; }
    public DbSet<Reservation> Reservations { get; set; }
    public DbSet<Device> Devices { get; set; }
    public DbSet<Printer> Printers { get; set; }
    public DbSet<BusinessSettings> BusinessSettings { get; set; }
    public DbSet<ReceiptSequence> ReceiptSequences { get; set; }
    public DbSet<PriceChangeLog> PriceChangeLogs { get; set; }
    public DbSet<IdempotentRequest> IdempotentRequests { get; set; }
    public DbSet<SyncCursor> SyncCursors { get; set; }
    public DbSet<AuditEvent> AuditEvents { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<SyncOperationReceipt> SyncOperationReceipts { get; set; }
    public DbSet<SyncConflictRecord> SyncConflictRecords { get; set; }
    public DbSet<OperationalAuditRecord> OperationalAuditRecords { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure decimal properties
        modelBuilder.Entity<MenuItem>()
            .Property(m => m.Price)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<AddOn>()
            .Property(a => a.Price)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Ingredient>()
            .Property(i => i.CostPerUnit)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Order>()
            .Property(o => o.SubTotal)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Order>()
            .Property(o => o.TaxAmount)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Order>()
            .Property(o => o.DiscountAmount)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Order>()
            .Property(o => o.TotalAmount)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Order>()
            .Property(o => o.AmountTendered)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Order>()
            .Property(o => o.ChangeDue)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Order>()
            .Property(o => o.NetCapturedAmount)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<OrderLine>()
            .Property(ol => ol.UnitPrice)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<OrderLine>()
            .Property(ol => ol.TotalPrice)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<OrderLine>()
            .Property(ol => ol.KdsStatus)
            .HasDefaultValue(KdsStatus.Pending)
            .HasConversion<int>();

        modelBuilder.Entity<OrderLineAddOn>()
            .Property(ola => ola.Price)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Payment>()
            .Property(p => p.Amount)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Payment>()
            .Property(p => p.AmountInUsd)
            .HasColumnType("decimal(18,4)");

        modelBuilder.Entity<Payment>()
            .Property(p => p.ExchangeRateUsed)
            .HasColumnType("decimal(18,4)");

        modelBuilder.Entity<Payment>()
            .Property(p => p.TenderedCurrency)
            .HasMaxLength(8)
            .HasDefaultValue("USD");

        modelBuilder.Entity<Order>()
            .Property(o => o.StampDutyAmount)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0m);

        modelBuilder.Entity<BusinessSettings>()
            .Property(bs => bs.ExchangeRateLbpPerUsd)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0m);

        modelBuilder.Entity<BusinessSettings>()
            .Property(bs => bs.StampDutyAmountUsd)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(2.00m);

        modelBuilder.Entity<BusinessSettings>()
            .Property(bs => bs.LoyaltyPointValueUsd)
            .HasColumnType("decimal(18,4)")
            .HasDefaultValue(0.01m);

        // ── Loyalty ──────────────────────────────────────────────────────────
        modelBuilder.Entity<LoyaltyAccount>()
            .HasIndex(la => la.CustomerId)
            .IsUnique();

        modelBuilder.Entity<LoyaltyAccount>()
            .HasOne(la => la.Customer)
            .WithOne()
            .HasForeignKey<LoyaltyAccount>(la => la.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<LoyaltyTransaction>()
            .HasOne(lt => lt.LoyaltyAccount)
            .WithMany(la => la.Transactions)
            .HasForeignKey(lt => lt.LoyaltyAccountId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<LoyaltyTransaction>()
            .Property(lt => lt.TransactionType)
            .HasConversion<int>();

        modelBuilder.Entity<LoyaltyTransaction>()
            .HasIndex(lt => lt.LoyaltyAccountId);

        modelBuilder.Entity<LoyaltyTransaction>()
            .HasIndex(lt => lt.OrderId);

        // ── Loyalty Campaigns ────────────────────────────────────────────────
        modelBuilder.Entity<LoyaltyCampaign>()
            .Property(c => c.Name)
            .HasMaxLength(100);

        modelBuilder.Entity<LoyaltyCampaign>()
            .Property(c => c.Message)
            .HasMaxLength(500);

        modelBuilder.Entity<LoyaltyCampaign>()
            .Property(c => c.TargetSegment)
            .HasConversion<int>();

        modelBuilder.Entity<LoyaltyCampaign>()
            .Property(c => c.Status)
            .HasConversion<int>();

        modelBuilder.Entity<LoyaltyCampaign>()
            .HasIndex(c => c.CreatedAt);

        // ── Tables ───────────────────────────────────────────────────────────
        modelBuilder.Entity<Table>()
            .Property(t => t.Status)
            .HasConversion<int>()
            .HasDefaultValue(TableStatus.Available);

        modelBuilder.Entity<Table>()
            .HasOne(t => t.FloorPlan)
            .WithMany(fp => fp.Tables)
            .HasForeignKey(t => t.FloorPlanId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Table>()
            .HasIndex(t => t.FloorPlanId);

        modelBuilder.Entity<Order>()
            .HasOne(o => o.Table)
            .WithMany(t => t.Orders)
            .HasForeignKey(o => o.TableId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        modelBuilder.Entity<Order>()
            .HasIndex(o => o.TableId);

        modelBuilder.Entity<PaymentRefund>()
            .Property(r => r.Amount)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<PaymentRefund>()
            .Property(r => r.Reason)
            .HasMaxLength(512);

        modelBuilder.Entity<PaymentRefund>()
            .Property(r => r.CorrelationId)
            .HasMaxLength(256);

        modelBuilder.Entity<Shift>()
            .Property(s => s.OpeningBalance)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Shift>()
            .Property(s => s.ClosingBalance)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Shift>()
            .Property(s => s.ExpectedCash)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Shift>()
            .Property(s => s.ActualCash)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Shift>()
            .Property(s => s.CashDifference)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<CashDrawerEvent>()
            .Property(cde => cde.Amount)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<InventoryItem>()
            .Property(ii => ii.AverageCost)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<InventoryMovement>()
            .Property(im => im.UnitCost)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<InventoryMovement>()
            .Property(im => im.TotalCost)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<WastageRecord>()
            .Property(wr => wr.UnitCost)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<WastageRecord>()
            .Property(wr => wr.TotalCost)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<PriceChangeLog>()
            .Property(pcl => pcl.OldPrice)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<PriceChangeLog>()
            .Property(pcl => pcl.NewPrice)
            .HasColumnType("decimal(18,2)");

        // Configure relationships
        modelBuilder.Entity<MenuItem>()
            .HasOne(m => m.Category)
            .WithMany(c => c.MenuItems)
            .HasForeignKey(m => m.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MenuItem>()
            .HasMany(m => m.AddOns)
            .WithMany(a => a.MenuItems)
            .UsingEntity(j => j.ToTable("MenuItemAddOns"));

        modelBuilder.Entity<Recipe>()
            .HasOne(r => r.MenuItem)
            .WithMany(m => m.Recipes)
            .HasForeignKey(r => r.MenuItemId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RecipeLine>()
            .HasOne(rl => rl.Recipe)
            .WithMany(r => r.RecipeLines)
            .HasForeignKey(rl => rl.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RecipeLine>()
            .HasOne(rl => rl.Ingredient)
            .WithMany(i => i.RecipeLines)
            .HasForeignKey(rl => rl.IngredientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PurchaseOrder>()
            .HasOne(po => po.Supplier)
            .WithMany(s => s.PurchaseOrders)
            .HasForeignKey(po => po.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PurchaseOrderLine>()
            .HasOne(pol => pol.PurchaseOrder)
            .WithMany(po => po.Lines)
            .HasForeignKey(pol => pol.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PurchaseOrderLine>()
            .HasOne(pol => pol.Ingredient)
            .WithMany()
            .HasForeignKey(pol => pol.IngredientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<GoodsReceipt>()
            .HasOne(gr => gr.PurchaseOrder)
            .WithMany(po => po.GoodsReceipts)
            .HasForeignKey(gr => gr.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<GoodsReceiptLine>()
            .HasOne(grl => grl.GoodsReceipt)
            .WithMany(gr => gr.Lines)
            .HasForeignKey(grl => grl.GoodsReceiptId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GoodsReceiptLine>()
            .HasOne(grl => grl.Ingredient)
            .WithMany()
            .HasForeignKey(grl => grl.IngredientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<InventoryItem>()
            .HasOne(ii => ii.Ingredient)
            .WithMany(i => i.InventoryItems)
            .HasForeignKey(ii => ii.IngredientId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<InventoryMovement>()
            .HasOne(im => im.InventoryItem)
            .WithMany(ii => ii.InventoryMovements)
            .HasForeignKey(im => im.InventoryItemId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<InventoryMovement>()
            .HasOne<InventoryMovement>()
            .WithMany()
            .HasForeignKey(im => im.ReversedMovementId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<WastageRecord>()
            .HasOne(wr => wr.InventoryItem)
            .WithMany()
            .HasForeignKey(wr => wr.InventoryItemId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Order>()
            .HasOne(o => o.Customer)
            .WithMany(c => c.Orders)
            .HasForeignKey(o => o.CustomerId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Order>()
            .HasOne(o => o.Shift)
            .WithMany(s => s.Orders)
            .HasForeignKey(o => o.ShiftId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Order>()
            .HasOne(o => o.User)
            .WithMany(u => u.Orders)
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<OrderLine>()
            .HasOne(ol => ol.Order)
            .WithMany(o => o.OrderLines)
            .HasForeignKey(ol => ol.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<OrderLine>()
            .HasOne(ol => ol.MenuItem)
            .WithMany(m => m.OrderLines)
            .HasForeignKey(ol => ol.MenuItemId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<OrderLineAddOn>()
            .HasOne(ola => ola.OrderLine)
            .WithMany(ol => ol.OrderLineAddOns)
            .HasForeignKey(ola => ola.OrderLineId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<OrderLineAddOn>()
            .HasOne(ola => ola.AddOn)
            .WithMany(a => a.OrderLineAddOns)
            .HasForeignKey(ola => ola.AddOnId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Payment>()
            .HasOne(p => p.Order)
            .WithMany(o => o.Payments)
            .HasForeignKey(p => p.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PaymentRefund>()
            .HasOne(r => r.Order)
            .WithMany(o => o.PaymentRefunds)
            .HasForeignKey(r => r.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PaymentRefund>()
            .HasOne(r => r.OriginalPayment)
            .WithMany()
            .HasForeignKey(r => r.OriginalPaymentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PaymentRefund>()
            .HasIndex(r => r.OrderId)
            .IsUnique();

        modelBuilder.Entity<Shift>()
            .HasOne(s => s.User)
            .WithMany(u => u.Shifts)
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CashDrawerEvent>()
            .HasOne(cde => cde.Shift)
            .WithMany(s => s.CashDrawerEvents)
            .HasForeignKey(cde => cde.ShiftId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Printer>()
            .HasOne(p => p.Device)
            .WithMany(d => d.Printers)
            .HasForeignKey(p => p.DeviceId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<PriceChangeLog>()
            .HasOne(pcl => pcl.MenuItem)
            .WithMany()
            .HasForeignKey(pcl => pcl.MenuItemId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PriceChangeLog>()
            .HasOne(pcl => pcl.User)
            .WithMany()
            .HasForeignKey(pcl => pcl.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure indexes
        modelBuilder.Entity<Order>()
            .HasIndex(o => o.OrderNumber)
            .IsUnique();

        modelBuilder.Entity<Shift>()
            .HasIndex(s => s.ShiftNumber)
            .IsUnique();

        // Optimistic concurrency (PostgreSQL bytea): RowVersion on Order, InventoryItem, Shift — see entity [Timestamp].
        ConfigureByteaRowVersion(modelBuilder.Entity<Order>().Property(o => o.RowVersion));
        ConfigureByteaRowVersion(modelBuilder.Entity<InventoryItem>().Property(ii => ii.RowVersion));
        ConfigureByteaRowVersion(modelBuilder.Entity<Shift>().Property(s => s.RowVersion));

        // Configure User entity with normalized fields
        modelBuilder.Entity<User>()
            .HasIndex(u => u.NormalizedUsername)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.NormalizedEmail)
            .IsUnique()
            .HasFilter("\"NormalizedEmail\" IS NOT NULL");

        // Ensure normalized fields are required/optional as needed
        modelBuilder.Entity<User>()
            .Property(u => u.NormalizedUsername)
            .IsRequired();

        modelBuilder.Entity<User>()
            .Property(u => u.NormalizedEmail)
            .IsRequired(false);

        modelBuilder.Entity<Customer>()
            .HasIndex(c => c.Email)
            .IsUnique();

        modelBuilder.Entity<Customer>()
            .HasIndex(c => c.Phone)
            .IsUnique();

        modelBuilder.Entity<ReceiptSequence>()
            .HasIndex(rs => rs.SequenceType)
            .IsUnique();

        // Configure concurrency tokens for sync conflict handling
        modelBuilder.Entity<Customer>()
            .Property(c => c.Version)
            .IsConcurrencyToken();

        modelBuilder.Entity<MenuItem>()
            .Property(m => m.Version)
            .IsConcurrencyToken();

        modelBuilder.Entity<AddOn>()
            .Property(a => a.Version)
            .IsConcurrencyToken();

        // Configure AuditEvent
        modelBuilder.Entity<AuditEvent>()
            .HasIndex(ae => ae.Utc);

        modelBuilder.Entity<AuditEvent>()
            .HasIndex(ae => new { ae.Entity, ae.EntityId });

        modelBuilder.Entity<AuditEvent>()
            .HasIndex(ae => ae.CorrelationId);

        modelBuilder.Entity<AuditEvent>()
            .HasOne(ae => ae.User)
            .WithMany()
            .HasForeignKey(ae => ae.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Configure RefreshToken
        modelBuilder.Entity<RefreshToken>()
            .HasOne(rt => rt.User)
            .WithMany()
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RefreshToken>()
            .HasIndex(rt => rt.Token)
            .IsUnique();

        modelBuilder.Entity<RefreshToken>()
            .HasIndex(rt => new { rt.UserId, rt.ExpiresAt });

        modelBuilder.Entity<SyncConflictRecord>(entity =>
        {
            entity.HasIndex(e => new { e.DeviceId, e.OperationId, e.ConflictType });
            entity.Property(e => e.DeviceId).HasMaxLength(128);
            entity.Property(e => e.OperationType).HasMaxLength(64);
            entity.Property(e => e.OperationId).HasMaxLength(256);
            entity.Property(e => e.EntityType).HasMaxLength(128);
            entity.Property(e => e.ConflictType).HasMaxLength(64);
            entity.Property(e => e.Reason).HasMaxLength(1024);
            entity.Property(e => e.CorrelationId).HasMaxLength(256);
            entity.Property(e => e.ResolutionNotes).HasMaxLength(512);
            entity.Property(e => e.ResolutionStatus).HasMaxLength(32).HasDefaultValue("Unresolved");
            entity.Property(e => e.ResolvedBy).HasMaxLength(256);
            entity.HasIndex(e => new { e.ResolutionStatus, e.CreatedAtUtc });
        });

        modelBuilder.Entity<OperationalAuditRecord>(entity =>
        {
            entity.HasIndex(e => new { e.OrderId, e.CreatedAtUtc });
            entity.HasIndex(e => new { e.DeviceId, e.OperationId, e.Action });
            entity.HasIndex(e => new { e.EntityType, e.EntityId, e.CreatedAtUtc });
            entity.Property(e => e.Category).HasMaxLength(64);
            entity.Property(e => e.Action).HasMaxLength(128);
            entity.Property(e => e.EntityType).HasMaxLength(128);
            entity.Property(e => e.DeviceId).HasMaxLength(128);
            entity.Property(e => e.OperationId).HasMaxLength(256);
            entity.Property(e => e.CorrelationId).HasMaxLength(256);
            entity.Property(e => e.Severity).HasMaxLength(32);
            entity.Property(e => e.Summary).HasMaxLength(1024);
        });

        modelBuilder.Entity<SyncOperationReceipt>()
            .HasIndex(r => new { r.DeviceId, r.OperationId })
            .IsUnique();

        modelBuilder.Entity<SyncOperationReceipt>()
            .Property(r => r.DeviceId)
            .HasMaxLength(128);

        modelBuilder.Entity<SyncOperationReceipt>()
            .Property(r => r.OperationId)
            .HasMaxLength(256);

        modelBuilder.Entity<SyncOperationReceipt>()
            .Property(r => r.OperationType)
            .HasMaxLength(64);

        // ── Branch ───────────────────────────────────────────────────────────
        modelBuilder.Entity<Branch>()
            .HasIndex(b => b.IsDefault);

        modelBuilder.Entity<Order>()
            .HasOne(o => o.Branch)
            .WithMany(b => b.Orders)
            .HasForeignKey(o => o.BranchId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        modelBuilder.Entity<Order>()
            .HasIndex(o => o.BranchId);

        modelBuilder.Entity<Shift>()
            .HasOne(s => s.Branch)
            .WithMany(b => b.Shifts)
            .HasForeignKey(s => s.BranchId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        modelBuilder.Entity<Shift>()
            .HasIndex(s => s.BranchId);

        modelBuilder.Entity<InventoryItem>()
            .HasOne(ii => ii.Branch)
            .WithMany(b => b.InventoryItems)
            .HasForeignKey(ii => ii.BranchId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        modelBuilder.Entity<InventoryItem>()
            .HasIndex(ii => ii.BranchId);

        modelBuilder.Entity<WastageRecord>()
            .HasOne(wr => wr.Branch)
            .WithMany()
            .HasForeignKey(wr => wr.BranchId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        modelBuilder.Entity<PurchaseOrder>()
            .HasOne(po => po.Branch)
            .WithMany()
            .HasForeignKey(po => po.BranchId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        modelBuilder.Entity<GoodsReceipt>()
            .HasOne(gr => gr.Branch)
            .WithMany()
            .HasForeignKey(gr => gr.BranchId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        modelBuilder.Entity<InventoryMovement>()
            .HasOne(im => im.Branch)
            .WithMany()
            .HasForeignKey(im => im.BranchId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        // ── Reservation ───────────────────────────────────────────────────────
        modelBuilder.Entity<Reservation>()
            .Property(r => r.Status)
            .HasConversion<int>()
            .HasDefaultValue(ReservationStatus.Pending);

        modelBuilder.Entity<Reservation>()
            .Property(r => r.CustomerName)
            .HasMaxLength(100);

        modelBuilder.Entity<Reservation>()
            .Property(r => r.CustomerPhone)
            .HasMaxLength(50);

        modelBuilder.Entity<Reservation>()
            .Property(r => r.Notes)
            .HasMaxLength(500);

        modelBuilder.Entity<Reservation>()
            .HasOne(r => r.Table)
            .WithMany()
            .HasForeignKey(r => r.TableId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        modelBuilder.Entity<Reservation>()
            .HasOne(r => r.Branch)
            .WithMany()
            .HasForeignKey(r => r.BranchId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        modelBuilder.Entity<Reservation>()
            .HasIndex(r => r.ReservationDateTime);

        modelBuilder.Entity<Reservation>()
            .HasIndex(r => r.Status);

        modelBuilder.Entity<Reservation>()
            .HasIndex(r => r.BranchId);

        // ── DeliveryInfo ──────────────────────────────────────────────────────
        modelBuilder.Entity<DeliveryInfo>()
            .Property(d => d.Channel).HasConversion<int>();

        modelBuilder.Entity<DeliveryInfo>()
            .Property(d => d.Status).HasConversion<int>()
            .HasDefaultValue(DeliveryStatus.Pending);

        modelBuilder.Entity<DeliveryInfo>()
            .Property(d => d.DeliveryFee).HasColumnType("decimal(18,2)").HasDefaultValue(0m);

        modelBuilder.Entity<DeliveryInfo>()
            .Property(d => d.DeliveryAddress).HasMaxLength(500);

        modelBuilder.Entity<DeliveryInfo>()
            .Property(d => d.ApartmentDetails).HasMaxLength(200);

        modelBuilder.Entity<DeliveryInfo>()
            .Property(d => d.CustomerPhone).HasMaxLength(50);

        modelBuilder.Entity<DeliveryInfo>()
            .Property(d => d.DriverName).HasMaxLength(100);

        modelBuilder.Entity<DeliveryInfo>()
            .Property(d => d.DriverPhone).HasMaxLength(50);

        modelBuilder.Entity<DeliveryInfo>()
            .Property(d => d.Notes).HasMaxLength(500);

        modelBuilder.Entity<DeliveryInfo>()
            .Property(d => d.ExternalOrderId).HasMaxLength(128);

        modelBuilder.Entity<DeliveryInfo>()
            .Property(d => d.ExternalOrderReference).HasMaxLength(160);

        modelBuilder.Entity<DeliveryInfo>()
            .HasIndex(d => new { d.Channel, d.ExternalOrderId });

        modelBuilder.Entity<DeliveryInfo>()
            .HasOne(d => d.Order)
            .WithOne()
            .HasForeignKey<DeliveryInfo>(d => d.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DeliveryInfo>()
            .HasIndex(d => d.OrderId).IsUnique();

        modelBuilder.Entity<DeliveryInfo>()
            .HasIndex(d => d.Status);

        modelBuilder.Entity<DeliveryInfo>()
            .HasIndex(d => d.BranchId);

        modelBuilder.Entity<DeliveryInfo>()
            .HasOne(d => d.Branch)
            .WithMany()
            .HasForeignKey(d => d.BranchId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        // ── FeedbackSubmission ────────────────────────────────────────────────
        modelBuilder.Entity<FeedbackSubmission>()
            .Property(f => f.Category)
            .HasConversion<int>();

        modelBuilder.Entity<FeedbackSubmission>()
            .Property(f => f.Comment)
            .HasMaxLength(1000);

        modelBuilder.Entity<FeedbackSubmission>()
            .Property(f => f.CustomerName)
            .HasMaxLength(100);

        modelBuilder.Entity<FeedbackSubmission>()
            .Property(f => f.OrderNumber)
            .HasMaxLength(50);

        modelBuilder.Entity<FeedbackSubmission>()
            .HasOne(f => f.Order)
            .WithMany()
            .HasForeignKey(f => f.OrderId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        modelBuilder.Entity<FeedbackSubmission>()
            .HasOne(f => f.Branch)
            .WithMany()
            .HasForeignKey(f => f.BranchId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        modelBuilder.Entity<FeedbackSubmission>()
            .HasIndex(f => f.CreatedAt);

        modelBuilder.Entity<FeedbackSubmission>()
            .HasIndex(f => f.Rating);

        modelBuilder.Entity<FeedbackSubmission>()
            .HasIndex(f => f.BranchId);
    }

    private static void ConfigureByteaRowVersion(
        Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder<byte[]> property)
    {
        property.HasColumnType("bytea").IsConcurrencyToken();
    }
}
