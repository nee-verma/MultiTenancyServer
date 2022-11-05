﻿using System;
using System.Threading;
using System.Threading.Tasks;
using IdentityServer4.EntityFramework.Entities;
using IdentityServer4.EntityFramework.Extensions;
using IdentityServer4.EntityFramework.Interfaces;
using IdentityServer4.EntityFramework.Options;
using IdentityServerWithAspIdAndEF.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MultiTenancyServer;
using MultiTenancyServer.EntityFramework;
using MultiTenancyServer.Options;

namespace IdentityServerWithAspIdAndEF.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole, long>,
        IConfigurationDbContext, IPersistedGrantDbContext,
        ITenantDbContext<ApplicationTenant, long>
    {
        private static TenancyModelState<long> _tenancyModelState;
        private readonly ITenancyContext<ApplicationTenant> _tenancyContext;        
        private readonly ILogger _logger;

        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options,
            ITenancyContext<ApplicationTenant> tenancyContext,
            ILogger<ApplicationDbContext> logger)
            : base(options)
        {
            _tenancyContext = tenancyContext;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public DbSet<Client> Clients { get; set; }

        public DbSet<IdentityResource> IdentityResources { get; set; }

        public DbSet<ApiResource> ApiResources { get; set; }

        public DbSet<PersistedGrant> PersistedGrants { get; set; }

        public DbSet<DeviceFlowCodes> DeviceFlowCodes { get; set; }

        public DbSet<ApplicationTenant> Tenants { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // Customize the ASP.NET Identity model and override the defaults if needed.
            // For example, you can rename the ASP.NET Identity table names and more.
            // Add your customizations after calling base.OnModelCreating(builder);

            var configurationStoreOptions = new ConfigurationStoreOptions();
            builder.ConfigureClientContext(configurationStoreOptions);
            builder.ConfigureResourcesContext(configurationStoreOptions);

            var operationalStoreOptions = new OperationalStoreOptions();
            builder.ConfigurePersistedGrantContext(operationalStoreOptions);

            // MultiTenancyServer configuration.
            var tenantStoreOptions = new TenantStoreOptions();
            builder.ConfigureTenantContext<ApplicationTenant, string>(tenantStoreOptions);

            var tenantReferenceOptions = new TenantReferenceOptions();
            builder.HasTenancy<string>(tenantReferenceOptions, out _tenancyModelState);

            builder.Entity<ApplicationTenant>(b =>
            {
                b.Property(t => t.DisplayName).HasMaxLength(256);
            });

            // Configure properties on User (ASP.NET Core Identity).
            builder.Entity<ApplicationUser>(b =>
            {
                // Add multi-tenancy support to entity.
                b.HasTenancy(() => _tenancyContext.Tenant.Id, _tenancyModelState, hasIndex: false);
                // Remove unique index on NormalizedUserName.
                b.HasIndex(u => u.NormalizedUserName).HasDatabaseName("UserNameIndex").IsUnique(false);
                // Add unique index on TenantId and NormalizedUserName.
                b.HasIndex(tenantReferenceOptions.ReferenceName, nameof(ApplicationUser.NormalizedUserName))
                    .HasDatabaseName("TenantUserNameIndex").IsUnique();
            });

            // Configure properties on Role (ASP.NET Core Identity).
            builder.Entity<IdentityRole>(b =>
            {
                // Add multi-tenancy support to entity.
                b.HasTenancy(() => _tenancyContext.Tenant.Id, _tenancyModelState, hasIndex: false);
                // Remove unique index on NormalizedName.
                b.HasIndex(r => r.NormalizedName).HasDatabaseName("RoleNameIndex").IsUnique(false);
                // Add unique index on TenantId and NormalizedName.
                b.HasIndex(tenantReferenceOptions.ReferenceName, nameof(IdentityRole.NormalizedName))
                    .HasDatabaseName("TenantRoleNameIndex").IsUnique();
            });

            builder.Entity<Client>(b =>
            {
                b.HasTenancy(() => _tenancyContext.Tenant.Id, _tenancyModelState, hasIndex: false);
                b.HasIndex(c => c.ClientId).IsUnique(false);
                b.HasIndex(tenantReferenceOptions.ReferenceName, nameof(Client.ClientId)).IsUnique();
            });

            builder.Entity<IdentityResource>(b =>
            {
                b.HasTenancy(() => _tenancyContext.Tenant.Id, _tenancyModelState, hasIndex: false);
                b.HasIndex(r => r.Name).IsUnique(false);
                b.HasIndex(tenantReferenceOptions.ReferenceName, nameof(IdentityResource.Name)).IsUnique();
            });

            builder.Entity<ApiResource>(b =>
            {
                b.HasTenancy(() => _tenancyContext.Tenant.Id, _tenancyModelState, hasIndex: false);
                b.HasIndex(r => r.Name).IsUnique(false);
                b.HasIndex(tenantReferenceOptions.ReferenceName, nameof(ApiResource.Name)).IsUnique();
            });

            builder.Entity<ApiScope>(b =>
            {
                b.HasTenancy(() => _tenancyContext.Tenant.Id, _tenancyModelState, hasIndex: false);
                b.HasIndex(s => s.Name).IsUnique(false);
                b.HasIndex(tenantReferenceOptions.ReferenceName, nameof(ApiScope.Name)).IsUnique();
            });

            builder.Entity<PersistedGrant>(b =>
            {
                b.HasTenancy(() => _tenancyContext.Tenant.Id, _tenancyModelState, indexNameFormat: $"IX_{nameof(PersistedGrant)}_{{0}}");
            });

            builder.Entity<DeviceFlowCodes>(b =>
            {
                b.HasTenancy(() => _tenancyContext.Tenant.Id, _tenancyModelState, hasIndex: false);
                b.HasIndex(c => c.DeviceCode).IsUnique(false);
                b.HasIndex(tenantReferenceOptions.ReferenceName, nameof(IdentityServer4.EntityFramework.Entities.DeviceFlowCodes.DeviceCode)).IsUnique();
            });
        }

        public Task<int> SaveChangesAsync()
        {
            return base.SaveChangesAsync();
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            this.EnsureTenancy(_tenancyContext?.Tenant?.Id, _tenancyModelState);
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            this.EnsureTenancy(_tenancyContext?.Tenant?.Id, _tenancyModelState);
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
    }
}
