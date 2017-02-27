using System;
using System.Linq;
using System.Web;

namespace QueueIT.KnownUserV3.SDK
{
    internal interface IUserInQueueStateRepository
    {
        void Store(
            string eventId,
            bool isStateExtendable,
            string cookieDomain,
            int cookieValidityMinute,
            string customerSecretKey);
        bool HasValidState(string eventId,
            string customerSecretKey);
        bool IsStateExtendable(
            string eventId);
        void CancelQueueCookie(
           string eventId);
        void ExtendQueueCookie(
            string eventId,
            int cookieValidityMinute,
            string secretKey
            );

        
    }

    internal class UserInQueueStateCookieRepository : IUserInQueueStateRepository
    {
        private const string _QueueITDataKey = "QueueITAccepted-SDFrts345E-V3";
        private const string _QueueITRunTimeInfoKey = "QueueITRuntimeInfo-SDFrts345E-V3";

        public Func<HttpContextBase> GetHttpContext { get; set; }
        internal static string GetCookieKey(string eventId)
        {
            return $"{_QueueITDataKey}_{eventId}";
        }

        public UserInQueueStateCookieRepository()
        {
            this.GetHttpContext = () => new HttpContextWrapper(HttpContext.Current);
        }

        public void Store(
            string eventId,
            bool isStateExtendable,
            string cookieDomain,
            int cookieValidityMinute,
            string secretKey)
        {
            var cookieKey = GetCookieKey(eventId);
            var cookie = CreateCookie(eventId, isStateExtendable, cookieValidityMinute, cookieDomain, secretKey);
            if (GetHttpContext().Response.Cookies.AllKeys.Any(key => key == cookieKey))
                GetHttpContext().Response.Cookies.Remove(cookieKey);
            GetHttpContext().Response.Cookies.Add(cookie);
        }
        public bool IsStateExtendable(
            string eventId)
        {
            try
            {
                var cookieKey = GetCookieKey(eventId);
                HttpCookie cookie = GetHttpContext().Request.Cookies.Get(cookieKey);
                if (cookie == null)
                    return false;
                return bool.Parse(cookie["IsCookieExtendable"]);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool HasValidState(string eventId, string secretKey)
        {
            try
            {
                var cookieKey = GetCookieKey(eventId);
                HttpCookie cookie = GetHttpContext().Request.Cookies.Get(cookieKey);
                if (cookie == null)
                    return false;
                if (!IsCookieValid(secretKey, cookie, eventId))
                    return false;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private string GenerateHash(
            string isCookieExtendable,
            string expirationTime,
            string secretKey)
        {
            string valueToHash = string.Concat(
                isCookieExtendable,
                expirationTime,
                secretKey);
            return HashHelper.GenerateSHA256Hash(secretKey, valueToHash);
        }

        public void CancelQueueCookie(string eventId)
        {
            var cookieKey = GetCookieKey(eventId);
            if (GetHttpContext().Response.Cookies.AllKeys.Any(key => key == cookieKey))
            {
                GetHttpContext().Response.Cookies.Remove(cookieKey);
                var cookie = new HttpCookie(cookieKey);
                cookie.Expires = DateTime.UtcNow.AddDays(-1d);
                GetHttpContext().Response.Cookies.Add(cookie);
            }
        }

        public void ExtendQueueCookie(
            string eventId,
            int cookieValidityMinute,
            string secretKey)
        {
            var cookieKey = GetCookieKey(eventId);
            HttpCookie cookie = GetHttpContext().Request.Cookies.Get(cookieKey);
            if (cookie == null)
                return;
            if (!IsCookieValid(secretKey, cookie, eventId))
                return;
            var newCookie = CreateCookie(eventId, bool.Parse(cookie.Values["IsCookieExtendable"]),
                cookieValidityMinute, cookie.Domain, secretKey);

            if (GetHttpContext().Response.Cookies.AllKeys.Any(key => key == cookieKey))
                GetHttpContext().Response.Cookies.Remove(cookieKey);
            GetHttpContext().Response.Cookies.Add(newCookie);
        }

        private HttpCookie CreateCookie(
            string eventId,
            bool isCookieExtendable,
            int cookieValidityMinute,
            string cookieDomain,
            string secretKey)
        {
            var cookieKey = GetCookieKey(eventId);
            var expirationTime = DateTime.UtcNow.Add(TimeSpan.FromMinutes(cookieValidityMinute));
            var expirationTimeString = DateTimeHelper.GetUnixTimeStampFromDate(expirationTime).ToString();
            HttpCookie cookie = new HttpCookie(cookieKey);
            cookie.Values["IsCookieExtendable"] = isCookieExtendable.ToString();
            cookie.Values["Hash"] = GenerateHash( isCookieExtendable.ToString(), expirationTimeString, secretKey);
            cookie.Values["Expires"] = expirationTimeString;
            if (!string.IsNullOrEmpty(cookieDomain))
                cookie.Domain = cookieDomain;
            cookie.Expires = DateTime.UtcNow.AddDays(1);
            cookie.HttpOnly = true;
            return cookie;
        }
        private bool IsCookieValid(
            string secretKey,
            HttpCookie cookie,
            string eventId)
        {
            var storedHash = cookie.Values["Hash"];
            var expirationTimeString = cookie.Values["Expires"];
            var cookieExtensibility = cookie.Values["IsCookieExtendable"];
            var expectedHash = GenerateHash( cookieExtensibility, expirationTimeString, secretKey);
            if (!expectedHash.Equals(storedHash))
                return false;
            var expirationTime = DateTimeHelper.GetUnixTimeStampAsDate(expirationTimeString);
            if (expirationTime < DateTime.UtcNow)
                return false;
            return true;
        }

    }
}
