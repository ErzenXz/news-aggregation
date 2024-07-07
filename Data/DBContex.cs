using Microsoft.EntityFrameworkCore;
using NewsAggregation.Models;
using NewsAggregation.Models.Security;
using System.Collections.Generic;
using System.Xml.Linq;


namespace NewsAggregation.Data
{
    public class DBContext : DbContext
    {
        public DBContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<AccountSecurity> accountSecurity { get; set; }
        public DbSet<IpMitigations> ipMitigations { get; set; }
        public DbSet<ResetEmail> resetEmails { get; set; }
    }
}
