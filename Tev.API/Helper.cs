using MMSConstants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tev.API.Enums;
using Tev.API.Models;

namespace Tev.API
{
    public static class Helper
    {
        public static int GetAlertType(string addoncode)
        {
            if (!string.IsNullOrEmpty(addoncode))
            {
                var ary = addoncode.Split("-");
                return Convert.ToInt32(ary[1]);
            }
            else
            {
                return 0;
            }
        }

        public static AlertFilter AlertFilterHelper(GetAlertsRequest reqBody)
        {
            if (reqBody != null &&!string.IsNullOrEmpty(reqBody.LocationId))
            {
                return  AlertFilter.Location;
            }
            else if (reqBody != null && !string.IsNullOrEmpty(reqBody.DeviceId))
            {
                return AlertFilter.Device;
            }
            else
            {
               return AlertFilter.All;
            }
        }

        public static DateTime ConvertDateTime(string subscriptionExpiryDate)
        {
            if (!string.IsNullOrEmpty(subscriptionExpiryDate))
            {
                DateTime dt ;
                DateTime.TryParseExact(subscriptionExpiryDate, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out dt);
                return dt;
            }
            else
            {
                return new DateTime();
            }
        }

        public static DateTime FromUnixTime(long unixTime)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return epoch.AddSeconds(unixTime);
        }

        public static string Replace_withWhiteSpaceAndMakeTheStringIndexValueUpperCase(string str)
        {
            var txt = string.Empty;

            if (str != null)
            {
                txt = str.Replace("_", " ");
            }

            return char.ToUpper(txt[0]) + txt.Substring(1);
        }

        public static Applications GetDeviceType(string deviceType)
        {
            var value = Applications.TEV;

            switch (deviceType)
            {
                case nameof(Applications.TEV):
                    value = Applications.TEV;
                    break;
                case nameof(Applications.TEV2):
                    value = Applications.TEV2;
                    break;
                case nameof(Applications.WSD):
                    value = Applications.WSD;
                    break;
                default:
                    break;
            }
            return value;
        }

        public static DateTime GetISTNow()
        {
            DateTime date = DateTime.UtcNow;
            TimeSpan time = new TimeSpan(0, 5, 30, 0);
            return date.Add(time);
        }

        public static DateTime AddDayToDateTime(DateTime date,int day)
        {
            TimeSpan time = new TimeSpan(day, 0, 0, 0);
            return date.Add(time);
        }
    }
}
