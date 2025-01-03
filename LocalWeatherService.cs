using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Drawing.Processing;
using System.Net.Http;

namespace jclock
{
    public class LocalWeatherService : IHostedService, IAsyncDisposable
    {
        #region LOCAL VARIABLES

        protected readonly ILogger<LocalWeatherService> _Logger;
        private readonly Task _completedTask = Task.CompletedTask;
        private Task _timerTask;
        private int _executionCount = 0;
        private System.Threading.PeriodicTimer _Timer;
        private CancellationTokenSource _cts;

        // Display
        private int _DisplayW;
        private int _DisplayH;
        private int _TimeOffset;
        private EPaperDisplay2in7 _Display;

        private FontFamily _RobotoFontFamily;
        private Font _FontDate;
        private Font _FontDay;
        private Font _FontTime;
        private Font _FontWeather;

        private Image<Rgba32> _IconTemperature;
        private Image<Rgba32> _IconHumidity;
        private Image<Rgba32> _IconPressure;
        private Image<Rgba32> _IconWind;

        private Image<Rgba32> _IconHourGlass;
        private Image<Rgba32> _IconGlobe;
        private Image<Rgba32> _IconError;

        // Icon weather
        private Image<Rgba32> _IconWeatherClearSky; // 01d
        private Image<Rgba32> _IconWeatherClearSkyNight; // 01n
        private Image<Rgba32> _IconWeatherFewClouds; // 02
        private Image<Rgba32> _IconWeatherScatteredClouds; // 03
        private Image<Rgba32> _IconWeatherBrokenClouds; // 04
        private Image<Rgba32> _IconWeatherShowerRain; // 09
        private Image<Rgba32> _IconWeatherRain; // 10
        private Image<Rgba32> _IconWeatherThunderStorm; // 11
        private Image<Rgba32> _IconWeatherSnow; // 13
        private Image<Rgba32> _IconWeatherMist; // 50


        #endregion LOCAL VARIABLES

        #region CONSTRUCTOR

        public LocalWeatherService(ILogger<LocalWeatherService> logger)
        {
            _Logger = logger;

            _DisplayW = ConfigurationService.Instance.DisplayWidth;
            _DisplayH = ConfigurationService.Instance.DisplayHeight;
            _TimeOffset = ConfigurationService.Instance.TimeOffset;

            LoadFontsAndIcons();
            InitializeDisplay();
            ShowWait();
        }

        #endregion CONSTRUCTOR

        #region PUBLIC METHODS

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _cts = new CancellationTokenSource();
            _Timer = new PeriodicTimer(TimeSpan.FromSeconds(ConfigurationService.Instance.RefreshTimeout));
            _timerTask = HandleWeatherServiceTimerAsync(_Timer, _cts.Token);

            return _completedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _Logger.LogInformation("{Service} is stopping.", nameof(LocalWeatherService));

            _cts.Cancel();

            return _completedTask;
        }

        public async ValueTask DisposeAsync()
        {
            _Timer.Dispose();
            await _timerTask;
            GC.SuppressFinalize(this);
        }

        #endregion PUBLIC METHODS

        #region PRIVATE METHODS

        private async Task HandleWeatherServiceTimerAsync(PeriodicTimer timer, CancellationToken cancel = default)
        {
            try
            {
                while (await timer.WaitForNextTickAsync(cancel))
                {
                    try
                    {
                        int count = Interlocked.Increment(ref _executionCount);

                        _Logger.LogInformation(
                            "{Service} is working, execution count: {Count:#,0}",
                            nameof(LocalWeatherService),
                            count);

                        await ShowWeatherForecastAsync();
                    }
                    catch (Exception HandleException)
                    {
                        _Logger.LogError("LocalWeatherService : Facing some problems to handle old tokens clearance !");
                        _Logger.LogError("LocalWeatherService : Exception is : [{0}]", HandleException);
                    }
                }
            }

            catch (Exception Ex)
            {
                _Logger.LogError("Critical exception in LocalWeatherService. The service will be halted !");
                _Logger.LogError("Exception is : [{0}]", Ex);
            }
        }


