using Rhino.Mocks;
using System;
using System.Web;
using Xunit;

namespace QueueIT.KnownUserV3.SDK.Tests
{
    public class UserInQueueStateCookieRepositoryTest
    {
        private const string _FixedCookieValidityMinutesKey = "FixedValidityMins";

        [Fact]
        public void Store_HasValidState_ExtendableCookie_CookieIsSaved()
        {
            var eventId = "event1";
            var secretKey = "4e1db821-a825-49da-acd0-5d376f2068db";
            var cookieDomain = ".test.com";
            var queueId = "f8757c2d-34c2-4639-bef2-1736cdd30bbb";
            var cookieKey = UserInQueueStateCookieRepository.GetCookieKey(eventId);
            var cookieValidity = 10;
            var fakeContext = MockRepository.GenerateMock<HttpContextBase>();

            var fakeResponse = MockRepository.GenerateMock<HttpResponseBase>();
            fakeContext.Stub(stub => stub.Response).Return(fakeResponse);
            var cookies = new HttpCookieCollection() {
                new HttpCookie("key1") { Value = "test" },
                new HttpCookie("a") { Value = "test" },
                new HttpCookie("b") { Value = "test" }
            };
            fakeResponse.Stub(stub => stub.Cookies).Return(cookies);

            var testObject = new UserInQueueStateCookieRepository(fakeContext);
            testObject.Store(eventId, queueId, null, cookieDomain, "Queue", secretKey, null);
            var cookieValues = CookieHelper.ToNameValueCollectionFromValue(cookies[cookieKey].Value);
            Assert.True(cookies[cookieKey].Expires.Subtract(DateTime.UtcNow.AddDays(1)) < TimeSpan.FromMinutes(1));
            Assert.True(
                DateTimeHelper.GetDateTimeFromUnixTimeStamp(CookieHelper.ToNameValueCollectionFromValue(cookies[cookieKey].Value)["IssueTime"])
                .Subtract(DateTime.UtcNow) < TimeSpan.FromSeconds(10));
            
            Assert.True(cookies[cookieKey].Domain == cookieDomain);
            Assert.True(cookieValues["EventId"] == eventId);
            Assert.True(cookieValues["RedirectType"] == "queue");
            Assert.True(cookieValues["QueueId"] == queueId);
            Assert.True(string.IsNullOrEmpty(cookies[cookieKey].Values[_FixedCookieValidityMinutesKey]));

            //retrive
            var fakeRequest = MockRepository.GenerateMock<HttpRequestBase>();
            fakeContext.Stub(stub => stub.Request).Return(fakeRequest);
            fakeRequest.Stub(stub => stub.Cookies).Return(cookies);
            var state = testObject.GetState(eventId, cookieValidity, secretKey);
            Assert.True(state.IsValid);
            Assert.True(state.IsStateExtendable);
            Assert.True(state.QueueId == queueId);
            Assert.True(state.RedirectType == "queue");
        }

        [Fact]
        public void Store_HasValidState_NonExtendableCookie_CookieIsSaved()
        {
            var eventId = "event1";
            var secretKey = "secretKey";
            var cookieDomain = ".test.com";
            var queueId = "f8757c2d-34c2-4639-bef2-1736cdd30bbb";
            var cookieKey = UserInQueueStateCookieRepository.GetCookieKey(eventId);

            var cookieValidity = 3;
            var fakeContext = MockRepository.GenerateMock<HttpContextBase>();
            var fakeResponse = MockRepository.GenerateMock<HttpResponseBase>();
            fakeContext.Stub(stub => stub.Response).Return(fakeResponse);
            var cookies = new HttpCookieCollection() {
                new HttpCookie(cookieKey) { Value = "test" },
                new HttpCookie("a") { Value = "test" },
                new HttpCookie("b") { Value = "test" }
            };
            fakeResponse.Stub(stub => stub.Cookies).Return(cookies);

            var testObject = new UserInQueueStateCookieRepository(fakeContext);

            testObject.Store(eventId, queueId, cookieValidity, cookieDomain, "idle", secretKey, null);

            var cookieValues = CookieHelper.ToNameValueCollectionFromValue(cookies[cookieKey].Value);
            Assert.True(cookieValues[_FixedCookieValidityMinutesKey] == "3");
            Assert.True(cookies[cookieKey].Expires.Subtract(DateTime.UtcNow.AddDays(1)) < TimeSpan.FromMinutes(1));
            Assert.True(cookies[cookieKey].Domain == cookieDomain);
            Assert.True(
                    DateTimeHelper.GetDateTimeFromUnixTimeStamp(cookieValues["IssueTime"])
                    .Subtract(DateTime.UtcNow) < TimeSpan.FromSeconds(10));

            //retrive
            var fakeRequest = MockRepository.GenerateMock<HttpRequestBase>();
            fakeContext.Stub(stub => stub.Request).Return(fakeRequest);
            fakeRequest.Stub(stub => stub.Cookies).Return(cookies);

            var state = testObject.GetState(eventId, cookieValidity, secretKey);
            Assert.True(state.IsValid);
            Assert.False(state.IsStateExtendable);
            Assert.True(state.QueueId == queueId);
            Assert.True(state.RedirectType == "idle");
            Assert.True(state.FixedCookieValidityMinutes == 3);
        }

