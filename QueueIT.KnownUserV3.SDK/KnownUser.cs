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

        public static RequestValidationResult ValidateRequestByIntegrationConfig(
            string currentUrlWithoutQueueITToken, string queueitToken,
            CustomerIntegration customerIntegrationInfo, string customerId, string secretKey)
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
                GetHttpContextBase()?.Request);

            if (isDebug)
            {
                DoCookieLog(new Dictionary<string, string>() { { "matchedConfig", matchedConfig != null ? matchedConfig.Name : "NULL" } });
            }
            if (matchedConfig == null)
                return new RequestValidationResult(null);

            if (string.IsNullOrEmpty(matchedConfig.ActionType) || matchedConfig.ActionType == ActionType.QueueAction)
            {
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

                var queueEventConfig = new QueueEventConfig()
                {
                    QueueDomain = matchedConfig.QueueDomain,
                    Culture = matchedConfig.Culture,
                    EventId = matchedConfig.EventId,
                    ExtendCookieValidity = matchedConfig.ExtendCookieValidity.Value,
                    LayoutName = matchedConfig.LayoutName,
                    CookieValidityMinute = matchedConfig.CookieValidityMinute.Value,
                    CookieDomain = matchedConfig.CookieDomain,
                    Version = customerIntegrationInfo.Version
                };

                return ResolveQueueRequestByLocalConfig(targetUrl, queueitToken, queueEventConfig, customerId, secretKey);
            }
            else // CancelQueueAction
            {
                var cancelEventConfig = new CancelEventConfig()
                {
                    QueueDomain = matchedConfig.QueueDomain,
                    EventId = matchedConfig.EventId,
                    Version = customerIntegrationInfo.Version,
                    CookieDomain = matchedConfig.CookieDomain
                };

                return CancelRequestByLocalConfig(currentUrlWithoutQueueITToken, queueitToken, cancelEventConfig, customerId, secretKey);
            }
        }

        public static RequestValidationResult CancelRequestByLocalConfig(
            string targetUrl, string queueitToken, CancelEventConfig cancelConfig,
            string customerId, string secretKey)
        {
            if (GetIsDebug(queueitToken, secretKey))
            {
                var dic = new Dictionary<string, string>();
                dic.Add("targetUrl", targetUrl);
                dic.Add("queueitToken", queueitToken);
                dic.Add("cancelConfig", cancelConfig != null ? cancelConfig.ToString() : "NULL");
                dic.Add("OriginalURL", GetHttpContextBase().Request.Url.AbsoluteUri);
                DoCookieLog(dic);
            }
            if (string.IsNullOrEmpty(targetUrl))
                throw new ArgumentException("targeturl can not be null or empty.");
            if (string.IsNullOrEmpty(customerId))
                throw new ArgumentException("customerId can not be null or empty.");
            if (string.IsNullOrEmpty(secretKey))
                throw new ArgumentException("secretKey can not be null or empty.");
            if (cancelConfig == null)
                throw new ArgumentException("cancelEventConfig can not be null.");
            if (string.IsNullOrEmpty(cancelConfig.EventId))
                throw new ArgumentException("EventId from cancelEventConfig can not be null or empty.");
            if (string.IsNullOrEmpty(cancelConfig.QueueDomain))
                throw new ArgumentException("QueueDomain from cancelEventConfig can not be null or empty.");

            var userInQueueService = GetUserInQueueService();
            return userInQueueService.ValidateCancelRequest(targetUrl, cancelConfig, customerId, secretKey);
        }

        public static RequestValidationResult ResolveQueueRequestByLocalConfig(
            string targetUrl, string queueitToken, QueueEventConfig queueConfig,
            string customerId, string secretKey)
        {
            if (GetIsDebug(queueitToken, secretKey))
            {
                var dic = new Dictionary<string, string>();
                dic.Add("targetUrl", targetUrl);
                dic.Add("queueitToken", queueitToken);
                dic.Add("queueConfig", queueConfig != null ? queueConfig.ToString() : "NULL");
                dic.Add("OriginalURL", GetHttpContextBase().Request.Url.AbsoluteUri);
                DoCookieLog(dic);
            }
            if (string.IsNullOrEmpty(customerId))
                throw new ArgumentException("customerId can not be null or empty.");
            if (string.IsNullOrEmpty(secretKey))
                throw new ArgumentException("secretKey can not be null or empty.");
            if (queueConfig == null)
                throw new ArgumentException("eventConfig can not be null.");
            if (string.IsNullOrEmpty(queueConfig.EventId))
                throw new ArgumentException("EventId from eventConfig can not be null or empty.");
            if (string.IsNullOrEmpty(queueConfig.QueueDomain))
                throw new ArgumentException("QueueDomain from eventConfig can not be null or empty.");
            if (queueConfig.CookieValidityMinute <= 0)
                throw new ArgumentException("CookieValidityMinute from eventConfig should be greater than 0.");

            queueitToken = queueitToken ?? string.Empty;

            var userInQueueService = GetUserInQueueService();
            return userInQueueService.ValidateQueueRequest(targetUrl, queueitToken, queueConfig, customerId, secretKey);
        }

        public static void ExtendQueueCookie(
            string eventId,
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
        private static bool GetIsDebug(string queueitToken, string secretKey)
        {
            var qParams = QueueParameterHelper.ExtractQueueParams(queueitToken);
            if (qParams != null && qParams.RedirectType != null && qParams.RedirectType.ToLower() == "debug")
            {
                return HashHelper.GenerateSHA256Hash(secretKey, qParams.QueueITTokenWithoutHash) == qParams.HashCode;
            }
            return false;

        }
    }
}
