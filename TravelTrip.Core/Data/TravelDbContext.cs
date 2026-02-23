using Microsoft.EntityFrameworkCore;
using TravelTrip.Core.Models;

namespace TravelTrip.Core.Data;

public class TravelDbContext : DbContext
{
    public virtual DbSet<TripEntity> Trips { get; set; }
    public virtual DbSet<UserEntity> Users { get; set; }
    public virtual DbSet<BookingEntity> Bookings { get; set; }
    public virtual DbSet<BookingItemEntity> BookingItems { get; set; } 
    public virtual DbSet<CartEntity> Carts { get; set; }
    public virtual DbSet<CartItemEntity> CartItems { get; set; }
    public virtual DbSet<ChatHistoryEntity> ChatHistories { get; set; }
    public virtual DbSet<AdminAuditLogEntity> AdminAuditLogs { get; set; }
    public virtual DbSet<PasswordResetTokenEntity> PasswordResetTokens { get; set; }
    public virtual DbSet<SearchRankingLogEntity> SearchRankingLogs { get; set; }

    // 用於 Mocking 的無參數建構子
    protected TravelDbContext() { }

    public TravelDbContext(DbContextOptions<TravelDbContext> options)
        : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // ⚠️ 僅在「沒有透過 DI 設定」時才使用
        if (!optionsBuilder.IsConfigured)
        {
            // optionsBuilder.UseNpgsql(
            //     "Host=localhost;Database=traveltrip_db;Username=admin;Password=admin123",
            //     o => o.UseVector()
            // );
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var isInMemory =
            Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory";

        /*
         * ===============================
         * PostgreSQL 專屬設定（正式環境）
         * ===============================
         */
        if (!isInMemory)
        {
            modelBuilder.HasPostgresExtension("vector");
        }

        modelBuilder.Entity<TripEntity>(entity =>
        {
            entity.ToTable("trips_trip");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasColumnName("description").IsRequired();
            entity.Property(e => e.Price).HasColumnName("price").HasColumnType("decimal(10,2)");
            entity.Property(e => e.Image).HasColumnName("image").HasMaxLength(500);
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.AvailableSeats).HasColumnName("available_seats");
            entity.Property(e => e.Location).HasColumnName("location").HasMaxLength(100);
            entity.Property(e => e.Duration).HasColumnName("duration");
            entity.Property(e => e.Country).HasColumnName("country").HasMaxLength(100);
            entity.Property(e => e.City).HasColumnName("city").HasMaxLength(100);
            entity.Property(e => e.Keywords).HasColumnName("keywords").HasMaxLength(200);

            // ⭐ 關鍵差異點
            if (isInMemory)
            {
                // 測試環境：InMemory 不支援 Vector
                entity.Ignore(e => e.Embedding);
            }
            else
            {
                // 正式環境：PostgreSQL + pgvector
                entity.Property(e => e.Embedding)
                      .HasColumnName("embedding")
                      .HasColumnType("vector(384)");
            }
        });

        modelBuilder.Entity<UserEntity>(entity =>
        {
            entity.ToTable("accounts_customuser");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).HasColumnName("email").IsRequired();
            entity.HasIndex(e => e.Email).IsUnique();
        });

        modelBuilder.Entity<BookingEntity>(entity =>
        {
            entity.ToTable("bookings_booking");
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                  .WithMany(u => u.Bookings)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BookingItemEntity>(entity =>
        {
            entity.ToTable("bookings_bookingitem");
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Booking)
                  .WithMany(b => b.BookingItems)
                  .HasForeignKey(e => e.BookingId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Trip)
                  .WithMany()
                  .HasForeignKey(e => e.TripId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CartEntity>(entity =>
        {
            entity.ToTable("cart_cart");
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CartItemEntity>(entity =>
        {
            entity.ToTable("cart_cartitem");
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Cart)
                  .WithMany(c => c.CartItems)
                  .HasForeignKey(e => e.CartId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Trip)
                  .WithMany()
                  .HasForeignKey(e => e.TripId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ChatHistoryEntity>(entity =>
        {
            entity.ToTable("chatbot_chathistory");
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(e => e.SessionId);
        });

        modelBuilder.Entity<AdminAuditLogEntity>(entity =>
        {
            entity.ToTable("admin_audit_logs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.HasOne(e => e.Admin)
                  .WithMany(u => u.AuditLogs)
                  .HasForeignKey(e => e.AdminId)
                  .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.AdminId);
            entity.HasIndex(e => e.EntityType);
        });

        modelBuilder.Entity<PasswordResetTokenEntity>(entity =>
        {
            entity.ToTable("password_reset_tokens");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Token).IsRequired().HasMaxLength(100);
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasIndex(e => e.ExpiresAt);
            entity.HasIndex(e => e.UserId);
        });
    }
}
