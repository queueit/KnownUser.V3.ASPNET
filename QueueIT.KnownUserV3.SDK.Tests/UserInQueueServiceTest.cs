using QueueIT.KnownUserV3.SDK.IntegrationConfig;
using Rhino.Mocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Xunit;

namespace QueueIT.KnownUserV3.SDK.Tests
{
    public class UserInQueueServiceTest
    {
        #region ExtendableCookie Cookie
        [Fact]
        public void ValidateRequest_ValidState_ExtendableCookie_NoCookieExtensionFromConfig_DoNotRedirectDoNotStoreCookieWithExtension()
        {
            var cookieProviderMock = MockRepository.GenerateMock<IUserInQueueStateRepository>();
            string queueId = "queueId";
            var config = new EventConfig()
            {
                EventId = "e1",
                QueueDomain = "testDomain",
                CookieValidityMinute = 10,
                ExtendCookieValidity = false
            };

            cookieProviderMock.Stub(stub => stub.GetState("",""))
                .IgnoreArguments().Return(new StateInfo(true,queueId,true));
         

            UserInQueueService testObject = new UserInQueueService(cookieProviderMock);

            var result = testObject.ValidateRequest("url", "token",config,"testCustomer", "key");
            Assert.True(!result.DoRedirect);
            Assert.True(result.QueueId ==queueId);
            cookieProviderMock.AssertWasNotCalled(stub => stub.Store("",queueId, true, "", 0, ""),
                   options => options.IgnoreArguments());
            Assert.True(config.EventId == result.EventId);


        }

        [Fact]
        public void ValidateRequest_ValidState_ExtendableCookie_CookieExtensionFromConfig_DoNotRedirectDoStoreCookieWithExtension()
        {
            var cookieProviderMock = MockRepository.GenerateMock<IUserInQueueStateRepository>();
            string queueId = "queueId";

            var config = new EventConfig()
            {
                EventId = "e1",
                QueueDomain = "testdomain",
                CookieValidityMinute = 20,
                ExtendCookieValidity = true,
                CookieDomain = ".testdomain.com"
            };



            cookieProviderMock.Stub(stub => stub.GetState("", ""))
                          .IgnoreArguments().Return(new StateInfo(true, queueId, true));

            UserInQueueService testObject = new UserInQueueService(cookieProviderMock);

            var result = testObject.ValidateRequest("url", "token", config, "testCustomer", "key");
            Assert.True(!result.DoRedirect);
            Assert.True(result.QueueId == queueId);

            cookieProviderMock.AssertWasCalled(stub => stub.Store(
                                Arg<string>.Is.Equal("e1"),
                                Arg<string>.Is.Equal(queueId),
                                Arg<bool>.Is.Equal(true),
                                Arg<string>.Is.Equal(config.CookieDomain),
                                Arg<int>.Is.Equal(config.CookieValidityMinute),
                                Arg<string>.Is.Equal("key")));

            Assert.True(config.EventId == result.EventId);
        }

        #endregion
        [Fact]
        public void ValidateRequest_ValidState_NoExtendableCookie_DoNotRedirectDoNotStoreCookieWithExtension()
        {

            var cookieProviderMock = MockRepository.GenerateMock<IUserInQueueStateRepository>();
            string queueId = "queueId";

            var config = new EventConfig()
            {
                EventId = "e1",
                QueueDomain = "testDomain.com",
                CookieValidityMinute = 10,
                ExtendCookieValidity = true
            };
            var customerKey = "4e1db821-a825-49da-acd0-5d376f2068db";

            cookieProviderMock.Stub(stub => stub.GetState("", ""))
              .IgnoreArguments().Return(new StateInfo(true, queueId, false));



            UserInQueueService testObject = new UserInQueueService(cookieProviderMock);

            var result = testObject.ValidateRequest("url", "token",config,"testCustomer",customerKey);
            Assert.True(result.QueueId == queueId);
            Assert.True(!result.DoRedirect);
            cookieProviderMock.AssertWasNotCalled(stub =>
                                stub.Store(null, null,false, null, 0, null), options => options.IgnoreArguments());
            Assert.True(config.EventId == result.EventId);
        }