        private void LoadFontsAndIcons()
        {
            _Logger.LogDebug("Loading necessary fonts...");

            // Init font
            FontCollection collection = new FontCollection();
            _RobotoFontFamily = collection.Add(Path.Combine(ConfigurationService.Instance.FontsPath, "Roboto-Black.ttf"));

            _FontDate = _RobotoFontFamily.CreateFont(28, FontStyle.Bold);
            _FontDay = _RobotoFontFamily.CreateFont(16, FontStyle.Bold);
            _FontTime = _RobotoFontFamily.CreateFont(35, FontStyle.Bold);
            _FontWeather = _RobotoFontFamily.CreateFont(16, FontStyle.Bold);

            _Logger.LogDebug("Loading necessary icons...");

            _IconTemperature = Image.Load<Rgba32>(Path.Combine(ConfigurationService.Instance.IconsPath, "icon-temperature.bmp"));
            _IconHumidity = Image.Load<Rgba32>(Path.Combine(ConfigurationService.Instance.IconsPath, "icon-humidity.bmp"));
            _IconPressure = Image.Load<Rgba32>(Path.Combine(ConfigurationService.Instance.IconsPath, "icon-pressure.bmp"));
            _IconWind = Image.Load<Rgba32>(Path.Combine(ConfigurationService.Instance.IconsPath, "icon-wind.bmp"));

            _IconTemperature.Mutate(i => i.Resize(30, 30));
            _IconHumidity.Mutate(i => i.Resize(30, 30));
            _IconPressure.Mutate(i => i.Resize(30, 30));
            _IconWind.Mutate(i => i.Resize(30, 30));

            _IconHourGlass = Image.Load<Rgba32>(Path.Combine(ConfigurationService.Instance.IconsPath, "icon-hourglass.bmp"));
            _IconGlobe = Image.Load<Rgba32>(Path.Combine(ConfigurationService.Instance.IconsPath, "icon-globe.bmp"));
            _IconError = Image.Load<Rgba32>(Path.Combine(ConfigurationService.Instance.IconsPath, "icon-error.bmp"));
            _IconHourGlass.Mutate(i => i.Resize(64, 64));
            _IconGlobe.Mutate(i => i.Resize(64, 64));
            _IconError.Mutate(i => i.Resize(64, 64));

            _IconWeatherClearSky = Image.Load<Rgba32>(Path.Combine(ConfigurationService.Instance.IconsPath, "01d.bmp"));
            _IconWeatherClearSkyNight = Image.Load<Rgba32>(Path.Combine(ConfigurationService.Instance.IconsPath, "01n.bmp"));
            _IconWeatherFewClouds = Image.Load<Rgba32>(Path.Combine(ConfigurationService.Instance.IconsPath, "02d.bmp"));
            _IconWeatherScatteredClouds = Image.Load<Rgba32>(Path.Combine(ConfigurationService.Instance.IconsPath, "03d.bmp"));
            _IconWeatherBrokenClouds = Image.Load<Rgba32>(Path.Combine(ConfigurationService.Instance.IconsPath, "04d.bmp"));
            _IconWeatherShowerRain = Image.Load<Rgba32>(Path.Combine(ConfigurationService.Instance.IconsPath, "09d.bmp"));
            _IconWeatherRain = Image.Load<Rgba32>(Path.Combine(ConfigurationService.Instance.IconsPath, "10d.bmp"));
            _IconWeatherThunderStorm = Image.Load<Rgba32>(Path.Combine(ConfigurationService.Instance.IconsPath, "11d.bmp"));
            _IconWeatherSnow = Image.Load<Rgba32>(Path.Combine(ConfigurationService.Instance.IconsPath, "13d.bmp"));
            _IconWeatherMist = Image.Load<Rgba32>(Path.Combine(ConfigurationService.Instance.IconsPath, "50d.bmp"));

            _IconWeatherClearSky.Mutate(i => i.Resize(64, 64));
            _IconWeatherClearSkyNight.Mutate(i => i.Resize(64, 64));
            _IconWeatherFewClouds.Mutate(i => i.Resize(64, 64));
            _IconWeatherScatteredClouds.Mutate(i => i.Resize(64, 64));
            _IconWeatherBrokenClouds.Mutate(i => i.Resize(64, 64));
            _IconWeatherShowerRain.Mutate(i => i.Resize(64, 64));
            _IconWeatherRain.Mutate(i => i.Resize(64, 64));
            _IconWeatherThunderStorm.Mutate(i => i.Resize(64, 64));
            _IconWeatherSnow.Mutate(i => i.Resize(64, 64));
            _IconWeatherMist.Mutate(i => i.Resize(64, 64));
        }

