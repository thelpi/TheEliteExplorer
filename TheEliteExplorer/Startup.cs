using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TheEliteExplorer.Infrastructure;
using TheEliteExplorer.Infrastructure.Configuration;

namespace TheEliteExplorer
{
    /// <summary>
    /// Startup.
    /// </summary>
    public class Startup
    {
        private readonly IConfiguration _configuration;

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

            services
                .AddSingleton<IConnectionProvider>(new ConnectionProvider(_configuration))
                .Configure<CacheConfiguration>(_configuration.GetSection("Cache"))
                .Configure<TheEliteWebsiteConfiguration>(_configuration.GetSection("TheEliteWebsite"))
                .AddSingleton<ISqlContext, SqlContext>()
                .AddSingleton<ITheEliteWebSiteParser, TheEliteWebSiteParser>()
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

            app.UseMvc();
        }
    }
}
