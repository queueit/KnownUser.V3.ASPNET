using System;
using System.Web;
using System.Collections.Generic;
using QueueIT.KnownUserV3.SDK.IntegrationConfig;

namespace QueueIT.KnownUserV3.SDK
{
    internal interface IUserInQueueService
    {
        RequestValidationResult ValidateRequest(
            string targetUrl,
            string queueitToken,
            EventConfig config,
            string customerId,
             string secretKey);
        void CancelQueueCookie(string eventId);
        void ExtendQueueCookie(
           string eventId,
           int cookieValidityMinute,
           string secretKey
            );

    }
    internal class UserInQueueService : IUserInQueueService
    {
        private readonly IUserInQueueStateRepository _userInQueueStateRepository;
      

        public UserInQueueService(
            IUserInQueueStateRepository queueStateRepository)
        {
           
            this._userInQueueStateRepository = queueStateRepository;
           
        }





        public RequestValidationResult ValidateRequest(
            string targetUrl,
            string queueitToken,
            EventConfig config,
            string customerId,
            string secretKey
           )
        {

            if (this._userInQueueStateRepository.HasValidState(config.EventId, secretKey))
            {
                if (this._userInQueueStateRepository.IsStateExtendable(config.EventId)
                    && config.ExtendCookieValidity)
                {
                    this._userInQueueStateRepository.Store(config.EventId,
                        true,
                        config.CookieDomain,
                        config.CookieValidityMinute,
                        secretKey);
                }
                return new RequestValidationResult() { EventId = config.EventId };
            }

            QueueUrlParams queueParmas = QueueParameterHelper.ExtractQueueParams(queueitToken);

            if (queueParmas != null)
            {
                return GetQueueITTokenValidationResult(targetUrl, config.EventId, config, queueParmas,customerId, secretKey);
            }
            else
            {
                return GetInQueueRedirectResult(targetUrl, config, customerId);
            }
        }

        private RequestValidationResult GetQueueITTokenValidationResult(
            string targetUrl,
            string eventId,
            EventConfig config,
            QueueUrlParams queueParams,
            string customerId,
            string secretKey)
        {
            string calculatedHash = HashHelper.GenerateSHA256Hash(secretKey, queueParams.QueueITTokenWithoutHash);
            if (calculatedHash != queueParams.HashCode)
                return GetVaidationErrorResult(customerId, targetUrl, config, queueParams, "hash");

            if (queueParams.EventId != eventId)
                return GetVaidationErrorResult(customerId, targetUrl, config, queueParams, "eventid");

            if (queueParams.TimeStamp < DateTime.UtcNow)
                return GetVaidationErrorResult(customerId, targetUrl, config, queueParams, "timestamp");

            this._userInQueueStateRepository.Store(
                config.EventId,
                queueParams.ExtendableCookie,
                config.CookieDomain,
                queueParams.CookieValidityMinute ?? config.CookieValidityMinute,
                secretKey);

            return new RequestValidationResult() { EventId = config.EventId };
        }



        private RequestValidationResult GetVaidationErrorResult(
            string customerId,
             string targetUrl,
             EventConfig config,
             QueueUrlParams qParams,
             string errorCode)
        {

            var query = GetQueryString(customerId, config) +
                $"&queueittoken={qParams.QueueITToken}" +
                $"&ts={DateTimeHelper.GetUnixTimeStampFromDate(DateTime.UtcNow)}" +
                $"&t={HttpUtility.UrlEncode(targetUrl)}";
            var domainAlias = config.QueueDomain;
            if (!domainAlias.EndsWith("/"))
                domainAlias = domainAlias + "/";
            var redirectUrl = "https://" + domainAlias + $"error/{errorCode}?" + query;
            return new RequestValidationResult()
            {
                RedirectUrl = redirectUrl,
                EventId = config.EventId
            };
        }

        private RequestValidationResult GetInQueueRedirectResult(
            string targetUrl,
            EventConfig config,
            string customerId)
        {

            var redirectUrl = "https://" + config.QueueDomain + "?" +
                GetQueryString(customerId, config) +
                     $"&t={HttpUtility.UrlEncode(targetUrl)}";
            return new RequestValidationResult()
            {
                RedirectUrl = redirectUrl,
                EventId = config.EventId
            };
        }



        string GetQueryString(
            string customerId,
          
            EventConfig config)
        {
            List<string> queryStringList = new List<string>();
            queryStringList.Add($"c={HttpUtility.UrlEncode(customerId)}");
            queryStringList.Add($"e={HttpUtility.UrlEncode(config.EventId)}");
            queryStringList.Add($"ver=v3-{this.GetType().Assembly.GetName().Version.ToString()}");
            queryStringList.Add($"cver={config.Version.ToString()}");

            if (!string.IsNullOrEmpty(config.Culture))
                queryStringList.Add(string.Concat("cid=", HttpUtility.UrlEncode(config.Culture)));

            if (!string.IsNullOrEmpty(config.LayoutName))
                queryStringList.Add(string.Concat("l=", HttpUtility.UrlEncode(config.LayoutName)));


            return string.Join("&", queryStringList);
        }
        public void CancelQueueCookie(string eventId)
        {
            this._userInQueueStateRepository.CancelQueueCookie(eventId);
        }
        public void ExtendQueueCookie(
            string eventId,
            int cookieValidityMinute,
            string secretKey)
        {
            this._userInQueueStateRepository.ExtendQueueCookie(eventId, cookieValidityMinute, secretKey);
        }
    }
}
