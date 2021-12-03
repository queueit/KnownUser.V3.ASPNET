﻿using System;
using System.Collections.Specialized;
using Xunit;

namespace QueueIT.KnownUserV3.SDK.Tests
{
    public class UserInQueueStateCookieRepositoryTest
    {
        private const string _FixedCookieValidityMinutesKey = "FixedValidityMins";

        [Fact]
        public void Store_GetState_ExtendableCookie_CookieIsSaved()
        {
            var eventId = "event1";
            var secretKey = "4e1db821-a825-49da-acd0-5d376f2068db";
            var cookieDomain = ".test.com";
            var isCookieHttpOnly = true;
            var isCookieSecure = true;
            var queueId = "f8757c2d-34c2-4639-bef2-1736cdd30bbb";
            var cookieKey = UserInQueueStateCookieRepository.GetCookieKey(eventId);
            var cookieValidity = 10;

            KnownUserTest.HttpContextMock fakeContext = new KnownUserTest.HttpContextMock();
            var fakeResponse = new KnownUserTest.MockHttpResponse();
            fakeContext.HttpResponse = fakeResponse;

            var testObject = new UserInQueueStateCookieRepository(fakeContext);
            testObject.Store(eventId, queueId, null, cookieDomain, isCookieHttpOnly, isCookieSecure, "Queue", secretKey);

            var cookieValues = CookieHelper.ToNameValueCollectionFromValue(fakeResponse.CookiesValue[cookieKey]["cookieValue"].ToString());

            Assert.True(((DateTime)fakeResponse.CookiesValue[cookieKey]["expiration"]).Subtract(DateTime.UtcNow.AddDays(1)) < TimeSpan.FromMinutes(1));
            Assert.True(
                DateTimeHelper.GetDateTimeFromUnixTimeStamp(cookieValues["IssueTime"])
                .Subtract(DateTime.UtcNow) < TimeSpan.FromSeconds(10));

            Assert.Equal(cookieDomain, fakeResponse.CookiesValue[cookieKey]["domain"].ToString());
            Assert.Equal(isCookieHttpOnly, fakeResponse.CookiesValue[cookieKey]["isHttpOnly"] as bool?);
            Assert.Equal(isCookieSecure, fakeResponse.CookiesValue[cookieKey]["isSecure"] as bool?);

            Assert.Equal(eventId, cookieValues["EventId"]);
            Assert.Equal("queue", cookieValues["RedirectType"]);
            Assert.Equal(queueId, cookieValues["QueueId"]);
            Assert.True(string.IsNullOrEmpty(cookieValues[_FixedCookieValidityMinutesKey]));

            //retrive
            var fakeRequest = new KnownUserTest.MockHttpRequest
            {
                CookiesValue = new NameValueCollection
                {
                    { "a1","b1" },
                    { cookieKey,fakeResponse.CookiesValue[cookieKey]["cookieValue"].ToString() }
                }
            };
            fakeContext.HttpRequest = fakeRequest;
            var state = testObject.GetState(eventId, cookieValidity, secretKey);
            Assert.True(state.IsValid);
            Assert.True(state.IsStateExtendable);
            Assert.True(state.QueueId == queueId);
            Assert.True(state.RedirectType == "queue");
        }

