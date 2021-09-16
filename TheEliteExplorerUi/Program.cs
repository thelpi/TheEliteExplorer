using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Authentication;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace TheEliteExplorerUi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseKestrel(opt =>
                {
                    opt.AddServerHeader = false;
                    opt.ConfigureHttpsDefaults(s =>
                    {
                        s.SslProtocols = SslProtocols.Tls12;
                    });
                })
                .UseStartup<Startup>();
    }
}
