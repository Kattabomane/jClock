using System;
using System.Globalization;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace jclock
{
    public class ClockManager
    {
        #region LOCAL VARIABLES

        private static volatile ClockManager _Instance;
        private static object _SyncRoot = new Object();

        #endregion LOCAL VARIABLES

        #region CONSTRUCTOR

        private ClockManager()
        {

        }

        #endregion CONSTRUCTOR

        #region PROPERTIES

        public static ClockManager Instance
        {
            get
            {
                if (_Instance == null)
                {
                    lock (_SyncRoot)
                    {
                        if (_Instance == null)
                            _Instance = new ClockManager();
                    }
                }

                return _Instance;
            }
        }

        #endregion PROPERTIES

        #region METHODS

        public string GetCurrentDateString(int offsetminutes = 0)
        {
            if (offsetminutes == 0)
                return DateTime.Now.ToString("dd/MM/yyyy");
            else
                return DateTime.Now.AddMinutes(offsetminutes).ToString("dd/MM/yyyy");
        }

        public string GetCurrentHourString(int offsetminutes = 0)
        {
            if (offsetminutes == 0)
                return DateTime.Now.ToString("HH");
            else
                return DateTime.Now.AddMinutes(offsetminutes).ToString("HH");
        }

        public string GetCurrentMinuteString(int offsetminutes = 0)
        {
            if (offsetminutes == 0)
                return DateTime.Now.ToString("mm");
            else
                return DateTime.Now.AddMinutes(offsetminutes).ToString("mm");
        }

        public string GetCurrentMonthString(int offsetminutes = 0)
        {
            // https://learn.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings
            return DateTime.Now.ToString("MMM", new CultureInfo("en-US"));
        }

        public string GetCurrentDayString(int offsetminutes = 0)
        {
            // https://learn.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings
            return DateTime.Now.ToString("ddd", new CultureInfo("en-US"));
        }

        public async Task<bool> IsOpenWeatherMapApiReachableAsync()
        {
            // https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/network-info
            bool result = false;

            string apiHostName = "api.openweathermap.org";

            using (var ping = new Ping())
            {
                PingReply reply = await ping.SendPingAsync(apiHostName);
                if (reply != null)
                    result = reply.Status == IPStatus.Success;
            }

            return result;
        }

        #endregion METHODS
    }
}