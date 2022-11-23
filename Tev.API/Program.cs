using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureKeyVault;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MMSConstants;
using Serilog;
using Serilog.Events;

namespace Tev.API
{
    public static class Program
    {
        public static IConfiguration Configuration { get; } = new ConfigurationBuilder()
           .SetBasePath(Directory.GetCurrentDirectory())
           .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
           .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
           .AddEnvironmentVariables()
           .Build();
        public static int Main(string[] args)
        {
            switch (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"))
            {
                case "Development":
                    Log.Logger = new LoggerConfiguration()
                   .ReadFrom.Configuration(Configuration)
                   .WriteTo.AzureAnalytics("653d0492-896f-450b-9fe2-ff78e31fe0f1", "SMmshJY57Mej6q63nh/k89ccnbDH0k9INoC+eEARAsVP3FLPIZioXIgpWuUJh9xJu8lYBGKOL5jtMaREFziv3Q==", "TEV_API")
                   .CreateLogger();
                    break;
                case "QA":
                    Log.Logger = new LoggerConfiguration()
                   .ReadFrom.Configuration(Configuration)
                   .WriteTo.AzureAnalytics("653d0492-896f-450b-9fe2-ff78e31fe0f1", "SMmshJY57Mej6q63nh/k89ccnbDH0k9INoC+eEARAsVP3FLPIZioXIgpWuUJh9xJu8lYBGKOL5jtMaREFziv3Q==", "TEV_API")
                   .CreateLogger();
                    break;
                case "Staging":
                    Log.Logger = new LoggerConfiguration()
                   .ReadFrom.Configuration(Configuration)
                   .WriteTo.AzureAnalytics("149bf053-22c2-4b89-9e67-0593e296f99c", "iTYvey63joRkE2dSAXaVh4ESAM9qLFdn8cd7u2wQE4EL9Me9FbQmKRv9FzZBm4W/wu6Fgz2gxdT6KzHa+9RZ/g==", "TEV_API")
                   .CreateLogger();
                    break;
                case "Production":
                    Log.Logger = new LoggerConfiguration()
                   .ReadFrom.Configuration(Configuration)
                   .WriteTo.AzureAnalytics("a0612b9e-7cac-4229-9b57-37f3b5a9f015", "xz3yGyDk2X0pJ7gTWM+xk/tWBCkbv6qs9FgbCzvfasGvCycMRRO7ERvD9wN9YegvJJGoqI7LJddhWCi/SArEyw==", "TEV_API")
                   .CreateLogger();
                    break;
                default:
                    throw new InvalidOperationException("Unexpected value for Environment = " + Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"));
            }

            try
            {
                Log.Information("Starting up {application}", ApplicationNames.TEV);
                CreateHostBuilder(args).Build().Run();
                return 0;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application {application} startup failed", ApplicationNames.TEV);
                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args).ConfigureAppConfiguration((ctx, builder) =>
            {
                var root = builder.Build();
                var keyVaultEndpoint = root["KeyVaultEndpoint"];
                if (!string.IsNullOrEmpty(keyVaultEndpoint))
                {
                    var azureServiceTokenProvider = new AzureServiceTokenProvider();
                    var keyVaultClient = new KeyVaultClient(
                       new KeyVaultClient.AuthenticationCallback(
                          azureServiceTokenProvider.KeyVaultTokenCallback));
                    builder.AddAzureKeyVault(
                       keyVaultEndpoint, keyVaultClient, new DefaultKeyVaultSecretManager());
                }
            })
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.ConfigureKestrel(serverOptions =>
                {
                    serverOptions.AddServerHeader = false;
                });
                webBuilder.UseStartup<Startup>();
               
            }).UseSerilog();

    }
}