        [Fact]
        public void ValidateRequest_NoCookie_TampredToken_RedirectToErrorPageWithHashError_DoNotStoreCookie()
        {
            Exception expectedException = new Exception();
            var cookieProviderMock = MockRepository.GenerateMock<IUserInQueueStateRepository>();

            var config = new EventConfig()
            {
                EventId = "e1",
                QueueDomain = "testDomain.com",
                CookieValidityMinute = 10,
                ExtendCookieValidity = false,
                Version= 100
            };
            var customerKey = "4e1db821-a825-49da-acd0-5d376f2068db";
            var queueId = "iopdb821-a825-49da-acd0-5d376f2068db";

            cookieProviderMock.Stub(stub => stub.GetState("", "")).IgnoreArguments().Return(new StateInfo(false,"",false));
            string hash = "";

            var queueitToken = QueueITTokenGenerator.GenerateToken(
                                  DateTime.UtcNow.AddHours(1),
                                  "e1",
                                  queueId,
                                  false,
                                  20,
                               
                                  customerKey,
                                  out hash

                          );
            queueitToken = queueitToken.Replace("False", "True");
            var currentUrl = "http://test.test.com?b=h";
            var knownUserVersion = typeof(UserInQueueService).Assembly.GetName().Version.ToString();//queryStringList.Add($"ver=c{}");
            var expectedErrorUrl = $"https://testDomain.com/error/hash?c=testCustomer&e=e1" +
                $"&ver=v3-{knownUserVersion}"
                + $"&cver=100"
                + $"&queueittoken={queueitToken}"
                + $"&t={HttpUtility.UrlEncode(currentUrl)}";


            UserInQueueService testObject = new UserInQueueService(cookieProviderMock);
            var result = testObject.ValidateRequest(currentUrl, queueitToken, config, "testCustomer", customerKey);
            Assert.True(result.DoRedirect);

            var regex = new Regex("&ts=[^&]*");
            var match = regex.Match(result.RedirectUrl);
            var serverTimestamp = DateTimeHelper.GetUnixTimeStampAsDate(match.Value.Replace("&ts=", "").Replace("&", ""));
            Assert.True(DateTime.UtcNow.Subtract(serverTimestamp) < TimeSpan.FromSeconds(10));
            var redirectUrl = regex.Replace(result.RedirectUrl, "");
            Assert.True(redirectUrl.ToUpper() == expectedErrorUrl.ToUpper());
            Assert.True(config.EventId == result.EventId);
            cookieProviderMock.AssertWasNotCalled(stub => stub.Store("","", true, "", 0, ""),
                options => options.IgnoreArguments());

        }

