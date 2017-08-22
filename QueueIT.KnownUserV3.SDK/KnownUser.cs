using QueueIT.KnownUserV3.SDK.IntegrationConfig;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QueueIT.KnownUserV3.SDK
{
    public static class KnownUser
    {

        public const string QueueITTokenKey = "queueittoken";
        public const string QueueITDebugKey = "queueitdebug";

        public static RequestValidationResult ValidateRequestByIntegrationConfig(string currentUrlWithoutQueueITToken,
          string queueitToken, CustomerIntegration customerIntegrationInfo,
          string customerId, string secretKey)
        {
            var isDebug = GetIsDebug(queueitToken, secretKey);
            if (isDebug)
            {
                var dic = new Dictionary<string, string>();
                dic.Add("configVersion", customerIntegrationInfo.Version.ToString());
                dic.Add("pureUrl", currentUrlWithoutQueueITToken);
                dic.Add("queueitToken", queueitToken);
                dic.Add("OriginalURL", GetHttpContextBase().Request.Url.AbsoluteUri);
                DoCookieLog(dic);
            }
            if (string.IsNullOrEmpty(currentUrlWithoutQueueITToken))
                throw new ArgumentException("currentUrlWithoutQueueITToken can not be null or empty.");
            if (customerIntegrationInfo == null)
                throw new ArgumentException("customerIntegrationInfo can not be null.");

            var configEvaluater = new IntegrationEvaluator();

            var matchedConfig = configEvaluater.GetMatchedIntegrationConfig(
                customerIntegrationInfo,
                currentUrlWithoutQueueITToken,
                GetHttpContextBase()?.Request?.Cookies,
                GetHttpContextBase()?.Request?.UserAgent ?? string.Empty
                );

            if (isDebug)
            {
                DoCookieLog(new Dictionary<string, string>() { { "matchedConfig", matchedConfig != null ? matchedConfig.Name : "NULL" } });
            }
            if (matchedConfig == null)
                return new RequestValidationResult();

            var targetUrl = "";
            switch (matchedConfig.RedirectLogic)
            {
                case "ForcedTargetUrl":
                case "ForecedTargetUrl":
                    targetUrl = matchedConfig.ForcedTargetUrl;
                    break;
                case "EventTargetUrl":
                    targetUrl = "";
                    break;
                default:
                    targetUrl = currentUrlWithoutQueueITToken;
                    break;
            }

            var eventConfig = new EventConfig()
            {
                QueueDomain = matchedConfig.QueueDomain,
                Culture = matchedConfig.Culture,
                EventId = matchedConfig.EventId,
                ExtendCookieValidity = matchedConfig.ExtendCookieValidity,
                LayoutName = matchedConfig.LayoutName,
                CookieValidityMinute = matchedConfig.CookieValidityMinute,
                CookieDomain = matchedConfig.CookieDomain,
                Version = customerIntegrationInfo.Version
            };

            return ValidateRequestByLocalEventConfig(targetUrl, queueitToken, eventConfig, customerId, secretKey);
        }

        private static bool GetIsDebug(string queueitToken, string secretKey)
        {
            var qParams = QueueParameterHelper.ExtractQueueParams(queueitToken);
            if (qParams != null && qParams.RedirectType != null && qParams.RedirectType.ToLower() == "debug")
            {
                return HashHelper.GenerateSHA256Hash(secretKey, qParams.QueueITTokenWithoutHash) == qParams.HashCode;
            }
            return false;

        }

        public static RequestValidationResult ValidateRequestByLocalEventConfig(string targetUrl, string queueitToken, EventConfig eventConfig,
            string customerId, string secretKey)
        {

            if (GetIsDebug(queueitToken, secretKey))
            {
                var dic = new Dictionary<string, string>();
                dic.Add("targetUrl", targetUrl);
                dic.Add("queueitToken", queueitToken);
                dic.Add("EventConfig", eventConfig != null ? eventConfig.ToString() : "NULL");
                dic.Add("OriginalURL", GetHttpContextBase().Request.Url.AbsoluteUri);
                DoCookieLog(dic);
            }
            if (string.IsNullOrEmpty(customerId))
                throw new ArgumentException("customerId can not be null or empty.");
            if (string.IsNullOrEmpty(secretKey))
                throw new ArgumentException("secretKey can not be null or empty.");
            if (eventConfig == null)
                throw new ArgumentException("eventConfig can not be null.");
            if (string.IsNullOrEmpty(eventConfig.EventId))
                throw new ArgumentException("EventId from eventConfig can not be null or empty.");
            if (string.IsNullOrEmpty(eventConfig.QueueDomain))
                throw new ArgumentException("QueueDomain from eventConfig can not be null or empty.");
            if (eventConfig.CookieValidityMinute <= 0)
                throw new ArgumentException("CookieValidityMinute from eventConfig should be greater than 0.");
            queueitToken = queueitToken ?? string.Empty;
            var userInQueueService = GetUserInQueueService();
            return userInQueueService.ValidateRequest(targetUrl, queueitToken, eventConfig, customerId, secretKey);
        }


        public static void CancelQueueCookie(string eventId)
        {
            if (string.IsNullOrEmpty(eventId))
                throw new ArgumentException("eventId can not be null or empty.");

            var userInQueueService = GetUserInQueueService();
            userInQueueService.CancelQueueCookie(eventId);
        }
        public static void ExtendQueueCookie(string eventId,
            int cookieValidityMinute,
            string secretKey)
        {
            if (string.IsNullOrEmpty(eventId))
                throw new ArgumentException("eventId can not be null or empty.");
            if (cookieValidityMinute <= 0)
                throw new ArgumentException("cookieValidityMinute should be greater than 0.");
            if (string.IsNullOrEmpty(secretKey))
                throw new ArgumentException("secretKey can not be null or empty.");

            var userInQueueService = GetUserInQueueService();
            userInQueueService.ExtendQueueCookie(eventId, cookieValidityMinute, secretKey);
        }

        //used for unit testing
        internal static HttpContextBase _HttpContextBase;
        internal static IUserInQueueService _UserInQueueService;

        private static IUserInQueueService GetUserInQueueService()
        {
            if (_UserInQueueService == null)
                return new UserInQueueService(new UserInQueueStateCookieRepository(new HttpContextWrapper(HttpContext.Current)));
            return _UserInQueueService;
        }
        private static HttpContextBase GetHttpContextBase()
        {
            if (_UserInQueueService == null)
                return new HttpContextWrapper(HttpContext.Current);
            return _HttpContextBase;
        }

        internal static void DoCookieLog(Dictionary<string, string> dic)
        {
            if (!GetHttpContextBase().Response.Cookies.AllKeys.Any(key => key == QueueITDebugKey))
            {
                var cookie = new HttpCookie(QueueITDebugKey);
                GetHttpContextBase().Response.Cookies.Add(cookie);
            }

            var debugCookie = GetHttpContextBase().Response.Cookies[QueueITDebugKey];
            foreach (var nameVal in dic)
            {
                if (!debugCookie.Values.AllKeys.Any(key => key == nameVal.Key))
                {
                    debugCookie.Values.Add(nameVal.Key, nameVal.Value);
                }
            }
        }
    }
}
