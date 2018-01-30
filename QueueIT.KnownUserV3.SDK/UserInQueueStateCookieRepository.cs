using System;
using System.Collections.Specialized;
using System.Linq;
using System.Web;

namespace QueueIT.KnownUserV3.SDK
{
    internal interface IUserInQueueStateRepository
    {
        void Store(
            string eventId,
            string queueId,
            int? fixedCookieValidityMinutes,
            string cookieDomain,
            string redirectType,
            string secretKey);

        StateInfo GetState(
            string eventId,
            int cookieValidityMinutes,
            string secretKey,
            bool validateTime = true);

        void CancelQueueCookie(
            string eventId,
            string cookieDomain);

        void ReissueQueueCookie(
            string eventId,
            int cookieValidityMinutes,
            string secretKey);
    }

    internal class UserInQueueStateCookieRepository : IUserInQueueStateRepository
    {
        private const string _QueueITDataKey = "QueueITAccepted-SDFrts345E-V3";
        private const string _HashKey = "Hash";
        private const string _IssueTimeKey = "IssueTime";
        private const string _QueueIdKey = "QueueId";
        private const string _EventIdKey = "EventId";
        private const string _RedirectTypeKey = "RedirectType";
        private const string _FixedCookieValidityMinutesKey = "FixedValidityMins";

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
            int? fixedCookieValidityMinutes,
            string cookieDomain,
            string redirectType,
            string secretKey)
        {
            var cookieKey = GetCookieKey(eventId);

            var cookie = CreateCookie(
                eventId, queueId,
                Convert.ToString(fixedCookieValidityMinutes),
                redirectType, cookieDomain, secretKey);

            if (_httpContext.Response.Cookies.AllKeys.Any(key => key == cookieKey))
                _httpContext.Response.Cookies.Remove(cookieKey);

            _httpContext.Response.Cookies.Add(cookie);
        }

        public StateInfo GetState(string eventId,
            int cookieValidityMinutes,
            string secretKey,
            bool validateTime = true)
        {
            try
            {
                var cookieKey = GetCookieKey(eventId);
                HttpCookie cookie = _httpContext.Request.Cookies.Get(cookieKey);
                if (cookie == null)
                    return new StateInfo(false, string.Empty, null, string.Empty);

                var cookieValues = CookieHelper.ToNameValueCollectionFromValue(cookie.Value);
                if (!IsCookieValid(secretKey, cookieValues, eventId, cookieValidityMinutes, validateTime))
                    return new StateInfo(false, string.Empty, null, string.Empty);

                return new StateInfo(
                    true, cookieValues[_QueueIdKey],
                    !string.IsNullOrEmpty(cookieValues[_FixedCookieValidityMinutesKey])
                        ? int.Parse(cookieValues[_FixedCookieValidityMinutesKey]) : (int?)null,
                    cookieValues[_RedirectTypeKey]);
            }
            catch (Exception)
            {
                return new StateInfo(false, string.Empty, null, string.Empty);
            }
        }

        private string GenerateHash(
            string eventId,
            string queueId,
            string fixedCookieValidityMinutes,
            string redirectType,
            string issueTime,
            string secretKey)
        {
            string valueToHash = string.Concat(eventId, queueId, fixedCookieValidityMinutes, redirectType, issueTime);
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

        public void ReissueQueueCookie(
            string eventId,
            int cookieValidityMinutes,
            string secretKey)
        {
            var cookieKey = GetCookieKey(eventId);
            HttpCookie cookie = _httpContext.Request.Cookies.Get(cookieKey);

            if (cookie == null)
                return;

            var cookieValues = CookieHelper.ToNameValueCollectionFromValue(cookie.Value);

            if (!IsCookieValid(secretKey, cookieValues, eventId, cookieValidityMinutes, true))
                return;

            var newCookie = CreateCookie(
                eventId, cookieValues[_QueueIdKey],
                cookieValues[_FixedCookieValidityMinutesKey],
                cookieValues[_RedirectTypeKey],
                cookie.Domain, secretKey);

            if (_httpContext.Response.Cookies.AllKeys.Any(key => key == cookieKey))
                _httpContext.Response.Cookies.Remove(cookieKey);

            _httpContext.Response.Cookies.Add(newCookie);
        }

        private HttpCookie CreateCookie(
            string eventId,
            string queueId,
            string fixedCookieValidityMinutes,
            string redirectType,
            string cookieDomain,
            string secretKey)
        {
            var cookieKey = GetCookieKey(eventId);

            var issueTime = DateTimeHelper.GetUnixTimeStampFromDate(DateTime.UtcNow).ToString();

            NameValueCollection cookieValues = new NameValueCollection();
            cookieValues.Add(_EventIdKey, eventId);
            cookieValues.Add(_QueueIdKey, queueId);
            if (!string.IsNullOrEmpty(fixedCookieValidityMinutes))
            {
                cookieValues.Add(_FixedCookieValidityMinutesKey, fixedCookieValidityMinutes);
            }
            cookieValues.Add(_RedirectTypeKey, redirectType.ToLower());
            cookieValues.Add(_IssueTimeKey, issueTime);
            cookieValues.Add(_HashKey, GenerateHash(eventId.ToLower(), queueId, fixedCookieValidityMinutes, redirectType.ToLower(), issueTime, secretKey));

            HttpCookie cookie = new HttpCookie(cookieKey, CookieHelper.ToValueFromNameValueCollection(cookieValues));

            if (!string.IsNullOrEmpty(cookieDomain))
                cookie.Domain = cookieDomain;

            cookie.Expires = DateTime.UtcNow.AddDays(1);

            return cookie;
        }

        private bool IsCookieValid(
            string secretKey,
            NameValueCollection cookieValues,
            string eventId,
            int cookieValidityMinutes,
            bool validateTime)
        {
            var storedHash = cookieValues[_HashKey];
            var issueTimeString = cookieValues[_IssueTimeKey];
            var queueId = cookieValues[_QueueIdKey];
            var eventIdFromCookie = cookieValues[_EventIdKey];
            var redirectType = cookieValues[_RedirectTypeKey];
            var fixedCookieValidityMinutes = cookieValues[_FixedCookieValidityMinutesKey];

            var expectedHash = GenerateHash(
                eventIdFromCookie,
                queueId,
                fixedCookieValidityMinutes,
                redirectType,
                issueTimeString,
                secretKey);

            if (!expectedHash.Equals(storedHash))
                return false;

            if (eventId.ToLower() != eventIdFromCookie.ToLower())
                return false;

            if (validateTime)
            {
                var validity = !string.IsNullOrEmpty(fixedCookieValidityMinutes) ? int.Parse(fixedCookieValidityMinutes) : cookieValidityMinutes;
                var expirationTime = DateTimeHelper.GetDateTimeFromUnixTimeStamp(issueTimeString).AddMinutes(validity);
                if (expirationTime < DateTime.UtcNow)
                    return false;
            }
            return true;
        }
    }

    internal class StateInfo
    {
        public bool IsValid { get; private set; }
        public string QueueId { get; private set; }
        public bool IsStateExtendable
        {
            get
            {
                return IsValid && !FixedCookieValidityMinutes.HasValue;
            }
        }
        public int? FixedCookieValidityMinutes { get; private set; }
        public string RedirectType { get; private set; }

        public StateInfo(bool isValid, string queueId, int? fixedCookieValidityMinutes, string redirectType)
        {
            IsValid = isValid;
            QueueId = queueId;
            FixedCookieValidityMinutes = fixedCookieValidityMinutes;
            RedirectType = redirectType;
        }
    }
}