        [Fact]
        public void Store_HasValidState_TamperedCookie_StateIsNotValid_IsCookieExtendable()
        {
            var eventId = "event1";
            var secretKey = "4e1db821-a825-49da-acd0-5d376f2068db";
            var cookieDomain = ".test.com";
            var queueId = "f8757c2d-34c2-4639-bef2-1736cdd30bbb";
            var cookieValidity = 10;
            var fakeContext = MockRepository.GenerateMock<HttpContextBase>();
            var cookieKey = UserInQueueStateCookieRepository.GetCookieKey(eventId);

            var fakeResponse = MockRepository.GenerateMock<HttpResponseBase>();
            fakeContext.Stub(stub => stub.Response).Return(fakeResponse);
            var cookies = new HttpCookieCollection() {
                new HttpCookie(cookieKey) { Value = "test" },
                new HttpCookie("a") { Value = "test" },
                new HttpCookie("b") { Value = "test" }
            };
            fakeResponse.Stub(stub => stub.Cookies).Return(cookies);

            var testObject = new UserInQueueStateCookieRepository(fakeContext);
            testObject.Store(eventId, queueId, 3, cookieDomain, "idle", secretKey, null);
            Assert.True(cookies[cookieKey].Expires.Subtract(DateTime.UtcNow.AddDays(1)) < TimeSpan.FromMinutes(1));
            Assert.True(cookies[cookieKey].Domain == cookieDomain);

            //Retrive
            var fakeRequest = MockRepository.GenerateMock<HttpRequestBase>();
            fakeContext.Stub(stub => stub.Request).Return(fakeRequest);
            fakeRequest.Stub(stub => stub.Cookies).Return(cookies);

            cookies[cookieKey].Values[_FixedCookieValidityMinutesKey] = "10";

            var state = testObject.GetState(eventId, cookieValidity, secretKey);
            Assert.False(state.IsValid);
            Assert.False(state.IsStateExtendable);
            Assert.True(String.IsNullOrEmpty(state.QueueId));
            Assert.True(String.IsNullOrEmpty(state.RedirectType));
        }
        [Fact]
        public void Store_HasValidState_TamperedCookie_StateIsNotValid_EventId()
        {
            var eventId = "event1";
            var secretKey = "4e1db821-a825-49da-acd0-5d376f2068db";
            var cookieDomain = ".test.com";
            var queueId = "f8757c2d-34c2-4639-bef2-1736cdd30bbb";
            var cookieValidity = 10;
            var fakeContext = MockRepository.GenerateMock<HttpContextBase>();
            var cookieKey = UserInQueueStateCookieRepository.GetCookieKey(eventId);

            var fakeResponse = MockRepository.GenerateMock<HttpResponseBase>();
            fakeContext.Stub(stub => stub.Response).Return(fakeResponse);
            var cookies = new HttpCookieCollection() {
                new HttpCookie(cookieKey) { Value = "test" },
                new HttpCookie("a") { Value = "test" },
                new HttpCookie("b") { Value = "test" }
            };
            fakeResponse.Stub(stub => stub.Cookies).Return(cookies);

            var testObject = new UserInQueueStateCookieRepository(fakeContext);
            testObject.Store(eventId, queueId, 3, cookieDomain, "idle", secretKey, null);
            Assert.True(cookies[cookieKey].Expires.Subtract(DateTime.UtcNow.AddDays(1)) < TimeSpan.FromMinutes(1));
            Assert.True(cookies[cookieKey].Domain == cookieDomain);

            //Retrive
            var fakeRequest = MockRepository.GenerateMock<HttpRequestBase>();
            fakeContext.Stub(stub => stub.Request).Return(fakeRequest);
            fakeRequest.Stub(stub => stub.Cookies).Return(cookies);

            cookies[cookieKey].Values["EventId"] = "EventId2";

            var state = testObject.GetState(eventId, cookieValidity, secretKey);
            Assert.False(state.IsValid);
            Assert.False(state.IsStateExtendable);
            Assert.True(String.IsNullOrEmpty(state.QueueId));
            Assert.True(String.IsNullOrEmpty(state.RedirectType));
        }

