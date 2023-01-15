using NewsParser.Core.Domain;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace DataAccess
{
    public class DataContext: DbContext
    {
        public DbSet<Channel> Channel { get; set; } = null!;
        public DbSet<Item> Item { get; set; } = null!;

        public DataContext() {}

        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {
            //Database.EnsureCreated();
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<Item>().Property("IsReaded").HasDefaultValue(false);
            builder.Entity<Item>().HasAlternateKey(u=>u.Link);
            builder.Entity<Item>().HasIndex(u=>u.Description);
            //builder.Entity<Item>().Ignore(u=>u.IsSaved);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
 }
    }
}
