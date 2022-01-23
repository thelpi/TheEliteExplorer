using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TheEliteExplorerCommon;
using TheEliteExplorerDomain.Abstractions;
using TheEliteExplorerDomain.Configuration;
using TheEliteExplorerDomain.Providers;
using TheEliteExplorerInfrastructure;
using TheEliteExplorerInfrastructure.Configuration;
using TheEliteExplorerInfrastructure.Repositories;

namespace TheEliteExplorerUi
{
    public class Startup
    {
        private const string _cacheSection = "Cache";
        private const string _rankingSection = "Ranking";
        private const string _theEliteWebsiteSection = "TheEliteWebsite";

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

            services
                .AddSingleton<IClockProvider, ClockProvider>()
                .AddSingleton<IConnectionProvider>(new ConnectionProvider(Configuration))
                .Configure<RankingConfiguration>(Configuration.GetSection(_rankingSection))
                .Configure<TheEliteWebsiteConfiguration>(Configuration.GetSection(_theEliteWebsiteSection))
                .AddSingleton<IReadRepository, ReadRepository>()
                .AddSingleton<IWriteRepository, WriteRepository>()
                .AddSingleton<ITheEliteWebSiteParser, TheEliteWebSiteParser>()
                .AddSingleton<IRankingProvider, RankingProvider>()
                .AddSingleton<IIntegrationProvider, IntegrationProvider>()
                .AddSingleton<IWorldRecordProvider, WorldRecordProvider>()
                .AddSingleton<IStageStatisticsProvider, StageStatisticsProvider>();

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
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
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();

            ServiceProviderAccessor.SetProvider(app.ApplicationServices);
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