        [Fact]
        public void Store_HasValidState_ExpiredCookie_StateIsNotValid()
        {
            var eventId = "event1";
            var secretKey = "secretKey";
            var cookieDomain = ".test.com";
            var queueId = "f8757c2d-34c2-4639-bef2-1736cdd30bbb";
            var cookieKey = UserInQueueStateCookieRepository.GetCookieKey(eventId);

            var cookieValidity = -1;
            var fakeContext = MockRepository.GenerateMock<HttpContextBase>();
            var fakeResponse = MockRepository.GenerateMock<HttpResponseBase>();
            fakeContext.Stub(stub => stub.Response).Return(fakeResponse);
            var cookies = new HttpCookieCollection() {
                new HttpCookie(cookieKey) { Value = "test" },
                new HttpCookie("a") { Value = "test" },
                new HttpCookie("b") { Value = "test" }
            };
            fakeResponse.Stub(stub => stub.Cookies).Return(cookies);

            var testObject = new UserInQueueStateCookieRepository(fakeContext);

            testObject.Store(eventId, queueId, null, cookieDomain, "idle", secretKey, null);
            Assert.True(cookies[cookieKey].Expires.Subtract(DateTime.UtcNow.AddDays(1)) < TimeSpan.FromMinutes(1));
            Assert.True(cookies[cookieKey].Domain == cookieDomain);

            //retrive
            var fakeRequest = MockRepository.GenerateMock<HttpRequestBase>();
            fakeContext.Stub(stub => stub.Request).Return(fakeRequest);
            fakeRequest.Stub(stub => stub.Cookies).Return(cookies);

            var state = testObject.GetState(eventId, cookieValidity, secretKey);
            Assert.False(state.IsValid);
            Assert.False(state.IsStateExtendable);
            Assert.True(String.IsNullOrEmpty(state.QueueId));
            Assert.True(String.IsNullOrEmpty(state.RedirectType));
        }

        [Fact]
        public void Store_HasValidState_DifferentEventId_StateIsNotValid()
        {
            var eventId = "event1";
            var secretKey = "secretKey";
            var cookieDomain = ".test.com";
            var queueId = "f8757c2d-34c2-4639-bef2-1736cdd30bbb";
            var cookieKey = UserInQueueStateCookieRepository.GetCookieKey(eventId);

            var cookieValidity = 10;
            var fakeContext = MockRepository.GenerateMock<HttpContextBase>();

            var fakeResponse = MockRepository.GenerateMock<HttpResponseBase>();
            fakeContext.Stub(stub => stub.Response).Return(fakeResponse);
            var cookies = new HttpCookieCollection() {
                new HttpCookie(cookieKey) { Value = "test" },
                new HttpCookie("a") { Value = "test" },
                new HttpCookie("b") { Value = "test" }
            };
            fakeResponse.Stub(stub => stub.Cookies).Return(cookies);

            var testObject = new UserInQueueStateCookieRepository(fakeContext);

            testObject.Store(eventId, queueId, null, cookieDomain, "queue", secretKey, null);
            Assert.True(cookies[cookieKey].Expires.Subtract(DateTime.UtcNow.AddDays(1)) < TimeSpan.FromMinutes(1));
            Assert.True(cookies[cookieKey].Domain == cookieDomain);

            //Retrive
            var fakeRequest = MockRepository.GenerateMock<HttpRequestBase>();
            fakeContext.Stub(stub => stub.Request).Return(fakeRequest);
            fakeRequest.Stub(stub => stub.Cookies).Return(cookies);
            var state = testObject.GetState("event2", cookieValidity, secretKey);
            Assert.False(state.IsValid);
            Assert.False(state.IsStateExtendable);
            Assert.True(String.IsNullOrEmpty(state.QueueId));
        }