        [Fact]
        public void Store_GetState_NonExtendableCookie_CookieIsSaved()
        {
            var eventId = "event1";
            var secretKey = "secretKey";
            var cookieDomain = ".test.com";
            var isCookieHttpOnly = true;
            var isCookieSecure = true;
            var queueId = "f8757c2d-34c2-4639-bef2-1736cdd30bbb";
            var cookieKey = UserInQueueStateCookieRepository.GetCookieKey(eventId);

            var cookieValidity = 3;
            KnownUserTest.HttpContextMock fakeContext = new KnownUserTest.HttpContextMock();
            var fakeResponse = new KnownUserTest.MockHttpResponse
            {
            };
            fakeContext.HttpResponse = fakeResponse;

            var testObject = new UserInQueueStateCookieRepository(fakeContext);

            testObject.Store(eventId, queueId, cookieValidity, cookieDomain, isCookieHttpOnly, isCookieSecure, "idle", secretKey);
            var cookieValue = fakeResponse.CookiesValue[cookieKey]["cookieValue"].ToString();
            var cookieValues = CookieHelper.ToNameValueCollectionFromValue(cookieValue);

            Assert.Equal("3", cookieValues[_FixedCookieValidityMinutesKey]);
            Assert.True(((DateTime)fakeResponse.CookiesValue[cookieKey]["expiration"]).Subtract(DateTime.UtcNow.AddDays(1)) < TimeSpan.FromMinutes(1));
            Assert.Equal(cookieDomain, fakeResponse.CookiesValue[cookieKey]["domain"].ToString());
            Assert.Equal(isCookieHttpOnly, fakeResponse.CookiesValue[cookieKey]["isHttpOnly"] as bool?);
            Assert.Equal(isCookieSecure, fakeResponse.CookiesValue[cookieKey]["isSecure"] as bool?);
            Assert.True(
                DateTimeHelper.GetDateTimeFromUnixTimeStamp(cookieValues["IssueTime"])
                .Subtract(DateTime.UtcNow) < TimeSpan.FromSeconds(10));


            //retrive
            var fakeRequest = new KnownUserTest.MockHttpRequest
            {
                CookiesValue = new NameValueCollection
                {
                    {"a1", "b1"},
                    {cookieKey, cookieValue}
                }
            };
            fakeContext.HttpRequest = fakeRequest;

            var state = testObject.GetState(eventId, cookieValidity, secretKey);
            Assert.True(state.IsValid);
            Assert.False(state.IsStateExtendable);
            Assert.True(state.QueueId == queueId);
            Assert.True(state.RedirectType == "idle");
            Assert.True(state.FixedCookieValidityMinutes == 3);
        }

        [Fact]
        public void Store_GetState_TamperedCookie_StateIsNotValid_IsCookieExtendable()
        {
            var eventId = "event1";
            var secretKey = "4e1db821-a825-49da-acd0-5d376f2068db";
            var cookieDomain = ".test.com";
            var isCookieHttpOnly = true;
            var isCookieSecure = true;
            var queueId = "f8757c2d-34c2-4639-bef2-1736cdd30bbb";
            var cookieValidity = 10;
            var cookieKey = UserInQueueStateCookieRepository.GetCookieKey(eventId);

            KnownUserTest.HttpContextMock fakeContext = new KnownUserTest.HttpContextMock();
            var fakeResponse = new KnownUserTest.MockHttpResponse
            {
            };
            fakeContext.HttpResponse = fakeResponse;

            var testObject = new UserInQueueStateCookieRepository(fakeContext);
            testObject.Store(eventId, queueId, 3, cookieDomain, isCookieHttpOnly, isCookieSecure, "idle", secretKey);
            var cookieValue = fakeResponse.CookiesValue[cookieKey]["cookieValue"].ToString();
            var cookieValues = CookieHelper.ToNameValueCollectionFromValue(cookieValue);
            Assert.True(((DateTime)fakeResponse.CookiesValue[cookieKey]["expiration"]).Subtract(DateTime.UtcNow.AddDays(1)) < TimeSpan.FromMinutes(1));
            Assert.True(fakeResponse.CookiesValue[cookieKey]["domain"].ToString() == cookieDomain);

            //Retrive
            var tamperedCookie = cookieValue.Replace("3", "10");
            var fakeRequest = new KnownUserTest.MockHttpRequest
            {
                CookiesValue = new NameValueCollection { { cookieKey, tamperedCookie } }
            };
            fakeContext.HttpRequest = fakeRequest;

            var state = testObject.GetState(eventId, cookieValidity, secretKey);
            Assert.False(state.IsValid);
            Assert.False(state.IsStateExtendable);
            Assert.True(string.IsNullOrEmpty(state.QueueId));
            Assert.True(string.IsNullOrEmpty(state.RedirectType));
        }

