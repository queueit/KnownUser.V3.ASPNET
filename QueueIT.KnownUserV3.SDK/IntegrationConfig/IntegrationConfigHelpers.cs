using System.Text.RegularExpressions;
using System.Web;

namespace QueueIT.KnownUserV3.SDK.IntegrationConfig
{
    internal class IntegrationEvaluator : IIntegrationEvaluator
    {
        HttpRequestBase _httpRequest;
        public HttpRequestBase HttpRequest
        {
            get
            {
                if (_httpRequest == null)
                {
                    this._httpRequest = new HttpRequestWrapper(HttpContext.Current.Request);
                }
                return this._httpRequest;

            }
            set
            {
                this._httpRequest = value;
            }
        }

        public IntegrationConfigModel GetMatchedIntegrationConfig(CustomerIntegration customerIntegration,
            string currentPageUrl)
        {
            foreach (var integration in customerIntegration.Integrations)
            {
                foreach (var trigger in integration.Triggers)
                {
                    if (EvaluateTrigger(trigger, currentPageUrl))
                    {
                        return integration;
                    }
                }
            }
            return null;
        }

        private bool EvaluateTrigger(TriggerModel trigger, string currentPageUrl)
        {
            if (trigger.LogicalOperator == LogicalOperatorType.Or)
            {
                foreach (var part in trigger.TriggerParts)
                {
                    if (EvaluateTriggerPart(part, currentPageUrl))
                        return true;
                }
                return false;
            }
            else
            {
                foreach (var part in trigger.TriggerParts)
                {
                    if (!EvaluateTriggerPart(part, currentPageUrl))
                        return false;
                }
                return true;
            }
        }

        private bool EvaluateTriggerPart(TriggerPart triggerPart, string currentPageUrl)
        {
            switch (triggerPart.ValidatorType)
            {
                case ValidatorType.UrlValidator:

                    return UrlValidatorHelper.Evaluate(triggerPart, currentPageUrl);
                case ValidatorType.CookieValidator:
                    return CookieValidatorHelper.Evaluate(triggerPart, HttpRequest.Cookies);
                default:
                    return false;
            }
        }
    }

    internal interface IIntegrationEvaluator
    {
        IntegrationConfigModel GetMatchedIntegrationConfig(CustomerIntegration customerIntegration,
             string currentPageUrl);
    }

    internal class UrlValidatorHelper
    {
        public static bool Evaluate(TriggerPart triggerPart, string url)
        {
            return ComparisonOperatorHelper.Evaluate(
                triggerPart.Operator,
                triggerPart.IsNegative,
                triggerPart.IsIgnoreCase,
                GetUrlPart(triggerPart, url),
                triggerPart.ValueToCompare);
        }

        private static string GetUrlPart(TriggerPart triggerPart, string url)
        {
            switch (triggerPart.UrlPart)
            {
                case UrlPartType.PagePath:
                    return GetPathFromUrl(url);
                case UrlPartType.PageUrl:
                    return url;
                case UrlPartType.HostName:
                    return GetHostNameFromUrl(url);
                default:
                    return string.Empty;
            }
        }

        public static string GetHostNameFromUrl(string url)
        {
            string urlMatcher = @"^(([^:/\?#]+):)?("
                + @"//(?<hostname>[^/\?#]*))?([^\?#]*)"
                + @"(\?([^#]*))?"
                + @"(#(.*))?";
            Regex re = new Regex(urlMatcher, RegexOptions.ExplicitCapture);
            Match m = re.Match(url);
            if (!m.Success)
                return string.Empty;
            return m.Groups["hostname"].Value;
        }

        public static string GetPathFromUrl(string url)
        {
            string urlMatcher = @"^(([^:/\?#]+):)?("
                + @"//([^/\?#]*))?(?<path>[^\?#]*)"
                + @"(\?([^#]*))?"
                + @"(#(.*))?";
            Regex re = new Regex(urlMatcher, RegexOptions.ExplicitCapture);
            Match m = re.Match(url);
            if (!m.Success)
                return string.Empty;
            return m.Groups["path"].Value;
        }
    }

    internal static class CookieValidatorHelper
    {
        public static bool Evaluate(TriggerPart triggerPart, System.Web.HttpCookieCollection cookieCollection)
        {
            return ComparisonOperatorHelper.Evaluate(triggerPart.Operator,
                triggerPart.IsNegative,
                triggerPart.IsIgnoreCase,
                GetCookie(triggerPart.CookieName, cookieCollection),
                triggerPart.ValueToCompare);
        }

        private static string GetCookie(string cookieName, System.Web.HttpCookieCollection cookieCollection)
        {
            var cookie = cookieCollection.Get(cookieName);
            if (cookie == null)
                return string.Empty;
            return cookieCollection[cookieName].Value;
        }
    }

    internal static class ComparisonOperatorHelper
    {
        public static bool Evaluate(string opt, bool isNegative, bool isIgnoreCase, string left, string right)
        {
            left = left ?? string.Empty;
            right = right ?? string.Empty;
            switch (opt)
            {
                case ComparisonOperatorType.EqualS:
                    return EqualS(left, right, isNegative, isIgnoreCase);

                case ComparisonOperatorType.Contains:
                    return Contains(left, right, isNegative, isIgnoreCase);

                case ComparisonOperatorType.StartsWith:
                    return StartsWith(left, right, isNegative, isIgnoreCase);
                case ComparisonOperatorType.EndsWith:
                    return EndsWith(left, right, isNegative, isIgnoreCase);
                case ComparisonOperatorType.MatchesWith:
                    return MatchesWith(left, right, isNegative, isIgnoreCase);


                default:
                    return false;
            }
        }

        private static bool Contains(string left, string right, bool isNegative, bool ignoreCase)
        {
            if (right == "*")
                return true;
            var evaluation = false;
            if (ignoreCase)
                evaluation = left.ToUpper().Contains(right.ToUpper());
            else
                evaluation = left.Contains(right);
            if (isNegative)
                return !evaluation;
            else
                return evaluation;
        }

        private static bool EqualS(string left, string right, bool isNegative, bool ignoreCase)
        {
            var evaluation = false;

            if (ignoreCase)
                evaluation = left.ToUpper() == right.ToUpper();
            else
                evaluation = left == right;


            if (isNegative)
                return !evaluation;
            else
                return evaluation;
        }

        private static bool EndsWith(string left, string right, bool isNegative, bool ignoreCase)
        {
            var evaluation = false;

            if (ignoreCase)
                evaluation = left.ToUpper().EndsWith(right.ToUpper());
            else
                evaluation = left.EndsWith(right);

            if (isNegative)
                return !evaluation;
            else
                return evaluation;
        }

        private static bool StartsWith(string left, string right, bool isNegative, bool ignoreCase)
        {
            var evaluation = false;

            if (ignoreCase)
                evaluation = left.ToUpper().StartsWith(right.ToUpper());
            else
                evaluation = left.StartsWith(right);
            if (isNegative)
                return !evaluation;
            else
                return evaluation;
        }

        private static bool MatchesWith(string left, string right, bool isNegative, bool isIgnoreCase)
        {
            Regex rg = null;
            if (isIgnoreCase)
                rg = new Regex(right, RegexOptions.IgnoreCase);
            else
                rg = new Regex(right);

            var evaluation = rg.IsMatch(left);
            if (isNegative)
                return !evaluation;
            else
                return evaluation;
        }
    }
}
