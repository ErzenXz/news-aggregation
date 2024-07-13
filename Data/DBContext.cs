using Microsoft.EntityFrameworkCore;
using NewsAggregation.Models;
using NewsAggregation.Models.Security;
using System.Collections.Generic;
using System.Xml.Linq;
using News_aggregation.Entities;


namespace NewsAggregation.Data
{
    public class DBContext : DbContext
    {
        public DBContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Article> Articles { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<ArticleTag> ArticleTags { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Favorite> Favorites { get; set; }
        public DbSet<UserPreference> UserPreferences { get; set; }

        // Security
        public DbSet<AccountSecurity> accountSecurity { get; set; }
        public DbSet<IpMitigations> ipMitigations { get; set; }
        public DbSet<ResetEmail> resetEmails { get; set; }
        public DbSet<PasswordChanges> passwordChanges { get; set; }
        public DbSet<AuthLogs> authLogs { get; set; }
        public DbSet<RefreshTokens> refreshTokens { get; set; }





        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);


            modelBuilder.Entity<ArticleTag>()
                 .HasKey(at => new { at.ArticleId, at.TagId });

            modelBuilder.Entity<ArticleTag>()
                .HasOne(at => at.Article)
                .WithMany(a => a.ArticleTags)
                .HasForeignKey(at => at.ArticleId);

            modelBuilder.Entity<ArticleTag>()
                .HasOne(at => at.Tag)
                .WithMany(t => t.ArticleTags)
                .HasForeignKey(at => at.TagId);

            modelBuilder.Entity<UserPreference>()
                .HasOne(up => up.Tag)
                .WithMany(t => t.UserPreferences)
                .HasForeignKey(up => up.TagId);
        }

    }

}
