using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MultiTenancyServer.Samples.AspNetIdentityAndEFCore.Data;
using MultiTenancyServer.Samples.AspNetIdentityAndEFCore.Models;
using MultiTenancyServer.Samples.AspNetIdentityAndEFCore.Services;

namespace MultiTenancyServer.Samples.AspNetIdentityAndEFCore
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Central database to manage tenants.
            // You could use the InMemoryTenantStore instead or your own custom store (such as from configuration).
            services.AddDbContext<ManagementDbContext>(options => options
                .UseSqlite(Configuration.GetConnectionString("ManagementConnection"))
                //.EnableSensitiveDataLogging()
                );

            // Application database per tenant.
            services.AddDbContext<ApplicationDbContext>(options => options
                .UseSqlite(Configuration.GetConnectionString("ApplicationConnection"))
                //.EnableSensitiveDataLogging()
                );

            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();


            // Add Multi-Tenancy services.
            services.AddMultiTenancy<ApplicationTenant, string>()
                //.AddChildPathParser("/tenants/")
                //.AddDomainParser()
                .AddSubdomainParser(".tenants.local")
                //.AddRequestParsers(parsers =>
                //{
                //    // To test a domain parser locally, add a similar line 
                //    // to your hosts file for each tenant you want to test
                //    // For Windows: C:\Windows\System32\drivers\etc\hosts
                //    // 127.0.0.1	tenant1.tenants.local
                //    // 127.0.0.1	tenant2.tenants.local
                //    //parsers.AddSubdomainParser(".tenants.local");
                //    parsers.AddChildPathParser("/tenants/");
                //})
                .AddEntityFrameworkStore<ManagementDbContext, ApplicationTenant, string>();

            // Add service to configure an ApplicationDbContext per tenant
            // TODO: this should be replaced with some sort of an IApplicationDbContextFactory
            services.AddTransient<IApplicationDbContextConfigurator, ApplicationDbContextConfigurator>();

            // Add application services.
            services.AddTransient<IEmailSender, EmailSender>();

            services.AddMvc();
            //    .SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                //app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseStaticFiles();

            app.UseMultiTenancy<ApplicationTenant>();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();


            app.UseEndpoints(endpoints =>
            {
                //Works with .AddChildPathParser("/tenants/") for "launchUrl": "tenants/tenant1/Account/Login",
                //endpoints.MapControllerRoute(
                //  name: "areas",
                //  pattern: "tenants/{_tenant_placeholder_}/{area:exists}/{controller=Home}/{action=Index}/{id?}"
                //);

                //endpoints.MapControllerRoute(
                //  name: "default",
                //  pattern: "tenants/{_tenant_placeholder_}/{controller=Home}/{action=Index}/{id?}"
                //);
                //Works with https://tenant1.tenants.local:5001/Account/Login
                endpoints.MapControllerRoute(
                  name: "areas",
                  pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}"
                );
                endpoints.MapControllerRoute(
                  name: "default",
                  pattern: "{controller=Home}/{action=Index}/{id?}"
                );
            });


            //app.UseEndpoints(endpoints =>
            //{
            //    endpoints.MapControllers();
            //});

            //app.UseMvc(routes =>
            //{
            //    routes.MapRoute(
            //        name: "default",
            //        // if using a PathParser, you will need to adjust this to accomodate the tenant paths
            //        template: "tenants/{_tenant_placeholder_}/{controller=Home}/{action=Index}/{id?}");
            //});
        }
    }
}