        private void InitializeDisplay()
        {
            _Logger.LogDebug("Initializing e-Paper display (2in7)...");

            _Display = new EPaperDisplay2in7();
            _Display.Init();
        }

        private void ShowWait()
        {
            _Logger.LogDebug("> ShowWait");

            var waitimage = new Image<Rgba32>(_DisplayW, _DisplayH, Color.White);

            waitimage.Mutate(x =>
            {
                x.DrawImage(_IconHourGlass, new Point(10, 50), 1f);

                x.DrawText("Initializing...", _FontDate, Color.Black, new PointF(90, 70));
            });

            _Display.Clear(0x00);
            _Display.DisplayImage(waitimage);
            _Logger.LogDebug("< ShowWait");
        }

        private void ShowConnecting()
        {
            _Logger.LogDebug("> ShowConnecting");

            var globeimage = new Image<Rgba32>(_DisplayW, _DisplayH, Color.White);

            globeimage.Mutate(x =>
            {
                x.DrawImage(_IconGlobe, new Point(10, 50), 1f);

                x.DrawText("Connecting...", _FontDate, Color.Black, new PointF(90, 70));
            });

            _Display.Clear(0x00);
            _Display.DisplayImage(globeimage);
            _Logger.LogDebug("< ShowConnecting");
        }

        private void ShowError()
        {
            _Logger.LogDebug("> ShowError");

            var errorimage = new Image<Rgba32>(_DisplayW, _DisplayH, Color.White);

            errorimage.Mutate(x =>
            {
                x.DrawImage(_IconError, new Point(10, 50), 1f);

                x.DrawText("Keep trying...", _FontDate, Color.Black, new PointF(90, 70));
            });

            _Display.Clear(0x00);
            _Display.DisplayImage(errorimage);
            _Logger.LogDebug("< ShowError");
        }

