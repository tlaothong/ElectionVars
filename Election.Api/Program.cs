using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Election.Api.Controllers;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Election.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
             .UseApplicationInsights()
                .UseStartup<Startup>().UseUrls("http://localhost:5000");

        // .UseApplicationInsights()
        // .UseStartup<Startup>()
        // .UseIISIntegration();
        // // .UseStartup<Startup>().UseUrls("http://localhost:5000");
    }
}