        [Fact]
        public void HasValidState_NoCookie_StateIsNotValid()
        {
            var eventId = "event1";
            var secretKey = "4e1db821-a825-49da-acd0-5d376f2068db";

            var cookieKey = UserInQueueStateCookieRepository.GetCookieKey(eventId);
            var fakeContext = MockRepository.GenerateMock<HttpContextBase>();

            var fakeResponse = MockRepository.GenerateMock<HttpResponseBase>();
            fakeContext.Stub(stub => stub.Response).Return(fakeResponse);
            var cookies = new HttpCookieCollection() {
                new HttpCookie("key1") { Value = "test" },
                new HttpCookie("a") { Value = "test" },
                new HttpCookie("b") { Value = "test" }
            };
            fakeResponse.Stub(stub => stub.Cookies).Return(cookies);

            var testObject = new UserInQueueStateCookieRepository(fakeContext);

            var fakeRequest = MockRepository.GenerateMock<HttpRequestBase>();
            fakeContext.Stub(stub => stub.Request).Return(fakeRequest);
            fakeRequest.Stub(stub => stub.Cookies).Return(cookies);

            var state = testObject.GetState(eventId, 10, secretKey);
            Assert.False(state.IsValid);
            Assert.False(state.IsStateExtendable);
            Assert.True(String.IsNullOrEmpty(state.QueueId));
        }

        [Fact]
        public void HasValidState_InvalidCookie_StateIsNotValid()
        {
            var eventId = "event1";
            var secretKey = "4e1db821-a825-49da-acd0-5d376f2068db";

            var cookieKey = UserInQueueStateCookieRepository.GetCookieKey(eventId);
            var fakeContext = MockRepository.GenerateMock<HttpContextBase>();

            var fakeResponse = MockRepository.GenerateMock<HttpResponseBase>();
            fakeContext.Stub(stub => stub.Response).Return(fakeResponse);
            var cookies = new HttpCookieCollection() {
                new HttpCookie(cookieKey) { Value = "Expires=odoododod&FixedCookieValidity=yes&jj=101" },
                new HttpCookie("a") { Value = "test" },
                new HttpCookie("b") { Value = "test" }
            };
            fakeResponse.Stub(stub => stub.Cookies).Return(cookies);

            var testObject = new UserInQueueStateCookieRepository(fakeContext);

            var fakeRequest = MockRepository.GenerateMock<HttpRequestBase>();
            fakeContext.Stub(stub => stub.Request).Return(fakeRequest);
            fakeRequest.Stub(stub => stub.Cookies).Return(cookies);

            var state = testObject.GetState(eventId, 10, secretKey);
            Assert.False(state.IsValid);
            Assert.False(state.IsStateExtendable);
            Assert.True(String.IsNullOrEmpty(state.QueueId));
        }

        [Fact]
        public void CancelQueueCookie_Test()
        {
            var eventId = "event1";
            var cookieDomain = "testDomain";

            var cookieKey = UserInQueueStateCookieRepository.GetCookieKey(eventId);
            var fakeContext = MockRepository.GenerateMock<HttpContextBase>();
            var fakeResponse = MockRepository.GenerateMock<HttpResponseBase>();
            fakeContext.Stub(stub => stub.Response).Return(fakeResponse);
            var cookies = new HttpCookieCollection() {
                new HttpCookie("a") { Value = "test" },
                new HttpCookie("b") { Value = "test" }
            };
            fakeResponse.Stub(stub => stub.Cookies).Return(cookies);

            var testObject = new UserInQueueStateCookieRepository(fakeContext);

            testObject.CancelQueueCookie(eventId, cookieDomain);
            Assert.True(cookies[cookieKey].Expires.Subtract(DateTime.UtcNow.AddDays(-1)) < TimeSpan.FromMinutes(1));
            Assert.True(cookies[cookieKey].Domain == cookieDomain);
        }

