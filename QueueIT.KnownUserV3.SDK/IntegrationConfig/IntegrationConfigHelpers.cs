﻿using System;
using System.Collections.Specialized;
using System.Text.RegularExpressions;
using System.Web;

namespace QueueIT.KnownUserV3.SDK.IntegrationConfig
{
    internal class IntegrationEvaluator : IIntegrationEvaluator
    {
        public IntegrationConfigModel GetMatchedIntegrationConfig(CustomerIntegration customerIntegration, string currentPageUrl, HttpRequestBase request)
        {
            if (request == null)
                throw new ArgumentException("request is null");

            foreach (var integration in customerIntegration.Integrations)
            {
                foreach (var trigger in integration.Triggers)
                {
                    if (EvaluateTrigger(trigger, currentPageUrl, request))
                    {
                        return integration;
                    }
                }
            }
            return null;
        }

        private bool EvaluateTrigger(TriggerModel trigger, string currentPageUrl, HttpRequestBase request)
        {
            if (trigger.LogicalOperator == LogicalOperatorType.Or)
            {
                foreach (var part in trigger.TriggerParts)
                {
                    if (EvaluateTriggerPart(part, currentPageUrl, request))
                        return true;
                }
                return false;
            }
            else
            {
                foreach (var part in trigger.TriggerParts)
                {
                    if (!EvaluateTriggerPart(part, currentPageUrl, request))
                        return false;
                }
                return true;
            }
        }

        private bool EvaluateTriggerPart(TriggerPart triggerPart, string currentPageUrl, HttpRequestBase request)
        {
            switch (triggerPart.ValidatorType)
            {
                case ValidatorType.UrlValidator:
                    return UrlValidatorHelper.Evaluate(triggerPart, currentPageUrl);
                case ValidatorType.CookieValidator:
                    return CookieValidatorHelper.Evaluate(triggerPart, request.Cookies);
                case ValidatorType.UserAgentValidator:
                    return UserAgentValidatorHelper.Evaluate(triggerPart, request.UserAgent);
                case ValidatorType.HttpHeaderValidator:
                    return HttpHeaderValidatorHelper.Evaluate(triggerPart, request.Headers);
                default:
                    return false;
            }
        }
    }

    internal interface IIntegrationEvaluator
    {
        IntegrationConfigModel GetMatchedIntegrationConfig(
            CustomerIntegration customerIntegration, string currentPageUrl, HttpRequestBase request);
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
        public static bool Evaluate(TriggerPart triggerPart, HttpCookieCollection cookieCollection)
        {
            return ComparisonOperatorHelper.Evaluate(triggerPart.Operator,
                triggerPart.IsNegative,
                triggerPart.IsIgnoreCase,
                GetCookie(triggerPart.CookieName, cookieCollection),
                triggerPart.ValueToCompare);
        }

        private static string GetCookie(string cookieName, HttpCookieCollection cookieCollection)
        {
            var cookie = cookieCollection?.Get(cookieName);

            if (cookie == null)
                return string.Empty;

            return cookieCollection[cookieName].Value;
        }
    }

    internal static class UserAgentValidatorHelper
    {
        public static bool Evaluate(TriggerPart triggerPart, string userAgent)
        {
            return ComparisonOperatorHelper.Evaluate(triggerPart.Operator,
                triggerPart.IsNegative,
                triggerPart.IsIgnoreCase,
                userAgent ?? string.Empty,
                triggerPart.ValueToCompare);
        }
    }

    internal static class HttpHeaderValidatorHelper
    {
        public static bool Evaluate(TriggerPart triggerPart, NameValueCollection httpHeaders)
        {
            return ComparisonOperatorHelper.Evaluate(triggerPart.Operator,
                triggerPart.IsNegative,
                triggerPart.IsIgnoreCase,
                GetHttpHeader(triggerPart.HttpHeaderName, httpHeaders),
                triggerPart.ValueToCompare);
        }

        private static string GetHttpHeader(string httpHeaderName, NameValueCollection httpHeaders)
        {
            var header = httpHeaders?.Get(httpHeaderName);

            if (header == null)
                return string.Empty;

            return httpHeaders[httpHeaderName];
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
