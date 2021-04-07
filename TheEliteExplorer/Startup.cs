using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Converters;
using TheEliteExplorerCommon;
using TheEliteExplorerDomain;
using TheEliteExplorerDomain.Abstractions;
using TheEliteExplorerDomain.Configuration;
using TheEliteExplorerDomain.Providers;
using TheEliteExplorerInfrastructure;
using TheEliteExplorerInfrastructure.Configuration;

namespace TheEliteExplorer
{
    /// <summary>
    /// Startup.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class Startup
    {
        private readonly IConfiguration _configuration;

        private const string _cacheSection = "Cache";
        private const string _rankingSection = "Ranking";
        private const string _theEliteWebsiteSection = "TheEliteWebsite";

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Instance of <see cref="IConfiguration"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="configuration"/> is <c>Null</c>.</exception>
        public Startup(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <summary>
        /// This method gets called by the runtime.
        /// Use this method to add services to the container.
        /// </summary>
        /// <param name="services">Collection of <see cref="ServiceDescriptor"/>.</param>
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();

            // Register the Swagger generator, defining 1 or more Swagger documents
            services.AddSwaggerGen();
          
            Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

            services
                .AddSingleton<IClockProvider, ClockProvider>()
                .AddSingleton<IConnectionProvider>(new ConnectionProvider(_configuration))
                .Configure<CacheConfiguration>(_configuration.GetSection(_cacheSection))
                .Configure<RankingConfiguration>(_configuration.GetSection(_rankingSection))
                .Configure<TheEliteWebsiteConfiguration>(_configuration.GetSection(_theEliteWebsiteSection))
                .AddSingleton<ISqlContext, SqlContext>()
                .AddSingleton<ITheEliteWebSiteParser, TheEliteWebSiteParser>()
                .AddSingleton<IStageSweepProvider, StageSweepProvider>()
                .AddSingleton<IRankingProvider, RankingProvider>()
                .AddDistributedMemoryCache();
        }

        /// <summary>
        /// This method gets called by the runtime.
        /// Use this method to configure the HTTP request pipeline.
        /// </summary>
        /// <param name="app">Application builder.</param>
        /// <param name="env">Environment informations.</param>
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger();

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.),
            // specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
            });

            ServiceProviderAccessor.SetProvider(app.ApplicationServices);
            app.UseMvc();
        }
    }
}
