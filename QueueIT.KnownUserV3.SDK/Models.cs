using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueueIT.KnownUserV3.SDK
{
    public class RequestValidationResult
    {
        public string RedirectUrl { get; set; }
        public string QueueId { get; set; }
        public bool DoRedirect
        {
            get
            {
                return !string.IsNullOrEmpty(RedirectUrl);
            }
        }
        public string EventId { get; set; }
    }

    public class EventConfig
    {
        public EventConfig()
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
    }

    public class KnowUserException : Exception
    {
        public KnowUserException(string message) : base(message)
        { }
        public KnowUserException(string message, Exception innerException) : base(message, innerException)
        { }
    }
}