        [Fact]
        public void ValidateRequest_NoCookie_ExpiredTimeStampInToken_RedirectToErrorPageWithTimeStampError_DoNotStoreCookie()
        {
            var cookieProviderMock = MockRepository.GenerateMock<IUserInQueueStateRepository>();

            var config = new EventConfig()
            {
                EventId = "e1",
                QueueDomain = "testDomain.com",
                CookieValidityMinute = 10,
                ExtendCookieValidity = false,
                Version= 100
            };
            var customerKey = "4e1db821-a825-49da-acd0-5d376f2068db";
            var queueId = "iopdb821-a825-49da-acd0-5d376f2068db";



            cookieProviderMock.Stub(stub => stub.GetState("", "")).IgnoreArguments().Return(new StateInfo(false, "", false));
            string hash = null;
            var queueitToken = QueueITTokenGenerator.GenerateToken(
                                    DateTime.UtcNow.AddHours(-1),
                                    "e1",
                                    queueId,
                                    true,
                                    20,
                                    customerKey,
                                    out hash
                            );
            var currentUrl = "http://test.test.com?b=h";
            var knownUserVersion = typeof(UserInQueueService).Assembly.GetName().Version.ToString();//queryStringList.Add($"ver=c{}");
            var expectedErrorUrl = $"https://testDomain.com/error/timestamp?c=testCustomer&e=e1" +
                $"&ver=v3-{knownUserVersion}"
                + $"&cver=100"
                + $"&queueittoken={queueitToken}"
                
                + $"&t={HttpUtility.UrlEncode(currentUrl)}";


            UserInQueueService testObject = new UserInQueueService(cookieProviderMock);

            var result = testObject.ValidateRequest(currentUrl, queueitToken,config,"testCustomer", customerKey);
            Assert.True(result.DoRedirect);
            var regex = new Regex("&ts=[^&]*");
            var match = regex.Match(result.RedirectUrl);
            var serverTimestamp = DateTimeHelper.GetUnixTimeStampAsDate(match.Value.Replace("&ts=", "").Replace("&", ""));
            Assert.True(DateTime.UtcNow.Subtract(serverTimestamp) < TimeSpan.FromSeconds(10));
            var redirectUrl = regex.Replace(result.RedirectUrl, "");
            Assert.True(redirectUrl.ToUpper() == expectedErrorUrl.ToUpper());
            Assert.True(config.EventId == result.EventId);
            cookieProviderMock.AssertWasNotCalled(stub => stub.Store("","", true, "", 0, ""),
                options => options.IgnoreArguments());

        }
        [Fact]
        public void ValidateRequest_NoCookie_EventIdMismatch_RedirectToErrorPageWithEventIdMissMatchError_DoNotStoreCookie()
        {
            var cookieProviderMock = MockRepository.GenerateMock<IUserInQueueStateRepository>();
            var config = new EventConfig()
            {
                EventId = "e2",
                QueueDomain = "testDomain.com",
                CookieValidityMinute = 10,
                ExtendCookieValidity = false,
                Version= 10

            };
            var customerKey = "4e1db821-a825-49da-acd0-5d376f2068db";
            var queueId = "iopdb821-a825-49da-acd0-5d376f2068db";
            cookieProviderMock.Stub(stub => stub.GetState("", "")).IgnoreArguments().Return(new StateInfo(false, "", false));
            string hash = "";

            var queueitToken = QueueITTokenGenerator.GenerateToken(
                                  DateTime.UtcNow.AddHours(1),
                                  "e1",
                                  queueId,
                                  true,
                                  null,
                                  customerKey,
                                  out hash
                          );

            var currentUrl = "http://test.test.com?b=h";
            var knownUserVersion = typeof(UserInQueueService).Assembly.GetName().Version.ToString();//queryStringList.Add($"ver=c{}");
            var expectedErrorUrl = $"https://testDomain.com/error/eventid?c=testCustomer&e=e2" +
                $"&ver=v3-{knownUserVersion}"+ "&cver=10"
                + $"&queueittoken={queueitToken}"
                + $"&t={HttpUtility.UrlEncode(currentUrl)}";


            UserInQueueService testObject = new UserInQueueService(cookieProviderMock);

            var result = testObject.ValidateRequest(currentUrl, queueitToken,config,"testCustomer", customerKey);
            Assert.True(result.DoRedirect);
            var regex = new Regex("&ts=[^&]*");
            var match = regex.Match(result.RedirectUrl);
            var serverTimestamp = DateTimeHelper.GetUnixTimeStampAsDate(match.Value.Replace("&ts=", "").Replace("&", ""));
            Assert.True(DateTime.UtcNow.Subtract(serverTimestamp) < TimeSpan.FromSeconds(10));

            var redirectUrl = regex.Replace(result.RedirectUrl, "");
            Assert.True(redirectUrl.ToUpper() == expectedErrorUrl.ToUpper());
            Assert.True(config.EventId == result.EventId);
            cookieProviderMock.AssertWasNotCalled(stub => stub.Store("","", true, "", 0, ""),
                options => options.IgnoreArguments());

        }

        [Fact]
        public void ValidateRequest_NoCookie_ValidToken_ExtendableCookie_DoNotRedirect_StoreEextendableCookie()
        {
            var cookieProviderMock = MockRepository.GenerateMock<IUserInQueueStateRepository>();

            var config = new EventConfig()
            {
                EventId = "e1",
                QueueDomain = "testDomain.com",
                CookieValidityMinute = 10,
                ExtendCookieValidity = false
            };
            var customerKey = "4e1db821-a825-49da-acd0-5d376f2068db";

            var queueId = "iopdb821-a825-49da-acd0-5d376f2068db";
            cookieProviderMock.Stub(stub => stub.GetState("", "")).IgnoreArguments().Return(new StateInfo(false, "", false));
            string hash = "";

            var queueitToken = QueueITTokenGenerator.GenerateToken(
                                  DateTime.UtcNow.AddHours(1),
                                  "e1",
                                  queueId,
                                  true,
                                  null,
                                  customerKey,
                                  out hash

                          );

            var currentUrl = "http://test.test.com?b=h";
            UserInQueueService testObject = new UserInQueueService(cookieProviderMock);

            var result = testObject.ValidateRequest(currentUrl, queueitToken,config,"testCustomer", customerKey);
            Assert.True(!result.DoRedirect);

            cookieProviderMock.AssertWasCalled(stub => stub.Store(
                                     Arg<string>.Is.Equal("e1"),
                                     Arg<string>.Is.Equal(queueId),

                                     Arg<bool>.Is.Equal(true),
                                     Arg<string>.Is.Equal(config.CookieDomain),
                                     Arg<int>.Is.Equal(config.CookieValidityMinute),
                                     Arg<string>.Is.Equal(customerKey)));
            Assert.True(config.EventId == result.EventId);

        }




