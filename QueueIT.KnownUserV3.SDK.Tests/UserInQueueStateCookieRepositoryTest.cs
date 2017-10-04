using Rhino.Mocks;
using System;
using System.Web;
using Xunit;

namespace QueueIT.KnownUserV3.SDK.Tests
{
    public class UserInQueueStateCookieRepositoryTest
    {
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

            testObject.Store(eventId, queueId,true, cookieDomain, cookieValidity, secretKey);
            Assert.True(cookies[cookieKey].Expires.Subtract(DateTime.UtcNow.AddDays(1)) < TimeSpan.FromMinutes(1));

            Assert.True(
                DateTimeHelper.GetUnixTimeStampAsDate(CookieHelper.ToNameValueCollectionFromValue(cookies[cookieKey].Value)["Expires"])
                .Subtract(DateTime.UtcNow.AddMinutes(10)) < TimeSpan.FromSeconds(10));

            Assert.True(cookies[cookieKey].HttpOnly);
            Assert.True(cookies[cookieKey].Domain == cookieDomain);

            //retrive
            var fakeRequest = MockRepository.GenerateMock<HttpRequestBase>();
            fakeContext.Stub(stub => stub.Request).Return(fakeRequest);
            fakeRequest.Stub(stub => stub.Cookies).Return(cookies);
            var state =  testObject.GetState(eventId, secretKey);
            Assert.True(state.IsValid);
            Assert.True(state.IsStateExtendable);
            Assert.True(state.QueueId == queueId);
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

            testObject.Store(eventId, queueId,false, cookieDomain, cookieValidity, secretKey);
            Assert.True(cookies[cookieKey].Expires.Subtract(DateTime.UtcNow.AddDays(1)) < TimeSpan.FromMinutes(1));
            Assert.True(cookies[cookieKey].HttpOnly);
            Assert.True(cookies[cookieKey].Domain == cookieDomain);

            //retrive
            var fakeRequest = MockRepository.GenerateMock<HttpRequestBase>();
            fakeContext.Stub(stub => stub.Request).Return(fakeRequest);
            fakeRequest.Stub(stub => stub.Cookies).Return(cookies);

            var state = testObject.GetState(eventId, secretKey);
            Assert.True(state.IsValid);
            Assert.False(state.IsStateExtendable);
            Assert.True(state.QueueId == queueId);
        }

        [Fact]
        public void Store_HasValidState_TamperedCookie_StateIsNotValid()
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
             testObject.Store(eventId,queueId, false, cookieDomain, cookieValidity, secretKey);
            Assert.True(cookies[cookieKey].Expires.Subtract(DateTime.UtcNow.AddDays(1)) < TimeSpan.FromMinutes(1));
            Assert.True(cookies[cookieKey].HttpOnly);
            Assert.True(cookies[cookieKey].Domain == cookieDomain);

            //Retrive
            var fakeRequest = MockRepository.GenerateMock<HttpRequestBase>();
            fakeContext.Stub(stub => stub.Request).Return(fakeRequest);
            fakeRequest.Stub(stub => stub.Cookies).Return(cookies);

            cookies[cookieKey].Values["IsCookieExtendable"] = "true";

            var state = testObject.GetState(eventId, secretKey);
            Assert.False(state.IsValid);
            Assert.False(state.IsStateExtendable);
            Assert.True(String.IsNullOrEmpty(state.QueueId));
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

            testObject.Store(eventId,queueId, true, cookieDomain, cookieValidity, secretKey);
            Assert.True(cookies[cookieKey].Expires.Subtract(DateTime.UtcNow.AddDays(1)) < TimeSpan.FromMinutes(1));
            Assert.True(cookies[cookieKey].HttpOnly);
            Assert.True(cookies[cookieKey].Domain == cookieDomain);

