using System;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using System.Reflection;
using System.Runtime.Versioning;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;


namespace jclock
{
    internal class Program
    {
        #region LOGGER

        private static NLog.Logger _Logger = NLog.LogManager.GetCurrentClassLogger();

        #endregion LOGGER

        /// <summary>
        /// MAIN method
        /// </summary>
        /// <param name="args">Command line switches (N/A)</param>
        static async Task Main(string[] args)
        {
            await InitService(args);
        }

        /// <summary>
        /// Initializes the service
        /// </summary>
        static async Task InitService(string[] args)
        {
            _Logger.Info(@"¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤");
            _Logger.Info(@"                    jClock Service");
            _Logger.Info(@"¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤¤");
            _Logger.Info(@"Dumping runtime environment information...");

            _Logger.Info($"     Machine name          : [{Environment.MachineName}]");
            _Logger.Info($"     OS                    : [{System.Runtime.InteropServices.RuntimeInformation.OSDescription}]");
            _Logger.Info($"     Is 64 bits OS         : [{Environment.Is64BitOperatingSystem}]");
            _Logger.Info($"     Framework             : [{Assembly.GetEntryAssembly()?.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName}]");
            _Logger.Info($"     Cmd line              : [{Environment.CommandLine}]");
            _Logger.Info($"     Base directory        : [{Environment.CurrentDirectory}]");
            _Logger.Info($"     Application version   : [{Assembly.GetEntryAssembly()?.GetName().Version}]");

            _Logger.Info($"");

            ConfigurationService.Instance.Load();

            try
            {
                await CreateHostBuilder(args).Build().RunAsync();
            }
            catch (Exception ex)
            {
                _Logger.Error($"Stopped service because of exception while creating program host : {ex}");
            }
        }

        static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
        .ConfigureAppConfiguration((builderContext, config) =>
        {
            // Nothing to do for moment
        })
        .ConfigureServices((hostcontext, services) =>
        {
            services.AddSystemd();
            // Start LocalWeatherService
            services.AddHostedService<LocalWeatherService>();
        })
        .ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddNLog(); // NLog: Setup NLog for Dependency injection
            logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
        });
    }
}