        [Fact]
        public void ValidateRequest_NoCookie_ValidToken_CookieValidityMinuteFromToken_DoNotRedirect_StoreNonEextendableCookie()
        {
            var cookieProviderMock = MockRepository.GenerateMock<IUserInQueueStateRepository>();

            var config = new EventConfig()
            {
                EventId = "eventid",
                QueueDomain = "testDomain.com",
                CookieValidityMinute = 10,
                ExtendCookieValidity = true
            };
            var customerKey = "secretekeyofuser";
            var queueId = "f8757c2d-34c2-4639-bef2-1736cdd30bbb";

            cookieProviderMock.Stub(stub => stub.GetState("", "")).IgnoreArguments().Return(new StateInfo(false, "", false));

            var queueitToken = "e_eventid~q_f8757c2d-34c2-4639-bef2-1736cdd30bbb~ri_34678c2d-34c2-4639-bef2-1736cdd30bbb~ts_1797033600~ce_False~cv_3~rt_DirectLink~h_5ee2babc3ac9fae9d80d5e64675710c371876386e77209f771007dc3e093e326";

            var currentUrl = "http://test.test.com?b=h";



            UserInQueueService testObject = new UserInQueueService(cookieProviderMock);

            var result = testObject.ValidateRequest(currentUrl, queueitToken,config,"testCustomer", customerKey);
            Assert.True(!result.DoRedirect);

            cookieProviderMock.AssertWasCalled(stub => stub.Store(
                                     Arg<string>.Is.Equal("eventid"),
                                     Arg<string>.Is.Equal(queueId),

                                     Arg<bool>.Is.Equal(false),
                                     Arg<string>.Is.Equal(config.CookieDomain),
                                     Arg<int>.Is.Equal(3),
                                     Arg<string>.Is.Equal(customerKey)));
            Assert.True(config.EventId == result.EventId);

        }



