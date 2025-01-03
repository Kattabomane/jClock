using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using NLog;

namespace jclock
{
    public class ConfigurationService
    {
        #region LOGGER

        private static NLog.Logger _Log = LogManager.GetCurrentClassLogger();

        #endregion LOGGER

        #region LOCAL VARIABLES

        private static volatile ConfigurationService _Instance;
        private static object _SyncRoot = new Object();


        private string _ApplicationPath;
        private string _ConfigFilePath;
        private string _ConfigFileName;
        private string _FontsPath;
        private string _IconsPath;


        private string _OpenWeatherMapApiKey;
        private Double _OpenWeatherMapApiCfgLatitude;
        private Double _OpenWeatherMapApiCfgLongitude;
        private ApiUnits _OpenWeatherMapApiCfgUnits;
        private ApiLang _OpenWeatherMapApiCfgLanguage;
        private int _RefreshTimeout;
        private int _DisplayWidth;
        private int _DisplayHeight;
        private int _TimeOffset;

        #endregion LOCAL VARIABLES

        #region CONSTRUCTOR

        /// <summary>
        /// Private constructor
        /// Singleton access should be made via Instance
        /// </summary>
        private ConfigurationService()
        {
            _ApplicationPath = string.Empty;
            _ConfigFilePath = string.Empty;
            _FontsPath = string.Empty;
            _IconsPath = string.Empty;
            _ConfigFileName = "appsettings.json";

            _OpenWeatherMapApiCfgLongitude = 7.367;          // Default example >>> Turin, IT
            _OpenWeatherMapApiCfgLongitude = 45.133;         // Default example >>> Turin, IT
            _OpenWeatherMapApiCfgUnits = ApiUnits.metric;    // Default in metric units            
            _OpenWeatherMapApiCfgLanguage = ApiLang.en;      // Default in english 

            _RefreshTimeout = 30;
            _DisplayWidth = 264;
            _DisplayHeight = 176;
            _TimeOffset = 5;
        }

        #endregion CONSTRUCTOR

        #region PROPERTIES

        public static ConfigurationService Instance
        {
            get
            {
                if (_Instance == null)
                {
                    lock (_SyncRoot)
                    {
                        if (_Instance == null)
                            _Instance = new ConfigurationService();
                    }
                }

                return _Instance;
            }
        }

        public string ApplicationPath
        {
            get { return _ApplicationPath; }
        }

        public string ConfigFileName
        {
            get { return _ConfigFileName; }
        }

        public string ConfigFilePath
        {
            get { return _ConfigFilePath; }
        }

        public string FontsPath
        {
            get { return _FontsPath; }
        }

        public string IconsPath
        {
            get { return _IconsPath; }
        }

        public string OpenWeatherMapApiKey
        {
            get { return _OpenWeatherMapApiKey; }
        }

        public Double OpenWeatherMapApiCfgLatitude
        {
            get { return _OpenWeatherMapApiCfgLatitude; }
        }

        public Double OpenWeatherMapApiCfgLongitude
        {
            get { return _OpenWeatherMapApiCfgLongitude; }
        }

        public ApiUnits OpenWeatherMapApiCfgUnits
        {
            get { return _OpenWeatherMapApiCfgUnits; }
        }

        public ApiLang OpenWeatherMapApiCfgLanguage
        {
            get { return _OpenWeatherMapApiCfgLanguage; }
        }

        public int RefreshTimeout
        {
            get { return _RefreshTimeout; }
        }

        public int DisplayWidth
        {
            get { return _DisplayWidth; }
        }

        public int DisplayHeight
        {
            get { return _DisplayHeight; }
        }

        public int TimeOffset
        {
            get { return _TimeOffset; }
        }

        #endregion PROPERTIES

        #region METHODS

        public void Load()
        {
            _Log.Info($"> Loading application configurations...");

            _ApplicationPath = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            _ConfigFilePath = $"{_ApplicationPath}{Path.DirectorySeparatorChar}{_ConfigFileName}";
            _FontsPath = $"{_ApplicationPath}{Path.DirectorySeparatorChar}fonts";
            _IconsPath = $"{_ApplicationPath}{Path.DirectorySeparatorChar}icons";

            _Log.Debug($"   ApplicationPath : {_ApplicationPath}");
            _Log.Debug($"   ConfigFilePath : {_ConfigFilePath}");

            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(_ConfigFilePath);

            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile(_ConfigFilePath, true, true)
                .Build();

            try
            {
                _Log.Debug($"Loading application settings from [{_ConfigFilePath}]...");

                // Read application configurations
                if (config.GetSection("JClock:OpenWeatherMapApiKey").Exists())
                    _OpenWeatherMapApiKey = config["JClock:OpenWeatherMapApiKey"];

                if (config.GetSection("JClock:OpenWeatherMapApiCfgLatitude").Exists())
                    _OpenWeatherMapApiCfgLatitude = Convert.ToDouble(config["JClock:OpenWeatherMapApiCfgLatitude"]);

                if (config.GetSection("JClock:OpenWeatherMapApiCfgLongitude").Exists())
                    _OpenWeatherMapApiCfgLongitude = Convert.ToDouble(config["JClock:OpenWeatherMapApiCfgLongitude"]);

                if (config.GetSection("JClock:OpenWeatherMapApiCfgUnits").Exists())
                    _OpenWeatherMapApiCfgUnits = (ApiUnits)Enum.Parse(typeof(ApiUnits), config["JClock:OpenWeatherMapApiCfgUnits"]);

                if (config.GetSection("JClock:OpenWeatherMapApiCfgLanguage").Exists())
                    _OpenWeatherMapApiCfgLanguage = (ApiLang)Enum.Parse(typeof(ApiLang), config["JClock:OpenWeatherMapApiCfgLanguage"]);

                if (config.GetSection("JClock:RefreshTimeout").Exists())
                    _RefreshTimeout = Convert.ToInt32(config["JClock:RefreshTimeout"]);

                if (config.GetSection("JClock:DisplayWidth").Exists())
                    _DisplayWidth = Convert.ToInt32(config["JClock:DisplayWidth"]);

                if (config.GetSection("JClock:DisplayHeight").Exists())
                    _DisplayHeight = Convert.ToInt32(config["JClock:DisplayHeight"]);

                if (config.GetSection("JClock:TimeOffset").Exists())
                    _TimeOffset = Convert.ToInt32(config["JClock:TimeOffset"]);
            }
            catch (Exception Ex)
            {
                Console.WriteLine($"Exception while loading configurations from appsettings.json : {Ex}");
            }
        }

        #endregion METHODS
    }
}