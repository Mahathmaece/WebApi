using Microsoft.EntityFrameworkCore;
using WebApi.Entities;

using WebApi.Models.Messaging;

using WebApi.Models.Users;

using WebApi.Models.BusinessAreas;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System;

namespace WebApi.Helpers
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<UserModel> UserModels { get; set; }
        public DbSet<UserCustodianModel> UserCustodianModels { get; set; }
        public DbSet<UserJoinUserRole> UserJoinUserRoles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<UserRoleJoinUserRight> UserRoleJoinUserRights { get; set; }
        public DbSet<UserRight> UserRights { get; set; }
        public DbSet<CostCenter> CostCenters { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<BusinessArea> BusinessAreas { get; set; }
        public DbSet<BusinessAreaModel> BusinessAreaModels { get; set; }
        public DbSet<Location> Locations { get; set; }

        public DbSet<Mail> Mail { get; set; }
        public DbSet<MailModel> MailModel { get; set; }
        public DbSet<MailAttachment> MailAttachments { get; set; }
        public DbSet<ModulePageJoinUserRight> ModulePageJoinUserRights { get; set; }
        public DbSet<ModulePage> ModulePages { get; set; }
        public DbSet<Module> Modules { get; set; }



        public DbSet<UserSavedToggleColumn> UserSavedToggleColumns { get; set; }
        public DbSet<DefaultToggleColumn> DefaultToggleColumns { get; set; }
        public DbSet<UserSavedFilter> UserSavedFilters { get; set; }

        public DbSet<UserSession> UserSessions { get; set; }

        // linkage between Business Area & Cost Center (only for assets cost center)
        public DbSet<BusinessAreaJoinCostCenter> BusinessAreaJoinCostCenters { get; set; }

        //XL 20191209 Task# SAI-29 Add Fluent API to Transaction to make sure TransactionCode uniqueness
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            modelBuilder.Entity<BusinessAreaModel>().ToView(null);
            modelBuilder.Entity<MailModel>().HasNoKey();

            modelBuilder.Entity<UserModel>().ToView(null);
            modelBuilder.Entity<UserCustodianModel>().ToView(null);

            modelBuilder.Entity<AppSettings>().HasNoKey().ToView(null);

            modelBuilder.Entity<BusinessAreaJoinCostCenter>().HasNoKey();
            // add unique constraint
            modelBuilder.Entity<BusinessAreaJoinCostCenter>().HasIndex(p => new { p.BusinessAreaID, p.CostCenterID }).IsUnique();
        }
    }
}