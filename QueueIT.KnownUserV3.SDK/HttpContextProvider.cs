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
        public string UserAgent => System.Web.HttpContext.Current.Request.UserAgent;

        public NameValueCollection Headers => System.Web.HttpContext.Current.Request.Headers;

        public Uri Url => System.Web.HttpContext.Current.Request.Url;

        public string UserHostAddress => System.Web.HttpContext.Current.Request.UserHostAddress;

        public string GetCookieValue(string cookieKey)
        {
            var cookieValue = System.Web.HttpContext.Current.Request.Cookies[cookieKey]?.Value;
            if (cookieValue == null)
                return null;
            return HttpUtility.UrlDecode(cookieValue);
        }
    }

    class HttpResponse : IHttpResponse
    {
        public void SetCookie(string cookieName, string cookieValue, string domain, DateTime expiration)
        {
            if (System.Web.HttpContext.Current.Response.
                Cookies.AllKeys.Any(key => key == KnownUser.QueueITDebugKey))
            {
                System.Web.HttpContext.Current.Response.Cookies.Remove(KnownUser.QueueITDebugKey);
            }
            var cookie = new System.Web.HttpCookie(cookieName, HttpUtility.UrlEncode(cookieValue));            
            if (!string.IsNullOrEmpty(domain))
            {
                cookie.Domain = domain;
            }
            cookie.HttpOnly = false;
            cookie.Expires = expiration;
            System.Web.HttpContext.Current.Response.Cookies.Add(cookie);
        }
    }
}
