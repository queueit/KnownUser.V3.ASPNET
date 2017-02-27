using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace QueueIT.KnownUserV3.SDK.Sample
{
    public class QueueParameterHelper
    {
        public const string QueueITTokenKey = "queueittoken";
        public const string _TimeStampKey = "ts";
        public const string _ExtendCookieKey = "ce";
        public const string _CookieValidityMinuteKey = "cv";
        public const string _RedirectIdentifierKey = "ri";
        public const string _ValidationStateKey = "vs";
        public const string _QueueIdKey = "q";
        public const string _HashKey = "h";
        public const string _CustomerIdKey = "c";
        public const string _EventIdKey = "e";
        public const string _RedirectTypeKey = "rt";
        public const string _DoHandShake = "dh";
        public const char _KeyValueSeparatorChar = ':';
        public const char _KeyValueSeparatorGroupChar = '&';
        public static QueueUrlParams ExtractQueueParams(string queueitToken)
        {
            try
            {
                if (string.IsNullOrEmpty(queueitToken))
                    return null;
                QueueUrlParams result = new QueueUrlParams()
                {
                    QueueITToken = HttpUtility.UrlDecode(queueitToken)

                };
                var parames = result.QueueITToken.Split(_KeyValueSeparatorGroupChar);
                foreach (var paramKeyValue in parames)
                {
                    var keyValueArr = paramKeyValue.Split(_KeyValueSeparatorChar);

                    switch (keyValueArr[0])
                    {
                        case _TimeStampKey:
                            {
                                result.TimeStampString = keyValueArr[1];
                                result.TimeStamp = DateTimeHelper.GetUnixTimeStampAsDate(result.TimeStampString);
                                break;
                            }
                        case _CookieValidityMinuteKey:
                            result.CookieValidityMinute = int.Parse(keyValueArr[1]);
                            break;
                        case _CustomerIdKey:
                            result.CustomerId = keyValueArr[1];
                            break;
                        case _EventIdKey:
                            result.EventId = keyValueArr[1];
                            break;
                        case _ExtendCookieKey:
                            result.ExtendCookie = bool.Parse(keyValueArr[1]);
                            break;
                        case _HashKey:
                            result.HashCode = keyValueArr[1];
                            break;
                        case _QueueIdKey:
                            result.QueueId = keyValueArr[1];
                            break;
                        case _RedirectIdentifierKey:
                            result.RedirectIdentifier = keyValueArr[1];
                            break;
                        case _RedirectTypeKey:
                            result.RedirectType = keyValueArr[1];
                            break;
                        case _ValidationStateKey:
                            result.ValidationState = keyValueArr[1];
                            break;
                        case _DoHandShake:
                            result.DoHandShake = bool.Parse(keyValueArr[1]);
                            break;
                    }
                }

                result.QueueITTokenWithoutHash =
                    result.QueueITToken.Replace($"{_KeyValueSeparatorGroupChar}{_HashKey}{_KeyValueSeparatorChar}{result.HashCode}", "");
                return result;
            }
            catch
            {
                return null;
            }

        }

        public static string GetPureUrlWithoutQueueITToken(string url)
        {
            return Regex.Replace(url, @"([\?&])(" + QueueITTokenKey + "=[^&]*)", string.Empty, RegexOptions.IgnoreCase);
        }


    }
    public static class DateTimeHelper
    {
        public static DateTime GetUnixTimeStampAsDate(string timeStampString)
        {
            long timestampSeconds = long.Parse(timeStampString);
            DateTime date1970 = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            return date1970.AddSeconds(timestampSeconds);
        }
        public static long GetUnixTimeStampFromDate(DateTime time)
        {
            return (long)(time.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }
        //public static string ToDateTimeUTCString(DateTime dateTime)
        //{
        //    return dateTime.ToUniversalTime().ToString("o");
        //}
        //public static DateTime GetDateTimeUTCFromString(string utcString)
        //{
        //    DateTime result;
        //    if (DateTime.TryParse(utcString, null, System.Globalization.DateTimeStyles.RoundtripKind, out result))
        //        return result;
        //    return DateTime.MinValue;
        //}

    }
    public static class HashHelper
    {
        public static string GenerateSHA256Hash(string secretKey, string stringToHash)
        {
            using (HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey)))
            {
                return HttpUtility.UrlEncode(HttpUtility.UrlEncode(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToHash))));
            }
        }
    }


    public class QueueUrlParams
    {
        public DateTime TimeStamp { get; set; }
        public string TimeStampString { get; set; }
        public string QueueId { get; set; }
        public string EventId { get; set; }
        public string CustomerId { get; set; }
        public string HashCode { get; set; }
        public bool? ExtendCookie { get; set; }
        public int? CookieValidityMinute { get; set; }
        public string RedirectIdentifier { get; set; }
        public string ValidationState { get; set; }
        public string RedirectType { get; set; }
        public bool DoHandShake { get; set; }
        public string QueueITToken { get; set; }
        public string QueueITTokenWithoutHash { get; set; }

    }
}
