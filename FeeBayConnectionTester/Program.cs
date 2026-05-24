using LocalDBConnections;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FeeBayConnectionTester
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();

            var host = CreateHostBuilder().Build();
            
            // Retrieve Form1 from DI container and run it
            using (var serviceScope = host.Services.CreateScope())
            {
                var services = serviceScope.ServiceProvider;
                var form1 = services.GetRequiredService<Form1>();
                Application.Run(form1);
            }
        }

        private static IHostBuilder CreateHostBuilder()
        {
            return Host.CreateDefaultBuilder()
                //.ConfigureAppConfiguration((context, config) =>
                //{
                //    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                //})
                .ConfigureServices((context, services) =>
                {
                    // Register HttpClientFactory
                    services.AddHttpClient();

                    // Register ILocalDbConnectionManager
                    services.AddSingleton<ILocalDbConnectionManager, LocalDbConnectionManager>();

                    // Register IConfiguration (already available via Host.CreateDefaultBuilder)
                    // services.AddSingleton<IConfiguration>(context.Configuration); // Not needed, already available

                    // Register Form1
                    services.AddTransient<Form1>();
                });
        }
    }
}