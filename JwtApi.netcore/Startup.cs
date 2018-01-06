using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using JwtApi.netcore.Data;
using JwtApi.netcore.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using AspNetCoreRateLimit;

namespace JwtApi.netcore
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
            services.AddOptions();
            services.AddMemoryCache();

            //configure ip rate limiting middle-ware
            services.Configure<IpRateLimitOptions>(Configuration.GetSection("IpRateLimiting"));
            services.Configure<IpRateLimitPolicies>(Configuration.GetSection("IpRateLimitPolicies"));
            services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
            services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();

            HandleDatabaseChoices(services, Configuration);

            services.AddIdentity<ApplicationUser, IdentityRole>(o =>
            {
                o.Password.RequireDigit = true;
                o.Password.RequireLowercase = true;
                o.Password.RequireUppercase = true;
                o.Password.RequireNonAlphanumeric = true;
                o.Password.RequiredLength = 8;
            })
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();

            services.AddAuthentication()
                .AddJwtBearer(cfg =>
                {
                    cfg.RequireHttpsMetadata = false;
                    cfg.SaveToken = true;

                    cfg.TokenValidationParameters = new TokenValidationParameters()
                    {
                        ValidIssuer = Configuration["Tokens:Issuer"],
                        ValidAudience = Configuration["Tokens:Issuer"],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["Tokens:Key"]))
                    };

                });

            services.AddCors(options =>
            {
                options.AddPolicy("Cors",
                    builder => builder.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials());
            });

            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseIpRateLimiting();

            app.UseStaticFiles();

            app.UseAuthentication();

            app.UseCors("Cors");

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");

                routes.MapSpaFallbackRoute(
                    name: "spa-fallback",
                    defaults: new { controller = "Home", action = "Index" });
            });
        }

        private static void HandleDatabaseChoices(IServiceCollection services, IConfiguration configuration)
        {
            var database = Environment.GetEnvironmentVariable("ASPNETCORE_DATABASE");

            if (string.IsNullOrEmpty(database))
            {
                /**
                 * This is where we configure the database without args. If we didn't add an argument for
                 * the database, and we haven't set the evironment variable, You can choose here what
                 * The server will use. Sql Server is the default. You will also want to let it set the
                 * environment variable for the appropriate database for configuring the context
                */

                // SQL Server -- Comment out these lines if you'd like to not use SQL Server

                services.AddEntityFrameworkSqlServer().AddDbContext<AppDbContext>(options =>
                    options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
                Environment.SetEnvironmentVariable("ASPNETCORE_DATABASE", "MSSQL");

                // MySQL -- Uncomment to use Entity Framework with MySql

                //services.AddEntityFrameworkMySql().AddDbContext<AppDbContext>(options =>
                //    options.UseMySql(configuration.GetConnectionString("MySqlConnection")));
                //Environment.SetEnvironmentVariable("ASPNETCORE_DATABASE", "MYSQL");

                //PostgreSQL -- Uncomment to use Entity Framework with PostgreSQL

                //services.AddEntityFrameworkNpgsql().AddEntityFrameworkMySql().AddDbContext<AppDbContext>(options =>
                //    options.UseNpgsql(configuration.GetConnectionString("MySqlConnection")));
                //Environment.SetEnvironmentVariable("ASPNETCORE_DATABASE", "NPGSQL");

                return;
            }

            switch (database)
            {
                case "MSSQL":
                    services.AddEntityFrameworkSqlServer().AddDbContext<AppDbContext>(options =>
                        options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
                    break;
                case "MYSQL":
                    services.AddEntityFrameworkMySql().AddDbContext<AppDbContext>(options =>
                        options.UseMySql(configuration.GetConnectionString("MySqlConnection")));
                    break;
                case "NPGSQL":
                    services.AddEntityFrameworkNpgsql().AddEntityFrameworkMySql().AddDbContext<AppDbContext>(options =>
                        options.UseNpgsql(configuration.GetConnectionString("MySqlConnection")));
                    break;
            }
        }
    }
}
