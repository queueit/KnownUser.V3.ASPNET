using System;
using System.Linq;
using System.Web;

namespace QueueIT.KnownUserV3.SDK
{
    internal interface IUserInQueueStateRepository
    {
        void Store(
            string eventId,
            string queueId,
            bool isStateExtendable,
            string cookieDomain,
            int cookieValidityMinute,
            string customerSecretKey);

        StateInfo GetState(
            string eventId,
            string customerSecretKey);

        void CancelQueueCookie(
            string eventId, 
            string cookieDomain);

        void ExtendQueueCookie(
            string eventId,
            int cookieValidityMinute,
            string secretKey);
    }

    internal class UserInQueueStateCookieRepository : IUserInQueueStateRepository
    {
        private const string _QueueITDataKey = "QueueITAccepted-SDFrts345E-V3";
        private const string _QueueITRunTimeInfoKey = "QueueITRuntimeInfo-SDFrts345E-V3";
        private const string _IsCookieExtendableKey = "IsCookieExtendable";
        private const string _HashKey = "Hash";
        private const string _ExpiresKey = "Expires";
        private const string _QueueIdKey = "QueueId";

        private HttpContextBase _httpContext;

        internal static string GetCookieKey(string eventId)
        {
            return $"{_QueueITDataKey}_{eventId}";
        }

        public UserInQueueStateCookieRepository(HttpContextBase httpContext)
        {
            this._httpContext = httpContext;
        }

        public void Store(
            string eventId,
            string queueId,
            bool isStateExtendable,
            string cookieDomain,
            int cookieValidityMinute,
            string secretKey)
        {
            var cookieKey = GetCookieKey(eventId);
            var cookie = CreateCookie(eventId, queueId, isStateExtendable, cookieValidityMinute, cookieDomain, secretKey);

            if (_httpContext.Response.Cookies.AllKeys.Any(key => key == cookieKey))
                _httpContext.Response.Cookies.Remove(cookieKey);

            _httpContext.Response.Cookies.Add(cookie);
        }

        public StateInfo GetState(string eventId, string secretKey)
        {
            try
            {
                var cookieKey = GetCookieKey(eventId);
                HttpCookie cookie = _httpContext.Request.Cookies.Get(cookieKey);
                if (cookie == null)
                    return new StateInfo(false, string.Empty, false);
                if (!IsCookieValid(secretKey, cookie, eventId))
                    return new StateInfo(false, string.Empty, false);

                return new StateInfo(true, cookie[_QueueIdKey], bool.Parse(cookie[_IsCookieExtendableKey])); ;
            }
            catch (Exception)
            {
                return new StateInfo(false, string.Empty, false);
            }
        }

        private string GenerateHash(
            string queueId,
            string isCookieExtendable,
            string expirationTime,
            string secretKey)
        {
            string valueToHash = string.Concat(queueId, isCookieExtendable, expirationTime, secretKey);
            return HashHelper.GenerateSHA256Hash(secretKey, valueToHash);
        }

        public void CancelQueueCookie(string eventId, string cookieDomain)
        {
            var cookieKey = GetCookieKey(eventId);

            var cookie = new HttpCookie(cookieKey);
            cookie.Expires = DateTime.UtcNow.AddDays(-1d);

            if (!string.IsNullOrEmpty(cookieDomain))
                cookie.Domain = cookieDomain;

            _httpContext.Response.Cookies.Add(cookie);
        }

        public void ExtendQueueCookie(
            string eventId,
            int cookieValidityMinute,
            string secretKey)
        {
            var cookieKey = GetCookieKey(eventId);
            HttpCookie cookie = _httpContext.Request.Cookies.Get(cookieKey);

            if (cookie == null)
                return;

            if (!IsCookieValid(secretKey, cookie, eventId))
                return;

            var newCookie = CreateCookie(eventId, cookie.Values[_QueueIdKey], bool.Parse(cookie.Values[_IsCookieExtendableKey]),
                cookieValidityMinute, cookie.Domain, secretKey);

            if (_httpContext.Response.Cookies.AllKeys.Any(key => key == cookieKey))
                _httpContext.Response.Cookies.Remove(cookieKey);

            _httpContext.Response.Cookies.Add(newCookie);
        }

        private HttpCookie CreateCookie(
            string eventId,
            string queueId,
            bool isCookieExtendable,
            int cookieValidityMinute,
            string cookieDomain,
            string secretKey)
        {
            var cookieKey = GetCookieKey(eventId);
            var expirationTime = DateTime.UtcNow.Add(TimeSpan.FromMinutes(cookieValidityMinute));
            var expirationTimeString = DateTimeHelper.GetUnixTimeStampFromDate(expirationTime).ToString();

            HttpCookie cookie = new HttpCookie(cookieKey);
            cookie.Values[_IsCookieExtendableKey] = isCookieExtendable.ToString();
            cookie.Values[_HashKey] = GenerateHash(queueId, isCookieExtendable.ToString(), expirationTimeString, secretKey);
            cookie.Values[_ExpiresKey] = expirationTimeString;
            cookie.Values[_QueueIdKey] = queueId;

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
            var storedHash = cookie.Values[_HashKey];
            var expirationTimeString = cookie.Values[_ExpiresKey];
            var cookieExtensibility = cookie.Values[_IsCookieExtendableKey];
            var queueId = cookie.Values[_QueueIdKey];

            var expectedHash = GenerateHash(queueId, cookieExtensibility, expirationTimeString, secretKey);
            if (!expectedHash.Equals(storedHash))
                return false;

            var expirationTime = DateTimeHelper.GetUnixTimeStampAsDate(expirationTimeString);
            if (expirationTime < DateTime.UtcNow)
                return false;

            return true;
        }
    }

    class StateInfo
    {
        public bool IsValid { get; private set; }
        public string QueueId { get; private set; }
        public bool IsStateExtendable { get; private set; }
        public StateInfo(bool isValid, string queueId, bool isStateExtendable)
        {
            this.IsValid = isValid;
            this.QueueId = queueId;
            this.IsStateExtendable = isStateExtendable;
        }
    }
}