        [Fact]
        public void Store_GetState_TamperedCookie_StateIsNotValid_EventId()
        {
            var eventId = "event1";
            var secretKey = "4e1db821-a825-49da-acd0-5d376f2068db";
            var cookieDomain = ".test.com";
            var isCookieHttpOnly = true;
            var isCookieSecure = true;
            var queueId = "f8757c2d-34c2-4639-bef2-1736cdd30bbb";
            var cookieValidity = 10;
            var cookieKey = UserInQueueStateCookieRepository.GetCookieKey(eventId);

            KnownUserTest.HttpContextMock fakeContext = new KnownUserTest.HttpContextMock();
            var fakeResponse = new KnownUserTest.MockHttpResponse
            {
            };
            fakeContext.HttpResponse = fakeResponse;

            var testObject = new UserInQueueStateCookieRepository(fakeContext);
            testObject.Store(eventId, queueId, 3, cookieDomain, isCookieHttpOnly, isCookieSecure, "idle", secretKey);
            var cookieValue = fakeResponse.CookiesValue[cookieKey]["cookieValue"].ToString();
            var cookieValues = CookieHelper.ToNameValueCollectionFromValue(cookieValue);
            Assert.True(((DateTime)fakeResponse.CookiesValue[cookieKey]["expiration"]).Subtract(DateTime.UtcNow.AddDays(1)) < TimeSpan.FromMinutes(1));
            Assert.True(fakeResponse.CookiesValue[cookieKey]["domain"].ToString() == cookieDomain);

            //Retrive

            var tamperedCookie = cookieValue.Replace("EventId", "EventId2");
            var fakeRequest = new KnownUserTest.MockHttpRequest
            {
                CookiesValue = new NameValueCollection { { cookieKey, tamperedCookie } }
            };
            fakeContext.HttpRequest = fakeRequest;

            var state = testObject.GetState(eventId, cookieValidity, secretKey);
            Assert.False(state.IsValid);
            Assert.False(state.IsStateExtendable);
            Assert.True(string.IsNullOrEmpty(state.QueueId));
            Assert.True(string.IsNullOrEmpty(state.RedirectType));
        }

        [Fact]
        public void Store_GetState_ExpiredCookie_StateIsNotValid()
        {
            var eventId = "event1";
            var secretKey = "secretKey";
            var cookieDomain = ".test.com";
            var isCookieHttpOnly = true;
            var isCookieSecure = true;
            var queueId = "f8757c2d-34c2-4639-bef2-1736cdd30bbb";
            var cookieKey = UserInQueueStateCookieRepository.GetCookieKey(eventId);

            var cookieValidity = -1;

            KnownUserTest.HttpContextMock fakeContext = new KnownUserTest.HttpContextMock();
            var fakeResponse = new KnownUserTest.MockHttpResponse
            {
            };
            fakeContext.HttpResponse = fakeResponse;

            var testObject = new UserInQueueStateCookieRepository(fakeContext);
            testObject.Store(eventId, queueId, null, cookieDomain, isCookieHttpOnly, isCookieSecure, "idle", secretKey);
            var cookieValue = fakeResponse.CookiesValue[cookieKey]["cookieValue"].ToString();
            var cookieValues = CookieHelper.ToNameValueCollectionFromValue(cookieValue);
            Assert.True(((DateTime)fakeResponse.CookiesValue[cookieKey]["expiration"]).Subtract(DateTime.UtcNow.AddDays(1)) < TimeSpan.FromMinutes(1));
            Assert.True(fakeResponse.CookiesValue[cookieKey]["domain"].ToString() == cookieDomain);

            //retrive
            var fakeRequest = new KnownUserTest.MockHttpRequest
            {
                CookiesValue = new NameValueCollection { { cookieKey, cookieValue } }
            };
            fakeContext.HttpRequest = fakeRequest;

            var state = testObject.GetState(eventId, cookieValidity, secretKey);
            Assert.False(state.IsValid);
            Assert.False(state.IsStateExtendable);
            Assert.True(string.IsNullOrEmpty(state.QueueId));
            Assert.True(string.IsNullOrEmpty(state.RedirectType));
        }

