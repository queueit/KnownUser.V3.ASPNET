using System;
using System.Collections.Specialized;
using System.Linq;
using System.Web;

namespace QueueIT.KnownUserV3.SDK
{
    public class SDKInitializer
    {
        public static void SetHttpRequest(IHttpRequest httpRequest)
        {
            HttpContextProvider.SetHttpRequest(httpRequest);
        }
    }

    class HttpContextProvider : IHttpContextProvider
    {
        IHttpRequest _httpRequest;
        public IHttpRequest HttpRequest => _httpRequest ?? (_httpRequest = new HttpRequest());

        IHttpResponse _httpResponse;
        public IHttpResponse HttpResponse => _httpResponse ?? (_httpResponse = new HttpResponse());

        public static IHttpContextProvider Instance { get; } = new HttpContextProvider();

        public static void SetHttpRequest(IHttpRequest httpRequest)
        {
            ((HttpContextProvider)Instance)._httpRequest = httpRequest;
        }
    }

    public class HttpRequest : IHttpRequest
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

        public virtual string GetRequestBodyAsString()
        {
            return string.Empty;
        }
    }

    internal class HttpResponse : IHttpResponse
    {
        public void SetCookie(string cookieName, string cookieValue, string domain, DateTime expiration, bool isHttpOnly, bool isSecure)
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
            cookie.HttpOnly = isHttpOnly;
            cookie.Secure = isSecure;
            cookie.Expires = expiration;

            HttpContext.Current.Response.Cookies.Add(cookie);
        }
    }
}
