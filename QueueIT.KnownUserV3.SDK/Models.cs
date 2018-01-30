using System;

namespace QueueIT.KnownUserV3.SDK
{
    public class RequestValidationResult
    {
        public RequestValidationResult(string actionType)
        {
            ActionType = actionType;
        }

        public string RedirectUrl { get; internal set; }
        public string QueueId { get; internal set; }
        public bool DoRedirect
        {
            get
            {
                return !string.IsNullOrEmpty(RedirectUrl);
            }
        }
        public string EventId { get; internal set; }
        public string ActionType { get; internal set; }
        public string RedirectType { get; internal set; }
    }

    public class QueueEventConfig
    {
        public QueueEventConfig()
        {
            Version = -1;
        }
        public string EventId { get; set; }
        public string LayoutName { get; set; }
        public string Culture { get; set; }
        public string QueueDomain { get; set; }
        public bool ExtendCookieValidity { get; set; }
        public int CookieValidityMinute { get; set; }
        public string CookieDomain { get; set; }
        public int Version { get; set; }
        public override string ToString()
        {
            return $"EventId:{EventId}&Version:{Version}" +
                $"&QueueDomain:{QueueDomain}&CookieDomain:{CookieDomain}&ExtendCookieValidity:{ExtendCookieValidity}" +
                $"&CookieValidityMinute:{CookieValidityMinute}&LayoutName:{LayoutName}&Culture:{Culture}";
        }
    }

    public class CancelEventConfig
    {
        public CancelEventConfig()
        {
            Version = -1;
        }
        public string EventId { get; set; }
        public string QueueDomain { get; set; }
        public int Version { get; set; }
        public string CookieDomain { get; set; }
        public override string ToString()
        {
            return $"EventId:{EventId}&Version:{Version}" +
                $"&QueueDomain:{QueueDomain}&CookieDomain:{CookieDomain}";
        }
    }
}