        [Fact]
        public void Store_GetState_DifferentEventId_StateIsNotValid()
        {
            var eventId = "event1";
            var secretKey = "secretKey";
            var cookieDomain = ".test.com";
            var isCookieHttpOnly = true;
            var isCookieSecure = true;
            var queueId = "f8757c2d-34c2-4639-bef2-1736cdd30bbb";
            var cookieKey = UserInQueueStateCookieRepository.GetCookieKey(eventId);

            var cookieValidity = 10;
            KnownUserTest.HttpContextMock fakeContext = new KnownUserTest.HttpContextMock();
            var fakeResponse = new KnownUserTest.MockHttpResponse
            {
            };
            fakeContext.HttpResponse = fakeResponse;

            var testObject = new UserInQueueStateCookieRepository(fakeContext);

            testObject.Store(eventId, queueId, null, cookieDomain, isCookieHttpOnly, isCookieSecure, "queue", secretKey);
            var cookieValue = fakeResponse.CookiesValue[cookieKey]["cookieValue"].ToString();
            Assert.True(((DateTime)fakeResponse.CookiesValue[cookieKey]["expiration"]).Subtract(DateTime.UtcNow.AddDays(1)) < TimeSpan.FromMinutes(1));
            Assert.True(fakeResponse.CookiesValue[cookieKey]["domain"].ToString() == cookieDomain);


            //Retrive
            var fakeRequest = new KnownUserTest.MockHttpRequest
            {
                CookiesValue = new NameValueCollection { { cookieKey, cookieValue } }
            };
            fakeContext.HttpRequest = fakeRequest;
            var state = testObject.GetState("event2", cookieValidity, secretKey);
            Assert.False(state.IsValid);
            Assert.False(state.IsStateExtendable);
            Assert.True(string.IsNullOrEmpty(state.QueueId));
        }

        [Fact]
        public void GetState_NoCookie_StateIsNotValid()
        {
            var eventId = "event1";
            var secretKey = "4e1db821-a825-49da-acd0-5d376f2068db";

            var cookieKey = UserInQueueStateCookieRepository.GetCookieKey(eventId);
            KnownUserTest.HttpContextMock fakeContext = new KnownUserTest.HttpContextMock();
            var fakeResponse = new KnownUserTest.MockHttpResponse
            {
            };
            fakeContext.HttpResponse = fakeResponse;
            var fakeRequest = new KnownUserTest.MockHttpRequest
            {
            };
            fakeContext.HttpRequest = fakeRequest;
            var testObject = new UserInQueueStateCookieRepository(fakeContext);

            var state = testObject.GetState(eventId, 10, secretKey);

            Assert.False(state.IsValid);
            Assert.False(state.IsStateExtendable);
            Assert.True(string.IsNullOrEmpty(state.QueueId));
        }

        [Fact]
        public void GetState_InvalidCookie_StateIsNotValid()
        {
            var eventId = "event1";
            var secretKey = "4e1db821-a825-49da-acd0-5d376f2068db";

            var cookieKey = UserInQueueStateCookieRepository.GetCookieKey(eventId);
            KnownUserTest.HttpContextMock fakeContext = new KnownUserTest.HttpContextMock();
            var fakeRequest = new KnownUserTest.MockHttpRequest
            {
                CookiesValue = new NameValueCollection { { cookieKey, "Expires=odoododod&FixedCookieValidity=yes&jj=101" } }
            };
            fakeContext.HttpRequest = fakeRequest;
            var testObject = new UserInQueueStateCookieRepository(fakeContext);

            var state = testObject.GetState(eventId, 10, secretKey);

            Assert.False(state.IsValid);
            Assert.False(state.IsStateExtendable);
            Assert.True(string.IsNullOrEmpty(state.QueueId));
        }

