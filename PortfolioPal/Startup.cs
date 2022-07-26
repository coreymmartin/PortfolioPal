using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data;
using MySql.Data.MySqlClient;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System.IO;

namespace PortfolioPal
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
            services.AddScoped<IDbConnection>((s) =>
            {
                IDbConnection conn = new MySqlConnection(Configuration.GetConnectionString("portfoliopal"));
                conn.Open();
                return conn;
            });

            // services.AddTransient<IPotentialRepo, PotentialRepo>();
            // services.AddTransient<IOrderRepo, OrderRepo>();
            // services.AddTransient<IAssetRepo, AssetRepo>();
            // services.AddTransient<IPortfolioRepo, PortfolioRepo>();
            // services.AddTransient<IConfigRepo, ConfigRepo>();

            services.AddScoped<IPotentialRepo, PotentialRepo>();
            services.AddScoped<IOrderRepo, OrderRepo>();
            services.AddScoped<IAssetRepo, AssetRepo>();
            services.AddScoped<IPortfolioRepo, PortfolioRepo>();
            services.AddScoped<IConfigRepo, ConfigRepo>();
            services.AddControllersWithViews();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{symbol?}");
            });
        }
    }
}
