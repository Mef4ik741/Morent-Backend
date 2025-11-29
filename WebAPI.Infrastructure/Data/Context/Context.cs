using Microsoft.EntityFrameworkCore;
using WebAPI.Domain.Models;
using WebAPI.Infrastructure.Data.Configurations;

namespace WebAPI.Infrastructure.Data.Context;

public class Context : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<BalanceTransaction> BalanceTransactions { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<Car> Cars { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; }
    public DbSet<ChatConversation> ChatConversations { get; set; }
    public DbSet<Favorites> Favorites { get; set; }
    public DbSet<CarBooking> CarBookings { get; set; }
    public DbSet<RentNotification> RentNotifications { get; set; }
    
    public Context(DbContextOptions<Context> options) : base(options) { }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new UserConfig());
        modelBuilder.ApplyConfiguration(new RoleConfig());
        modelBuilder.ApplyConfiguration(new UserRoleConfig());
        modelBuilder.ApplyConfiguration(new CarConfig());
        modelBuilder.ApplyConfiguration(new ChatMessageConfig());
        modelBuilder.ApplyConfiguration(new ChatConversationConfig());
        modelBuilder.ApplyConfiguration(new FavoritesConfig());
        modelBuilder.ApplyConfiguration(new CarBookingConfig());
        modelBuilder.ApplyConfiguration(new RentNotificationConfig());
        modelBuilder.ApplyConfiguration(new ReviewConfig());
        
        modelBuilder.Entity<User>()
            .Property(u => u.Rank)
            .HasConversion<string>();
    }
}