        [Fact]
        public void CancelQueueCookie_Test()
        {
            var eventId = "event1";
            var cookieDomain = "testDomain";

            var cookieKey = UserInQueueStateCookieRepository.GetCookieKey(eventId);
            KnownUserTest.HttpContextMock fakeContext = new KnownUserTest.HttpContextMock();
            var fakeResponse = new KnownUserTest.MockHttpResponse
            {
            };
            fakeContext.HttpResponse = fakeResponse;
            var testObject = new UserInQueueStateCookieRepository(fakeContext);

            testObject.CancelQueueCookie(eventId, cookieDomain, false, false);
            Assert.True(((DateTime)fakeResponse.CookiesValue[cookieKey]["expiration"]).Subtract(DateTime.UtcNow.AddDays(-1)) < TimeSpan.FromMinutes(1));
            Assert.True(fakeResponse.CookiesValue[cookieKey]["domain"].ToString() == cookieDomain);
        }

        [Fact]
        public void ExtendQueueCookie_CookieExist_Test()
        {
            var eventId = "event1";
            var secretKey = "4e1db821-a825-49da-acd0-5d376f2068db";

            var queueId = "f8757c2d-34c2-4639-bef2-1736cdd30bbb";
            var cookieKey = UserInQueueStateCookieRepository.GetCookieKey(eventId);
            var issueTime = DateTimeHelper.GetUnixTimeStampFromDate(DateTime.UtcNow.AddMinutes(-1));
            var hash = QueueITTokenGenerator.GetSHA256Hash(eventId.ToLower() + queueId + "3" + "idle" + issueTime, secretKey);
            var cookieValue = $"EventId={eventId}&QueueId={queueId}&{_FixedCookieValidityMinutesKey}=3&RedirectType=idle&IssueTime={issueTime}&Hash={hash}";

            var isCookieHttpOnly = true;
            var isCookieSecure = true;

            KnownUserTest.HttpContextMock fakeContext = new KnownUserTest.HttpContextMock();
            var fakeRequest = new KnownUserTest.MockHttpRequest
            {
                CookiesValue = new NameValueCollection { { cookieKey, cookieValue } }
            };
            fakeContext.HttpRequest = fakeRequest;

            var fakeResponse = new KnownUserTest.MockHttpResponse();
            fakeContext.HttpResponse = fakeResponse;

            var testObject = new UserInQueueStateCookieRepository(fakeContext);
            testObject.ReissueQueueCookie(eventId, 12, "testdomain", isCookieHttpOnly, isCookieSecure, secretKey);

            var newIssueTime = DateTimeHelper.GetDateTimeFromUnixTimeStamp(CookieHelper.ToNameValueCollectionFromValue(fakeResponse.CookiesValue[cookieKey]["cookieValue"].ToString())["IssueTime"]);
            Assert.True(newIssueTime.Subtract(DateTime.UtcNow) < TimeSpan.FromSeconds(2));
            Assert.True(fakeResponse.CookiesValue[cookieKey]["domain"].ToString() == "testdomain");
            Assert.Equal(isCookieHttpOnly, fakeResponse.CookiesValue[cookieKey]["isHttpOnly"] as bool?);
            Assert.Equal(isCookieSecure, fakeResponse.CookiesValue[cookieKey]["isSecure"] as bool?);

            var state = testObject.GetState(eventId, 5, secretKey);
            Assert.True(state.IsValid);
            Assert.True(!state.IsStateExtendable);
            Assert.True(state.QueueId == queueId);
            Assert.True(state.RedirectType == "idle");
        }