        [Fact]
        public void ExtendQueueCookie_CookieExist_Test()
        {
            var eventId = "event1";
            var secretKey = "4e1db821-a825-49da-acd0-5d376f2068db";

            var queueId = "f8757c2d-34c2-4639-bef2-1736cdd30bbb";
            var cookieKey = UserInQueueStateCookieRepository.GetCookieKey(eventId);

            var fakeContext = MockRepository.GenerateMock<HttpContextBase>();

            var issueTime = DateTimeHelper.GetUnixTimeStampFromDate(DateTime.UtcNow.AddMinutes(-1));
            var hash = QueueITTokenGenerator.GetSHA256Hash(eventId.ToLower() + queueId + "3" + "idle" + issueTime.ToString(),
                secretKey);
            var cookieValue = $"EventId={eventId}&QueueId={queueId}&{_FixedCookieValidityMinutesKey}=3&RedirectType=idle&IssueTime={issueTime}&Hash={hash}";

            var fakeRequest = MockRepository.GenerateMock<HttpRequestBase>();
            fakeContext.Stub(stub => stub.Request).Return(fakeRequest);
            var cookies = new HttpCookieCollection() {
                new HttpCookie("key1") { Value = "test" },
                new HttpCookie("a") { Value = "test" },
                new HttpCookie("b") { Value = "test" },
                new HttpCookie(cookieKey){ Value = cookieValue, Domain="testdomain"}
            };
            fakeRequest.Stub(stub => stub.Cookies).Return(cookies);

            var fakeResponse = MockRepository.GenerateMock<HttpResponseBase>();
            fakeContext.Stub(stub => stub.Response).Return(fakeResponse);
            fakeResponse.Stub(stub => stub.Cookies).Return(cookies);

            var testObject = new UserInQueueStateCookieRepository(fakeContext);
            testObject.ReissueQueueCookie(eventId, 12, secretKey);

            var newIssueTime = DateTimeHelper.GetDateTimeFromUnixTimeStamp(CookieHelper.ToNameValueCollectionFromValue(cookies[cookieKey].Value)["IssueTime"]);
            Assert.True(newIssueTime.Subtract(DateTime.UtcNow) < TimeSpan.FromSeconds(2));
            Assert.True(cookies[cookieKey].Domain == "testdomain");

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
            var fakeContext = MockRepository.GenerateMock<HttpContextBase>();
            var fakeResponse = MockRepository.GenerateMock<HttpResponseBase>();
            fakeContext.Stub(stub => stub.Response).Return(fakeResponse);
            var cookies = new HttpCookieCollection()
            {
            };
            fakeResponse.Stub(stub => stub.Cookies).Return(cookies);
            var fakeRequest = MockRepository.GenerateMock<HttpRequestBase>();
            fakeContext.Stub(stub => stub.Request).Return(fakeRequest);
            fakeRequest.Stub(stub => stub.Cookies).Return(cookies);

            var testObject = new UserInQueueStateCookieRepository(fakeContext);
            testObject.ReissueQueueCookie(eventId, 12, secretKey);

            var state = testObject.GetState(eventId, 12, secretKey);
            Assert.False(state.IsValid);
            Assert.False(state.IsStateExtendable);
            Assert.True(String.IsNullOrEmpty(state.QueueId));
        }

        [Fact]
        public void GetState_ValidCookieFormat_Extendable()
        {
            var eventId = "event1";
            var secretKey = "4e1db821-a825-49da-acd0-5d376f2068db";

            var queueId = "f8757c2d-34c2-4639-bef2-1736cdd30bbb";
            var cookieKey = UserInQueueStateCookieRepository.GetCookieKey(eventId);

            var fakeContext = MockRepository.GenerateMock<HttpContextBase>();

            var issueTime = DateTimeHelper.GetUnixTimeStampFromDate(DateTime.UtcNow);
            var hash = QueueITTokenGenerator.GetSHA256Hash(eventId.ToLower() + queueId + "queue" + issueTime.ToString(),
                secretKey);
            var cookieValue = HttpUtility.UrlEncode($"EventId={eventId}&QueueId={queueId}&RedirectType=queue&IssueTime={issueTime}&Hash={hash}");
            var fakeRequest = MockRepository.GenerateMock<HttpRequestBase>();
            fakeContext.Stub(stub => stub.Request).Return(fakeRequest);
            var cookies = new HttpCookieCollection() {
                new HttpCookie("key1") { Value = "test" },
                new HttpCookie("a") { Value = "test" },
                new HttpCookie("b") { Value = "test" },
                new HttpCookie(cookieKey){ Value = cookieValue}
            };
            fakeRequest.Stub(stub => stub.Cookies).Return(cookies);

            var testObject = new UserInQueueStateCookieRepository(fakeContext);

            var state = testObject.GetState(eventId, 10, secretKey);

            Assert.True(state.IsStateExtendable);
            Assert.True(state.IsValid);
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

            var fakeContext = MockRepository.GenerateMock<HttpContextBase>();

            var issueTime = DateTimeHelper.GetUnixTimeStampFromDate(DateTime.UtcNow.AddMinutes(-11));
            var hash = QueueITTokenGenerator.GetSHA256Hash(eventId.ToLower() + queueId + "queue" + issueTime.ToString(),
                secretKey);
            var cookieValue = HttpUtility.UrlEncode($"EventId={eventId}&QueueId={queueId}&RedirectType=queue&IssueTime={issueTime}&Hash={hash}");

            var fakeRequest = MockRepository.GenerateMock<HttpRequestBase>();
            fakeContext.Stub(stub => stub.Request).Return(fakeRequest);
            var cookies = new HttpCookieCollection() {
                new HttpCookie("key1") { Value = "test" },
                new HttpCookie("a") { Value = "test" },
                new HttpCookie("b") { Value = "test" },
                new HttpCookie(cookieKey){ Value = cookieValue}
            };
            fakeRequest.Stub(stub => stub.Cookies).Return(cookies);

            var testObject = new UserInQueueStateCookieRepository(fakeContext);

            var state = testObject.GetState(eventId, 10, secretKey);

            Assert.True(!state.IsValid);
        }

