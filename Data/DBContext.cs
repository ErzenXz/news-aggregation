using Microsoft.EntityFrameworkCore;
using NewsAggregation.Models;
using NewsAggregation.Models.Security;
using System.Collections.Generic;
using System.Xml.Linq;
using News_aggregation.Entities;
using NewsAggregation.Models.Stats;


namespace NewsAggregation.Data
{
    public class DBContext : DbContext
    {
        public DBContext(DbContextOptions options) : base(options) {}

        public DbSet<User> Users { get; set; }
        public DbSet<Article> Articles { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<UserPreference> UserPreferences { get; set; }
        public DbSet<Source> Sources { get; set; }
        public DbSet<ArticleStats> ArticleStats { get; set; }
        public DbSet<Bookmark> Bookmarks { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Ads> Ads { get; set; }
        public DbSet<Plans> Plans { get; set; }
        public DbSet<Subscriptions> Subscriptions { get; set; }



        // Security
        public DbSet<AccountSecurity> accountSecurity { get; set; }
        public DbSet<IpMitigations> ipMitigations { get; set; }
        public DbSet<ResetEmail> resetEmails { get; set; }
        public DbSet<PasswordChanges> passwordChanges { get; set; }
        public DbSet<AuthLogs> authLogs { get; set; }
        public DbSet<RefreshTokens> refreshTokens { get; set; }
        public DbSet<VerifyEmail> verifyEmails { get; set; }





        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
            modelBuilder.Entity<User>().HasIndex(u => u.Username).IsUnique();
            modelBuilder.Entity<User>().HasIndex(u => u.Id).IsUnique();
            modelBuilder.Entity<User>().HasIndex(u => u.TotpSecret).IsUnique();

           // modelBuilder.Entity<UserPreference>().HasIndex(u => u.UserId).IsUnique();
           // modelBuilder.Entity<UserPreference>().HasIndex(u => u.Id).IsUnique();

        }

    }

}