        [Fact]
        public void ExtendQueueCookie_CookieDoesNotExist_Test()
        {
            var eventId = "event1";
            var secretKey = "secretKey";
            var cookieKey = UserInQueueStateCookieRepository.GetCookieKey(eventId);
            KnownUserTest.HttpContextMock fakeContext = new KnownUserTest.HttpContextMock();
            var fakeRequest = new KnownUserTest.MockHttpRequest
            {
            };
            fakeContext.HttpRequest = fakeRequest;
            var fakeResponse = new KnownUserTest.MockHttpResponse
            {
            };
            fakeContext.HttpResponse = fakeResponse;

            var testObject = new UserInQueueStateCookieRepository(fakeContext);
            testObject.ReissueQueueCookie(eventId, 12, "testdomain", false, false, secretKey);

            var state = testObject.GetState(eventId, 12, secretKey);
            Assert.False(state.IsValid);
            Assert.False(state.IsStateExtendable);
            Assert.True(string.IsNullOrEmpty(state.QueueId));
        }

        [Fact]
        public void GetState_ValidCookieFormat_Extendable()
        {
            var eventId = "event1";
            var secretKey = "4e1db821-a825-49da-acd0-5d376f2068db";

            var queueId = "f8757c2d-34c2-4639-bef2-1736cdd30bbb";
            var cookieKey = UserInQueueStateCookieRepository.GetCookieKey(eventId);




            var issueTime = DateTimeHelper.GetUnixTimeStampFromDate(DateTime.UtcNow);
            var hash = QueueITTokenGenerator.GetSHA256Hash(eventId.ToLower() + queueId + "queue" + issueTime.ToString(),
                secretKey);
            var cookieValue = $"EventId={eventId}&QueueId={queueId}&RedirectType=queue&IssueTime={issueTime}&Hash={hash}";
            KnownUserTest.HttpContextMock fakeContext = new KnownUserTest.HttpContextMock();
            var fakeRequest = new KnownUserTest.MockHttpRequest
            {
                CookiesValue = new NameValueCollection { { cookieKey, cookieValue } }
            };
            fakeContext.HttpRequest = fakeRequest;
            var fakeResponse = new KnownUserTest.MockHttpResponse
            {
            };
            fakeContext.HttpResponse = fakeResponse;


            var testObject = new UserInQueueStateCookieRepository(fakeContext);

            var state = testObject.GetState(eventId, 10, secretKey);

            Assert.True(state.IsStateExtendable);
            Assert.True(state.IsValid);
            Assert.True(state.IsFound);
            Assert.True(state.QueueId == queueId);
            Assert.True(state.RedirectType == "queue");
        }

        [Fact]
        public void GetState_OldCookie_InValid_ExpiredCookie_Extendable()
        {
            var eventId = "event1";
            var secretKey = "4e1db821-a825-49da-acd0-5d376f2068db";

            var queueId = "f8757c2d-34c2-4639-bef2-1736cdd30bbb";
            var cookieKey = UserInQueueStateCookieRepository.GetCookieKey(eventId);

            var issueTime = DateTimeHelper.GetUnixTimeStampFromDate(DateTime.UtcNow.AddMinutes(-11));
            var hash = QueueITTokenGenerator.GetSHA256Hash(eventId.ToLower() + queueId + "queue" + issueTime.ToString(),
                secretKey);
            var cookieValue = Uri.EscapeDataString($"EventId={eventId}&QueueId={queueId}&RedirectType=queue&IssueTime={issueTime}&Hash={hash}");

            KnownUserTest.HttpContextMock fakeContext = new KnownUserTest.HttpContextMock();
            var fakeRequest = new KnownUserTest.MockHttpRequest
            {
                CookiesValue = new NameValueCollection { { cookieKey, cookieValue } }
            };
            fakeContext.HttpRequest = fakeRequest;
            var fakeResponse = new KnownUserTest.MockHttpResponse
            {
            };
            fakeContext.HttpResponse = fakeResponse;

            var testObject = new UserInQueueStateCookieRepository(fakeContext);

            var state = testObject.GetState(eventId, 10, secretKey);

            Assert.True(!state.IsValid);
            Assert.True(state.IsFound);
        }