        [Fact]
        public void GetState_OldCookie_InValid_ExpiredCookie_NonExtendable()
        {
            var eventId = "event1";
            var secretKey = "4e1db821-a825-49da-acd0-5d376f2068db";

            var queueId = "f8757c2d-34c2-4639-bef2-1736cdd30bbb";
            var cookieKey = UserInQueueStateCookieRepository.GetCookieKey(eventId);

            var fakeContext = MockRepository.GenerateMock<HttpContextBase>();

            var issueTime = DateTimeHelper.GetUnixTimeStampFromDate(DateTime.UtcNow.AddMinutes(-4));
            var hash = QueueITTokenGenerator.GetSHA256Hash(eventId.ToLower() + queueId + "3" + "idle" + issueTime.ToString(),
                secretKey);
            var cookieValue = HttpUtility.UrlEncode($"EventId={eventId}&QueueId={queueId}&{_FixedCookieValidityMinutesKey}=3&RedirectType=idle&IssueTime={issueTime}&Hash={hash}");

            var fakeRequest = MockRepository.GenerateMock<HttpRequestBase>();
            fakeContext.Stub(stub => stub.Request).Return(fakeRequest);
            var cookies = new HttpCookieCollection() {
                new HttpCookie("key1") { Value = "test" },
                new HttpCookie("a") { Value = "test" },
                new HttpCookie("b") { Value = "test" },
                new HttpCookie(cookieKey){ Value = cookieValue}
            };
            fakeRequest.Stub(stub => stub.Cookies).Return(cookies);

            var testObject = new UserInQueueStateCookieRepository(fakeContext);

            var state = testObject.GetState(eventId, 10, secretKey);

            Assert.True(!state.IsValid);
        }

        [Fact]
        public void GetState_ValidCookieFormat_NonExtendable()
        {
            var eventId = "event1";
            var secretKey = "4e1db821-a825-49da-acd0-5d376f2068db";

            var queueId = "f8757c2d-34c2-4639-bef2-1736cdd30bbb";
            var cookieKey = UserInQueueStateCookieRepository.GetCookieKey(eventId);

            var fakeContext = MockRepository.GenerateMock<HttpContextBase>();

            var issueTime = DateTimeHelper.GetUnixTimeStampFromDate(DateTime.UtcNow);
            var hash = QueueITTokenGenerator.GetSHA256Hash(eventId.ToLower() + queueId + "3" + "idle" + issueTime.ToString(),
                secretKey);
            var cookieValue = HttpUtility.UrlEncode($"EventId={eventId}&QueueId={queueId}&{_FixedCookieValidityMinutesKey}=3&RedirectType=idle&IssueTime={issueTime}&Hash={hash}");

            var fakeRequest = MockRepository.GenerateMock<HttpRequestBase>();
            fakeContext.Stub(stub => stub.Request).Return(fakeRequest);
            var cookies = new HttpCookieCollection() {
                new HttpCookie("key1") { Value = "test" },
                new HttpCookie("a") { Value = "test" },
                new HttpCookie("b") { Value = "test" },
                new HttpCookie(cookieKey){ Value = cookieValue}
            };
            fakeRequest.Stub(stub => stub.Cookies).Return(cookies);

            var testObject = new UserInQueueStateCookieRepository(fakeContext);

            var state = testObject.GetState(eventId, 10, secretKey);

            Assert.False(state.IsStateExtendable);
            Assert.True(state.IsValid);
            Assert.True(state.QueueId == queueId);
            Assert.True(state.RedirectType == "idle");
        }
    }
}