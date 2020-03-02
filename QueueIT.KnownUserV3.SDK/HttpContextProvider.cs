using System;
using System.Collections.Specialized;
using System.Linq;
using System.Web;

namespace QueueIT.KnownUserV3.SDK
{
    class HttpContextProvider : IHttpContextProvider
    {
        public IHttpRequest HttpRequest { get; } = new HttpRequest();

        public IHttpResponse HttpResponse { get; } = new HttpResponse();

        public static IHttpContextProvider Instance { get; private set; } = new HttpContextProvider();
    }

    class HttpRequest : IHttpRequest
    {
        public string UserAgent => HttpContext.Current.Request.UserAgent;

        public NameValueCollection Headers => HttpContext.Current.Request.Headers;

        public Uri Url => HttpContext.Current.Request.Url;

        public string UserHostAddress => HttpContext.Current.Request.UserHostAddress;

        public string GetCookieValue(string cookieKey)
        {
            var cookieValue = HttpContext.Current.Request.Cookies[cookieKey]?.Value;
            if (cookieValue == null)
                return null;
            return HttpUtility.UrlDecode(cookieValue);
        }
    }

    class HttpResponse : IHttpResponse
    {
        public void SetCookie(string cookieName, string cookieValue, string domain, DateTime expiration)
        {
            if (HttpContext.Current.Response.
                Cookies.AllKeys.Any(key => key == KnownUser.QueueITDebugKey))
            {
                HttpContext.Current.Response.Cookies.Remove(KnownUser.QueueITDebugKey);
            }
            var cookie = new HttpCookie(cookieName, Uri.EscapeDataString(cookieValue));
            if (!string.IsNullOrEmpty(domain))
            {
                cookie.Domain = domain;
            }
            cookie.HttpOnly = false;
            cookie.Expires = expiration;
            HttpContext.Current.Response.Cookies.Add(cookie);
        }
    }
}
