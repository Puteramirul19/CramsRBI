using CramsRBIApp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace CramsRBIApp.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, int>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<UserAccount> UserAccount { get; set; }  //add table dlm database
        public DbSet<Permission> Permissions { get; set; }   // ADD THIS LINE
        //public DbSet<RolePermission> RolePermissions { get; set; }   // CHANGED FROM Permissions to RolePermissions

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Existing code...
            builder.Entity<ApplicationUser>().ToTable("Users");
            builder.Entity<ApplicationRole>().ToTable("Roles");
            builder.Entity<IdentityUserRole<int>>().ToTable("UserRoles");
            builder.Entity<IdentityUserClaim<int>>().ToTable("UserClaims");
            builder.Entity<IdentityUserLogin<int>>().ToTable("UserLogins");
            builder.Entity<IdentityRoleClaim<int>>().ToTable("RoleClaims");
            builder.Entity<IdentityUserToken<int>>().ToTable("UserTokens");

            // Add Permission table configuration
            builder.Entity<Permission>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.MenuName).IsRequired().HasMaxLength(50);
                entity.Property(e => e.PermissionType).IsRequired().HasMaxLength(20);
                entity.HasOne(e => e.Role)
                      .WithMany()
                      .HasForeignKey(e => e.RoleId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Create unique index to prevent duplicate permissions
                entity.HasIndex(e => new { e.RoleId, e.MenuName, e.PermissionType })
                      .IsUnique()
                      .HasDatabaseName("IX_Permission_RoleId_MenuName_PermissionType");
            });
        }

        //4 step to add a table
        //1. Create a model class
        //2. Add DB set
        //3. add-migration AddDiaryEntryTable   >> tool menu / Nuget Package Manager/ Package manager console
        //4. update-database
    }
}