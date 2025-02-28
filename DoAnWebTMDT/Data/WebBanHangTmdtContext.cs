using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace DoAnWebTMDT.Data;

public partial class WebBanHangTmdtContext : DbContext
{
    public WebBanHangTmdtContext()
    {
    }

    public WebBanHangTmdtContext(DbContextOptions<WebBanHangTmdtContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Account> Accounts { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Chat> Chats { get; set; }

    public virtual DbSet<GioHang> GioHangs { get; set; }

    public virtual DbSet<GuestCart> GuestCarts { get; set; }

    public virtual DbSet<GuestLikeList> GuestLikeLists { get; set; }

    public virtual DbSet<LikeList> LikeLists { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderDetail> OrderDetails { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<RedeemPoint> RedeemPoints { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Data Source=DESKTOP-SL8KBFN\\SQLEXPRESS02;Initial Catalog=WebBanHangTMDT;Integrated Security=True;Trust Server Certificate=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.AccountId).HasName("PK__Account__349DA5869914D61D");

            entity.ToTable("Account");

            entity.HasIndex(e => e.Username, "UQ__Account__536C85E4F19EA2E4").IsUnique();

            entity.HasIndex(e => e.Email, "UQ__Account__A9D1053444F74FDE").IsUnique();

            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.OTP)
                .HasMaxLength(6)
                .HasColumnName("OTP");
            entity.Property(e => e.OTP_Expiry)
                .HasColumnType("datetime")
                .HasColumnName("OTP_Expiry");
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.Phone).HasMaxLength(15);
            entity.Property(e => e.RewardPoints).HasDefaultValue(0);
            entity.Property(e => e.Role)
                .HasMaxLength(20)
                .HasDefaultValue("User");
            entity.Property(e => e.Username).HasMaxLength(50);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__Category__19093A2B1E4BB214");

            entity.ToTable("Category");

            entity.HasIndex(e => e.Name, "UQ__Category__737584F62EC0D02F").IsUnique();

            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.MediaPath).HasMaxLength(500);
            entity.Property(e => e.Name).HasMaxLength(255);
        });

        modelBuilder.Entity<Chat>(entity =>
        {
            entity.HasKey(e => e.ChatId).HasName("PK__Chat__A9FBE626EFDD4542");

            entity.ToTable("Chat");

            entity.Property(e => e.ChatId).HasColumnName("ChatID");
            entity.Property(e => e.ReceiverId).HasColumnName("ReceiverID");
            entity.Property(e => e.SenderId).HasColumnName("SenderID");
            entity.Property(e => e.SentAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Receiver).WithMany(p => p.ChatReceivers)
                .HasForeignKey(d => d.ReceiverId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Chat__ReceiverID__00200768");

            entity.HasOne(d => d.Sender).WithMany(p => p.ChatSenders)
                .HasForeignKey(d => d.SenderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Chat__SenderID__7F2BE32F");
        });

        modelBuilder.Entity<GioHang>(entity =>
        {
            entity.HasKey(e => e.CartId).HasName("PK__GioHang__51BCD79744597926");

            entity.ToTable("GioHang");

            entity.Property(e => e.CartId).HasColumnName("CartID");
            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ProductId).HasColumnName("ProductID");

            entity.HasOne(d => d.Account).WithMany(p => p.GioHangs)
                .HasForeignKey(d => d.AccountId)
                .HasConstraintName("FK__GioHang__Account__5FB337D6");

            entity.HasOne(d => d.Product).WithMany(p => p.GioHangs)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK__GioHang__Product__60A75C0F");
        });

        modelBuilder.Entity<GuestCart>(entity =>
        {
            entity.HasKey(e => e.GuestCartId).HasName("PK__GuestCar__42B88B7606069314");

            entity.ToTable("GuestCart");

            entity.Property(e => e.GuestCartId).HasColumnName("GuestCartID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.SessionId)
                .HasMaxLength(100)
                .HasColumnName("SessionID");

            entity.HasOne(d => d.Product).WithMany(p => p.GuestCarts)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK__GuestCart__Produ__656C112C");
        });

        modelBuilder.Entity<GuestLikeList>(entity =>
        {
            entity.HasKey(e => e.GuestLikeId).HasName("PK__GuestLik__9DD124CBA7E7CE43");

            entity.ToTable("GuestLikeList");

            entity.Property(e => e.GuestLikeId).HasColumnName("GuestLikeID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.SessionId)
                .HasMaxLength(100)
                .HasColumnName("SessionID");

            entity.HasOne(d => d.Product).WithMany(p => p.GuestLikeLists)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK__GuestLike__Produ__0A9D95DB");
        });

        modelBuilder.Entity<LikeList>(entity =>
        {
            entity.HasKey(e => e.LikeId).HasName("PK__LikeList__A2922CF4AC39B7D1");

            entity.ToTable("LikeList");

            entity.Property(e => e.LikeId).HasColumnName("LikeID");
            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ProductId).HasColumnName("ProductID");

            entity.HasOne(d => d.Account).WithMany(p => p.LikeLists)
                .HasForeignKey(d => d.AccountId)
                .HasConstraintName("FK__LikeList__Accoun__7A672E12");

            entity.HasOne(d => d.Product).WithMany(p => p.LikeLists)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK__LikeList__Produc__7B5B524B");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("PK__Orders__C3905BAF265446A8");

            entity.ToTable(tb => tb.HasTrigger("trg_UpdateRewardPoints"));

            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.GuestEmail).HasMaxLength(100);
            entity.Property(e => e.GuestPhone).HasMaxLength(15);
            entity.Property(e => e.OrderStatus)
                .HasMaxLength(20)
                .HasDefaultValue("Pending");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Account).WithMany(p => p.Orders)
                .HasForeignKey(d => d.AccountId)
                .HasConstraintName("FK__Orders__AccountI__6B24EA82");
        });

        modelBuilder.Entity<OrderDetail>(entity =>
        {
            entity.HasKey(e => e.OrderDetailId).HasName("PK__OrderDet__D3B9D30C062FB0E1");

            entity.ToTable("OrderDetail");

            entity.Property(e => e.OrderDetailId).HasColumnName("OrderDetailID");
            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.Price).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ProductId).HasColumnName("ProductID");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderDetails)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK__OrderDeta__Order__6EF57B66");

            entity.HasOne(d => d.Product).WithMany(p => p.OrderDetails)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__OrderDeta__Produ__6FE99F9F");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PK__Payment__9B556A5800F42817");

            entity.ToTable("Payment");

            entity.Property(e => e.PaymentId).HasColumnName("PaymentID");
            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.PaymentMethod).HasMaxLength(50);
            entity.Property(e => e.PaymentStatus)
                .HasMaxLength(20)
                .HasDefaultValue("Pending");
            entity.Property(e => e.TransactionDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Order).WithMany(p => p.Payments)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Payment__OrderID__76969D2E");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.ProductId).HasName("PK__Product__B40CC6ED69F45743");

            entity.ToTable("Product", tb => tb.HasTrigger("trg_UpdateNewPrice"));

            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.DiscountPercent)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(5, 2)");
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(500)
                .HasColumnName("ImageURL");
            entity.Property(e => e.MediaPath).HasMaxLength(500);
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.NewPrice)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 2)");
            entity.Property(e => e.OldPrice).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Category).WithMany(p => p.Products)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("FK__Product__Categor__59FA5E80");
        });

        modelBuilder.Entity<RedeemPoint>(entity =>
        {
            entity.HasKey(e => e.RedeemId).HasName("PK__RedeemPo__C9E468F736B13690");

            entity.Property(e => e.RedeemId).HasColumnName("RedeemID");
            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.RedeemDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Account).WithMany(p => p.RedeemPoints)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__RedeemPoi__Accou__05D8E0BE");

            entity.HasOne(d => d.Product).WithMany(p => p.RedeemPoints)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__RedeemPoi__Produ__06CD04F7");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