            //retrive
            var fakeRequest = MockRepository.GenerateMock<HttpRequestBase>();
            fakeContext.Stub(stub => stub.Request).Return(fakeRequest);
            fakeRequest.Stub(stub => stub.Cookies).Return(cookies);

            var state = testObject.GetState(eventId, secretKey);
            Assert.False(state.IsValid);
            Assert.False(state.IsStateExtendable);
            Assert.True(String.IsNullOrEmpty(state.QueueId));
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

            testObject.Store(eventId,queueId, true, cookieDomain, cookieValidity, secretKey);
            Assert.True(cookies[cookieKey].Expires.Subtract(DateTime.UtcNow.AddDays(1)) < TimeSpan.FromMinutes(1));
            Assert.True(cookies[cookieKey].HttpOnly);
            Assert.True(cookies[cookieKey].Domain == cookieDomain);

            //Retrive
            var fakeRequest = MockRepository.GenerateMock<HttpRequestBase>();
            fakeContext.Stub(stub => stub.Request).Return(fakeRequest);
            fakeRequest.Stub(stub => stub.Cookies).Return(cookies);
            var state = testObject.GetState("event2", secretKey);
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

            var state = testObject.GetState(eventId, secretKey);
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
                new HttpCookie(cookieKey) { Value = "Expires=odoododod&IsCookieExtendable=yes&jj=101" },
                new HttpCookie("a") { Value = "test" },
                new HttpCookie("b") { Value = "test" }
            };
            fakeResponse.Stub(stub => stub.Cookies).Return(cookies);

            var testObject = new UserInQueueStateCookieRepository(fakeContext);
            
            var fakeRequest = MockRepository.GenerateMock<HttpRequestBase>();
            fakeContext.Stub(stub => stub.Request).Return(fakeRequest);
            fakeRequest.Stub(stub => stub.Cookies).Return(cookies);

            var state = testObject.GetState(eventId, secretKey);
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
            //creating valid cookie
            var eventId = "event1";
            var secretKey = "secretKey";
            var cookieDomain = ".test.com";
            var queueId = "f8757c2d-34c2-4639-bef2-1736cdd30bbb";
            var cookieKey = UserInQueueStateCookieRepository.GetCookieKey(eventId);
            var cookieValidity = 3;
            var fakeContext = MockRepository.GenerateMock<HttpContextBase>();
            var fakeResponse = MockRepository.GenerateMock<HttpResponseBase>();
            fakeContext.Stub(stub => stub.Response).Return(fakeResponse);
            var cookies = new HttpCookieCollection()
            {
            };
            fakeResponse.Stub(stub => stub.Cookies).Return(cookies);
            var testObject = new UserInQueueStateCookieRepository(fakeContext);

            testObject.Store(eventId,queueId, true, cookieDomain, cookieValidity, secretKey);
            //extend
            var fakeRequest = MockRepository.GenerateMock<HttpRequestBase>();
            fakeContext.Stub(stub => stub.Request).Return(fakeRequest);
            fakeRequest.Stub(stub => stub.Cookies).Return(cookies);

            testObject.ExtendQueueCookie(eventId, 12, secretKey);

            Assert.True(
                DateTimeHelper.GetUnixTimeStampAsDate(CookieHelper.ToNameValueCollectionFromValue(cookies[cookieKey].Value)["Expires"])
                .Subtract(DateTime.UtcNow.AddMinutes(12)) < TimeSpan.FromSeconds(10));
            Assert.True(cookies[cookieKey].Domain == cookieDomain);

            var state = testObject.GetState(eventId, secretKey);
            Assert.True(state.IsValid);
            Assert.True(state.IsStateExtendable);
            Assert.True(state.QueueId==queueId);
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
            testObject.ExtendQueueCookie(eventId, 12, secretKey);


            var state = testObject.GetState(eventId, secretKey);
            Assert.False(state.IsValid);
            Assert.False(state.IsStateExtendable);
            Assert.True(String.IsNullOrEmpty(state.QueueId));
        }
    }
}