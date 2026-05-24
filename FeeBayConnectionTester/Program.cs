using FeeBayOAuth.TokenFactory;
using LocalDBConnections;
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
            ApplicationConfiguration.Initialize();

            var host = CreateHostBuilder().Build();
            
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
                .ConfigureServices((context, services) =>
                {
                    // Register HttpClientFactory
                    services.AddHttpClient();

                    // Register ILocalDbConnectionManager
                    services.AddSingleton<ILocalDbConnectionManager, LocalDbConnectionManager>();

                    // Register OAuthTokenFactory
                    services.AddSingleton<OAuthTokenFactory>();

                    // Register Form1
                    services.AddTransient<Form1>();
                });
        }
    }
}