        [Fact]
        public void GetState_OldCookie_InValid_ExpiredCookie_NonExtendable()
        {
            var eventId = "event1";
            var secretKey = "4e1db821-a825-49da-acd0-5d376f2068db";

            var queueId = "f8757c2d-34c2-4639-bef2-1736cdd30bbb";
            var cookieKey = UserInQueueStateCookieRepository.GetCookieKey(eventId);

            var issueTime = DateTimeHelper.GetUnixTimeStampFromDate(DateTime.UtcNow.AddMinutes(-4));
            var hash = QueueITTokenGenerator.GetSHA256Hash(eventId.ToLower() + queueId + "3" + "idle" + issueTime.ToString(),
                secretKey);
            var cookieValue = Uri.EscapeDataString($"EventId={eventId}&QueueId={queueId}&{_FixedCookieValidityMinutesKey}=3&RedirectType=idle&IssueTime={issueTime}&Hash={hash}");

            KnownUserTest.HttpContextMock fakeContext = new KnownUserTest.HttpContextMock();
            var fakeRequest = new KnownUserTest.MockHttpRequest
            {
                CookiesValue = new NameValueCollection { { cookieKey, cookieValue } }
            };
            fakeContext.HttpRequest = fakeRequest;
            var fakeResponse = new KnownUserTest.MockHttpResponse
            {
            };
            fakeContext.HttpResponse = fakeResponse;

            var testObject = new UserInQueueStateCookieRepository(fakeContext);

            var state = testObject.GetState(eventId, 10, secretKey);

            Assert.True(!state.IsValid);
            Assert.True(state.IsFound);
        }

        [Fact]
        public void GetState_ValidCookieFormat_NonExtendable()
        {
            var eventId = "event1";
            var secretKey = "4e1db821-a825-49da-acd0-5d376f2068db";

            var queueId = "f8757c2d-34c2-4639-bef2-1736cdd30bbb";
            var cookieKey = UserInQueueStateCookieRepository.GetCookieKey(eventId);

            var issueTime = DateTimeHelper.GetUnixTimeStampFromDate(DateTime.UtcNow);
            var hash = QueueITTokenGenerator.GetSHA256Hash(eventId.ToLower() + queueId + "3" + "idle" + issueTime.ToString(),
                secretKey);
            var cookieValue = $"EventId={eventId}&QueueId={queueId}&{_FixedCookieValidityMinutesKey}=3&RedirectType=idle&IssueTime={issueTime}&Hash={hash}";

            KnownUserTest.HttpContextMock fakeContext = new KnownUserTest.HttpContextMock();
            var fakeRequest = new KnownUserTest.MockHttpRequest
            {
                CookiesValue = new NameValueCollection { { cookieKey, cookieValue } }
            };
            fakeContext.HttpRequest = fakeRequest;
            var fakeResponse = new KnownUserTest.MockHttpResponse
            {
            };
            fakeContext.HttpResponse = fakeResponse;

            var testObject = new UserInQueueStateCookieRepository(fakeContext);

            var state = testObject.GetState(eventId, 10, secretKey);

            Assert.False(state.IsStateExtendable);
            Assert.True(state.IsValid);
            Assert.True(state.IsFound);
            Assert.True(state.QueueId == queueId);
            Assert.True(state.RedirectType == "idle");
        }

        [Fact]
        public void GetState_NoCookie()
        {
            KnownUserTest.HttpContextMock fakeContext = new KnownUserTest.HttpContextMock();
            var testObject = new UserInQueueStateCookieRepository(fakeContext);

            var eventId = "event1";
            var secretKey = "4e1db821-a825-49da-acd0-5d376f2068db";

            var state = testObject.GetState(eventId, 10, secretKey);

            Assert.False(state.IsFound);
            Assert.False(state.IsValid);
        }
    }
}