        private async Task ShowWeatherForecastAsync()
        {
            _Logger.LogDebug("> ShowWeatherForecast");

            if (await ClockManager.Instance.IsOpenWeatherMapApiReachableAsync())
            {
                var wreport = await GetCurrentWeatherReportAsync();

                if (wreport != null)
                {
                    string currentdate = ClockManager.Instance.GetCurrentDateString(_TimeOffset);
                    string currentday = ClockManager.Instance.GetCurrentDayString(_TimeOffset);
                    string currentmonth = ClockManager.Instance.GetCurrentMonthString(_TimeOffset);
                    string currenttimehour = ClockManager.Instance.GetCurrentHourString(_TimeOffset);
                    string currenttimeminute = ClockManager.Instance.GetCurrentMinuteString(_TimeOffset);

                    string currenttemperature = $" : {Math.Ceiling(wreport.Main.Temp)} °C - ({Math.Ceiling(wreport.Main.TempMin)}|{Math.Ceiling(wreport.Main.TempMax)})";
                    string currentpressure = $" : {wreport.Main.Pressure} hPa";
                    string currenthumidity = $" : {wreport.Main.Humidity} %";
                    string currentwind = $" : {wreport.Wind.Speed} m/s";

                    string currentweathericon = wreport.Weather[0].Icon;

                    var wthimage = new Image<Rgba32>(_DisplayW, _DisplayH, Color.White);

                    wthimage.Mutate(x =>
                    {
                        x.DrawLine(Color.Black, 3, new PointF(0, 30), new PointF(_DisplayW - 50, 30));
                        x.DrawLine(Color.Black, 4, new PointF(_DisplayW - 50, 0), new PointF(_DisplayW - 50, _DisplayH));

                        x.DrawText(currentdate, _FontDate, Color.Black, new PointF(10, 1));
                        x.DrawText(currentday, _FontDay, Color.Black, new PointF(_DisplayW - 90, 1));
                        x.DrawText(currentmonth, _FontDay, Color.Black, new PointF(_DisplayW - 90, 13));
                        x.DrawText(currenttimehour, _FontTime, Color.Black, new PointF(_DisplayW - 45, 50));
                        x.DrawText(currenttimeminute, _FontTime, Color.Black, new PointF(_DisplayW - 45, 85));

                        x.DrawImage(_IconTemperature, new Point(2, 35), 1f);
                        x.DrawImage(_IconPressure, new Point(2, 70), 1f);
                        x.DrawImage(_IconHumidity, new Point(2, 105), 1f);
                        x.DrawImage(_IconWind, new Point(2, 140), 1f);

                        x.DrawText(currenttemperature, _FontWeather, Color.Black, new PointF(30, 40));
                        x.DrawText(currentpressure, _FontWeather, Color.Black, new PointF(30, 75));
                        x.DrawText(currenthumidity, _FontWeather, Color.Black, new PointF(30, 110));
                        x.DrawText(currentwind, _FontWeather, Color.Black, new PointF(30, 145));

                        x.DrawImage(GetWeatherIcon(currentweathericon), new Point(130, 80), 1f);
                    });

                    _Display.Clear(0x00);
                    _Display.DisplayImage(wthimage);
                }
                else
                {
                    _Logger.LogDebug("Error while loading error report !");
                    ShowError();
                }
            }
            else
            {
                _Logger.LogDebug("Net connection is down ! Connecting !");
                ShowConnecting();
            }

            _Logger.LogDebug("< ShowWeatherForecast");
        }

        private async Task<WeatherReport> GetCurrentWeatherReportAsync()
        {
            WeatherReport retvalue = null;

            try
            {
                var WeatherClient = new OpenWeatherMapRestClient("https://api.openweathermap.org", ConfigurationService.Instance.OpenWeatherMapApiKey, new HttpClient());
                retvalue = await WeatherClient.GetCurrentWeatherReportAsync(ConfigurationService.Instance.OpenWeatherMapApiCfgLatitude, ConfigurationService.Instance.OpenWeatherMapApiCfgLongitude,
                                                                            ConfigurationService.Instance.OpenWeatherMapApiCfgUnits, ConfigurationService.Instance.OpenWeatherMapApiCfgLanguage);
                if (retvalue != null)
                {
                    _Logger.LogDebug("Current weather report retrieved successfully !");
                }

            }
            catch (Exception Ex)
            {
                _Logger.LogError("Critical exception in GetCurrentWeatherReport. Cannot get current weather report !");
                _Logger.LogError("Exception is : [{0}]", Ex);
            }

            return retvalue;
        }

        private Image<Rgba32> GetWeatherIcon(string iconname)
        {
            if (iconname.StartsWith("01d"))
                return _IconWeatherClearSky;
            else if (iconname.StartsWith("01n"))
                return _IconWeatherClearSkyNight;
            else if (iconname.StartsWith("02"))
                return _IconWeatherFewClouds;
            else if (iconname.StartsWith("03"))
                return _IconWeatherScatteredClouds;
            else if (iconname.StartsWith("04"))
                return _IconWeatherBrokenClouds;
            else if (iconname.StartsWith("09"))
                return _IconWeatherShowerRain;
            else if (iconname.StartsWith("10"))
                return _IconWeatherRain;
            else if (iconname.StartsWith("11"))
                return _IconWeatherThunderStorm;
            else if (iconname.StartsWith("13"))
                return _IconWeatherSnow;
            else if (iconname.StartsWith("50"))
                return _IconWeatherMist;
            else
                return new Image<Rgba32>(32, 32, Color.White);
        }

        #endregion PRIVATE METHODS
    }
}