        [Fact]
        public void ValidateRequest_NoCookie_WithoutToken_RedirectToQueue()
        {

            var cookieProviderMock = MockRepository.GenerateMock<IUserInQueueStateRepository>();

            var config = new EventConfig()
            {
                EventId = "e1",
                QueueDomain = "testDomain.com",
                CookieValidityMinute = 10,
                ExtendCookieValidity = false,
                Culture = null,
                LayoutName = "testlayout",
                Version= 10

            };



            cookieProviderMock.Stub(stub => stub.GetState("", "")).IgnoreArguments().Return(new StateInfo(false, "", false));

            UserInQueueService testObject = new UserInQueueService(cookieProviderMock);
            var currentUrl = "http://test.test.com?b=h";
            var knownUserVersion = typeof(UserInQueueService).Assembly.GetName().Version.ToString();//queryStringList.Add($"ver=c{}");
   
            var expectedUrl = $"https://testDomain.com?c=testCustomer&e=e1" +
             $"&ver=v3-{knownUserVersion}" +
             $"&cver=10" +


             $"&l={config.LayoutName}" +
             $"&t={HttpUtility.UrlEncode(currentUrl)}";
            var result = testObject.ValidateRequest(currentUrl,"",config,"testCustomer", "key");

            Assert.True(result.DoRedirect);
            Assert.True(result.RedirectUrl.ToUpper() == expectedUrl.ToUpper());
            cookieProviderMock.AssertWasNotCalled(stub =>
                                stub.Store(null,null, true, null, 0, null), options => options.IgnoreArguments());
            Assert.True(config.EventId == result.EventId);

        }
        [Fact]
        public void ValidateRequest_NoCookie_InValidToken()
        {

            var cookieProviderMock = MockRepository.GenerateMock<IUserInQueueStateRepository>();

            var config = new EventConfig()
            {
                EventId = "e1",
                QueueDomain = "testDomain.com",
                CookieValidityMinute = 10,
                ExtendCookieValidity = false,
                Culture = null,
                LayoutName = "testlayout",
                Version = 10

            };
            cookieProviderMock.Stub(stub => stub.GetState("", "")).IgnoreArguments().Return(new StateInfo(false, "", false));

            UserInQueueService testObject = new UserInQueueService(cookieProviderMock);
            var currentUrl = "http://test.test.com?b=h";
            var knownUserVersion = typeof(UserInQueueService).Assembly.GetName().Version.ToString();//queryStringList.Add($"ver=c{}");

            var expectedUrl = $"https://testDomain.com?c=testCustomer&e=e1" +
             $"&ver=v3-{knownUserVersion}" +
             $"&cver=10" +


             $"&l={config.LayoutName}" +
             $"&t={HttpUtility.UrlEncode(currentUrl)}";
            var result = testObject.ValidateRequest(currentUrl, "ts_sasa~cv_adsasa~ce_falwwwse~q_944c1f44-60dd-4e37-aabc-f3e4bb1c8895", config, "testCustomer", "key");

            Assert.True(result.DoRedirect);
            Assert.True(result.RedirectUrl.StartsWith($"https://testDomain.com/error/hash?c=testCustomer&e=e1&ver=v3-{knownUserVersion}&cver=10&l=testlayout&queueittoken=ts_sasa~cv_adsasa~ce_falwwwse~q_944c1f44-60dd-4e37-aabc-f3e4bb1c8895&"));
            cookieProviderMock.AssertWasNotCalled(stub =>
                                stub.Store(null,null, true, null, 0, null), options => options.IgnoreArguments());
            Assert.True(config.EventId == result.EventId);
            cookieProviderMock.AssertWasNotCalled(stub => stub.Store("", null,true, "", 0, ""),
                options => options.IgnoreArguments());

        }
    }


    public class QueueITTokenGenerator
    {
        public static string GenerateToken(
                    DateTime timeStamp,
                    string eventId,
                    string queueId,
                    bool extendableCookie,
                    int? cookieValidityMinute,
                 
                    string secretKey,
                    out string hash
            )
        {
            var paramList = new List<string>();
            paramList.Add(QueueParameterHelper.TimeStampKey + QueueParameterHelper.KeyValueSeparatorChar + GetUnixTimestamp(timeStamp));
            if (cookieValidityMinute != null)
                paramList.Add(QueueParameterHelper.CookieValidityMinuteKey + QueueParameterHelper.KeyValueSeparatorChar + cookieValidityMinute);
            paramList.Add(QueueParameterHelper.EventIdKey + QueueParameterHelper.KeyValueSeparatorChar + eventId);
            paramList.Add(QueueParameterHelper.ExtendableCookieKey + QueueParameterHelper.KeyValueSeparatorChar + extendableCookie);
            paramList.Add(QueueParameterHelper.QueueIdKey + QueueParameterHelper.KeyValueSeparatorChar + queueId);



            var tokenWithoutHash = string.Join(QueueParameterHelper.KeyValueSeparatorGroupChar.ToString(), paramList);
            hash = GetSHA256Hash(tokenWithoutHash, secretKey);

            return tokenWithoutHash + QueueParameterHelper.KeyValueSeparatorGroupChar.ToString() + QueueParameterHelper.HashKey + QueueParameterHelper.KeyValueSeparatorChar + hash;


        }
        private static string GetUnixTimestamp(DateTime dateTime)
        {
            return ((Int32)(dateTime.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc))).TotalSeconds).ToString();
        }
        public static string GetSHA256Hash(string stringToHash, string key)
        {
            using (HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key)))
            {
                byte[] data = hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToHash));

                StringBuilder sBuilder = new StringBuilder();
                for (int i = 0; i < data.Length; i++)
                {
                    sBuilder.Append(data[i].ToString("x2"));
                }
                return sBuilder.ToString();
            }


        }
    }
}
