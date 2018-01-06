using JwtApi.netcore.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JwtApi.netcore.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }
        public AppDbContext() { }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // Customize the ASP.NET Identity model and override the defaults if needed.
            // For example, you can rename the ASP.NET Identity table names and more.
            // Add your customizations after calling base.OnModelCreating(builder);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            if (options.IsConfigured == true)
            {
                base.OnConfiguring(options);
            }
            else
            {
                var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
                var database = Environment.GetEnvironmentVariable("ASPNETCORE_DATABASE");

                var configuration = new ConfigurationBuilder()
                    .SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .AddJsonFile($"appsettings.{environmentName}.json", true)
                    .AddEnvironmentVariables();

                var config = configuration.Build();

                string connstr;

                switch (database)
                {
                    case "NPGSQL":
                        connstr = config.GetConnectionString("NpgSqlConnection");
                        if (!string.IsNullOrEmpty(connstr))
                            options.UseNpgsql(connstr);
                        break;
                    case "MYSQL":
                        connstr = config.GetConnectionString("MySqlConnection");
                        if (!string.IsNullOrEmpty(connstr))
                            options.UseMySql(connstr);
                        break;
                    case "MSSQL":
                        connstr = config.GetConnectionString("DefaultConnection");
                        if (!string.IsNullOrEmpty(connstr))
                            options.UseSqlServer(connstr);
                        break;
                    default:
                        connstr = null;
                        break;
                }

                if (string.IsNullOrWhiteSpace(connstr))
                {
                    throw new InvalidOperationException(
                        "Could not find a connection string.");
                }

                base.OnConfiguring(options);
            }
        }
    }
}
