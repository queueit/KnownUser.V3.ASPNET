using QueueIT.KnownUserV3.SDK.IntegrationConfig;
using System;
using System.Collections.Generic;
using System.Web;
using Xunit;
using System.Collections.Specialized;

namespace QueueIT.KnownUserV3.SDK.Tests
{
    public class KnowUserTest
    {
        class MockHttpRequest : HttpRequestBase
        {
            public MockHttpRequest()
            {
                this.QueryStringValue = new NameValueCollection();
            }
            public HttpCookieCollection CookiesValue { get; set; }
            public string UserAgentValue { get; set; }
            public NameValueCollection QueryStringValue { get; set; }
            public Uri UrlValue { get; set; }
            public override string UserAgent
            {
                get
                {
                    return UserAgentValue;
                }
            }
            public override HttpCookieCollection Cookies => this.CookiesValue;
            public override NameValueCollection QueryString => QueryStringValue;
            public override Uri Url => UrlValue;
        }
        class MockHttpResponse : HttpResponseBase
        {
            public HttpCookieCollection CookiesValue { get; set; }
            public override HttpCookieCollection Cookies => this.CookiesValue;
        }

        class HttpContextMock : HttpContextBase
        {
            public MockHttpRequest MockRequest { get; set; }
            public override HttpRequestBase Request
            {
                get
                {
                    return MockRequest;
                }
            }
            public MockHttpResponse MockResponse { get; set; }
            public override HttpResponseBase Response
            {
                get
                {
                    return MockResponse;
                }
            }
        }

        class UserInQueueServiceMock : IUserInQueueService
        {
            public List<List<string>> validateQueueRequestCalls = new List<List<string>>();
            public List<List<string>> extendQueueCookieCalls = new List<List<string>>();
            public List<List<string>> cancelRequestCalls = new List<List<string>>();
            
            public RequestValidationResult ValidateQueueRequest(string targetUrl, string queueitToken, QueueEventConfig config, string customerId, string secretKey)
            {
                List<string> args = new List<string>();
                args.Add(targetUrl);
                args.Add(queueitToken);
                args.Add(config.CookieDomain + ":"
                        + config.LayoutName + ":"
                        + config.Culture + ":"
                        + config.EventId + ":"
                        + config.QueueDomain + ":"
                        + config.ExtendCookieValidity.ToString().ToLower() + ":"
                        + config.CookieValidityMinute + ":"
                        + config.Version);
                args.Add(customerId);
                args.Add(secretKey);
                validateQueueRequestCalls.Add(args);

                return null;
            }

            public void ExtendQueueCookie(string eventId, int cookieValidityMinute, string secretKey)
            {
                List<string> args = new List<string>();
                args.Add(eventId);
                args.Add(cookieValidityMinute.ToString());
                args.Add(secretKey);
                extendQueueCookieCalls.Add(args);
            }

            public RequestValidationResult ValidateCancelRequest(string targetUrl, CancelEventConfig config, string customerId, string secretKey)
            {
                List<string> args = new List<string>();
                args.Add(targetUrl);
                args.Add(config.CookieDomain + ":"
                        + config.EventId + ":"
                        + config.QueueDomain + ":"
                        + config.Version);
                args.Add(customerId);
                args.Add(secretKey);
                cancelRequestCalls.Add(args);

                return null;
            }
        }

        [Fact]
        public void CancelRequestByLocalConfig_Test()
        {
            // Arrange
            UserInQueueServiceMock mock = new UserInQueueServiceMock();
            KnownUser._UserInQueueService = (mock);
            var cancelEventConfig = new CancelEventConfig() {  CookieDomain="cookiedomain", EventId="eventid", QueueDomain="queuedomain", Version=1};
            // Act
            KnownUser.CancelRequestByLocalConfig("url", "queueitToken", cancelEventConfig,"customerid","secretekey");

            // Assert
            Assert.Equal("url", mock.cancelRequestCalls[0][0]);
            Assert.Equal("cookiedomain:eventid:queuedomain:1", mock.cancelRequestCalls[0][1]);
            Assert.Equal("customerid", mock.cancelRequestCalls[0][2]);
            Assert.Equal("secretekey", mock.cancelRequestCalls[0][3]);
        }

        [Fact]
        public void CancelRequestByLocalConfig_NullQueueDomain_Test()
        {
            // Arrange
            UserInQueueServiceMock mock = new UserInQueueServiceMock();
            KnownUser._UserInQueueService = (mock);
            bool exceptionWasThrown = false;

            CancelEventConfig eventConfig = new CancelEventConfig();
            eventConfig.EventId = "eventid";
            eventConfig.CookieDomain = "cookieDomain";
            eventConfig.Version = 12;

            // Act
            try
            {
                KnownUser.CancelRequestByLocalConfig("targetUrl", "queueitToken", eventConfig, "customerId", "secretKey");
            }
            catch (ArgumentException ex)
            {
                exceptionWasThrown = ex.Message == "QueueDomain from cancelEventConfig can not be null or empty.";
            }

            // Assert
            Assert.True(mock.cancelRequestCalls.Count == 0);
            Assert.True(exceptionWasThrown);
        }

        [Fact]
        public void CancelRequestByLocalConfig_EventIdNull_Test()
        {
            // Arrange
            UserInQueueServiceMock mock = new UserInQueueServiceMock();
            KnownUser._UserInQueueService = (mock);
            bool exceptionWasThrown = false;

            CancelEventConfig eventConfig = new CancelEventConfig();
            eventConfig.CookieDomain = "domain";
            eventConfig.Version = 12;

            // Act
            try
            {
                KnownUser.CancelRequestByLocalConfig("targetUrl", "queueitToken", eventConfig, "customerId", "secretKey");
            }
            catch (ArgumentException ex)
            {
                exceptionWasThrown = ex.Message == "EventId from cancelEventConfig can not be null or empty.";
            }

            // Assert
            Assert.True(mock.cancelRequestCalls.Count == 0);
            Assert.True(exceptionWasThrown);
        }

        [Fact]
        public void CancelRequestByLocalConfig_CancelEventConfigNull_Test()
        {
            // Arrange
            UserInQueueServiceMock mock = new UserInQueueServiceMock();
            KnownUser._UserInQueueService = (mock);
            bool exceptionWasThrown = false;
            
            // Act
            try
            {
                KnownUser.CancelRequestByLocalConfig("targetUrl", "queueitToken", null, "customerId", "secretKey");
            }
            catch (ArgumentException ex)
            {
                exceptionWasThrown = ex.Message == "cancelEventConfig can not be null.";
            }

            // Assert
            Assert.True(mock.cancelRequestCalls.Count == 0);
            Assert.True(exceptionWasThrown);
        }

        [Fact]
        public void CancelRequestByLocalConfig_CustomerIdNull_Test()
        {
            // Arrange
            UserInQueueServiceMock mock = new UserInQueueServiceMock();
            KnownUser._UserInQueueService = (mock);
            bool exceptionWasThrown = false;

            // Act
            try
            {
                KnownUser.CancelRequestByLocalConfig("targetUrl", "queueitToken", new CancelEventConfig(), null, "secretKey");
            }
            catch (ArgumentException ex)
            {
                exceptionWasThrown = ex.Message == "customerId can not be null or empty.";
            }

            // Assert
            Assert.True(mock.cancelRequestCalls.Count == 0);
            Assert.True(exceptionWasThrown);
        }

        [Fact]
        public void CancelRequestByLocalConfig_SeceretKeyNull_Test()
        {
            // Arrange
            UserInQueueServiceMock mock = new UserInQueueServiceMock();
            KnownUser._UserInQueueService = (mock);
            bool exceptionWasThrown = false;

            // Act
            try
            {
                KnownUser.CancelRequestByLocalConfig("targetUrl", "queueitToken", new CancelEventConfig(), "customerid", null);
            }
            catch (ArgumentException ex)
            {
                exceptionWasThrown = ex.Message == "secretKey can not be null or empty.";
            }

            // Assert
            Assert.True(mock.cancelRequestCalls.Count == 0);
            Assert.True(exceptionWasThrown);
        }

        [Fact]
        public void CancelRequestByLocalConfig_TargetUrl_Test()
        {
            // Arrange
            UserInQueueServiceMock mock = new UserInQueueServiceMock();
            KnownUser._UserInQueueService = (mock);
            bool exceptionWasThrown = false;

            // Act
            try
            {
                KnownUser.CancelRequestByLocalConfig(null, "queueitToken", new CancelEventConfig(), "customerid", "secretkey");
            }
            catch (ArgumentException ex)
            {
                exceptionWasThrown = ex.Message == "targeturl can not be null or empty.";
            }

            // Assert
            Assert.True(mock.cancelRequestCalls.Count == 0);
            Assert.True(exceptionWasThrown);
        }

        [Fact]
        public void ExtendQueueCookie_NullEventId_Test()
        {
            // Arrange
            UserInQueueServiceMock mock = new UserInQueueServiceMock();
            KnownUser._UserInQueueService = (mock);
            bool exceptionWasThrown = false;

            // Act
            try
            {
                KnownUser.ExtendQueueCookie(null, 0, null);
            }
            catch (ArgumentException ex)
            {
                exceptionWasThrown = ex.Message == "eventId can not be null or empty.";
            }

            // Assert
            Assert.True(mock.extendQueueCookieCalls.Count == 0);
            Assert.True(exceptionWasThrown);
        }

        [Fact]
        public void ExtendQueueCookie_InvalidCookieValidityMinutes_Test()
        {
            // Arrange        
            UserInQueueServiceMock mock = new UserInQueueServiceMock();
            KnownUser._UserInQueueService = (mock);
            bool exceptionWasThrown = false;

            // Act
            try
            {
                KnownUser.ExtendQueueCookie("eventId", 0, null);
            }
            catch (ArgumentException ex)
            {
                exceptionWasThrown = ex.Message == "cookieValidityMinute should be greater than 0.";
            }

            // Assert
            Assert.True(mock.extendQueueCookieCalls.Count == 0);
            Assert.True(exceptionWasThrown);
        }

        [Fact]
        public void ExtendQueueCookie_NullSecretKey_Test()
        {
            // Arrange
            UserInQueueServiceMock mock = new UserInQueueServiceMock();
            KnownUser._UserInQueueService = (mock);
            bool exceptionWasThrown = false;

            // Act
            try
            {
                KnownUser.ExtendQueueCookie("eventId", 20, null);
            }
            catch (ArgumentException ex)
            {
                exceptionWasThrown = ex.Message == "secretKey can not be null or empty.";
            }

            // Assert
            Assert.True(mock.extendQueueCookieCalls.Count == 0);
            Assert.True(exceptionWasThrown);
        }

        [Fact]
        public void ExtendQueueCookie_Test()
        {
            // Arrange
            UserInQueueServiceMock mock = new UserInQueueServiceMock();
            KnownUser._UserInQueueService = (mock);

            // Act
            KnownUser.ExtendQueueCookie("eventId", 20, "secretKey");

            // Assert
            Assert.Equal("eventId", mock.extendQueueCookieCalls[0][0]);
            Assert.Equal("20", mock.extendQueueCookieCalls[0][1]);
            Assert.Equal("secretKey", mock.extendQueueCookieCalls[0][2]);
        }

        [Fact]
        public void ResolveQueueRequestByLocalConfig_NullCustomerId_Test()
        {
            // Arrange
            var httpContextMock = new HttpContextMock() { MockRequest = new MockHttpRequest() { } };
            KnownUser._HttpContextBase = httpContextMock;
            UserInQueueServiceMock mock = new UserInQueueServiceMock();
            KnownUser._UserInQueueService = (mock);
            bool exceptionWasThrown = false;

            // Act
            try
            {
                KnownUser.ResolveQueueRequestByLocalConfig("targetUrl", "queueitToken", null, null, "secretKey");
            }
            catch (ArgumentException ex)
            {
                exceptionWasThrown = ex.Message == "customerId can not be null or empty.";
            }

            // Assert
            Assert.True(mock.validateQueueRequestCalls.Count == 0);
            Assert.True(exceptionWasThrown);
        }

        [Fact]
        public void ResolveQueueRequestByLocalConfig_NullSecretKey_Test()
        {
            // Arrange
            var httpContextMock = new HttpContextMock() { MockRequest = new MockHttpRequest() { } };
            KnownUser._HttpContextBase = httpContextMock;
            UserInQueueServiceMock mock = new UserInQueueServiceMock();
            KnownUser._UserInQueueService = (mock);
            bool exceptionWasThrown = false;

            // Act
            try
            {
                KnownUser.ResolveQueueRequestByLocalConfig("targetUrl", "queueitToken", null, "customerId", null);
            }
            catch (ArgumentException ex)
            {
                exceptionWasThrown = ex.Message == "secretKey can not be null or empty.";
            }

            // Assert
            Assert.True(mock.validateQueueRequestCalls.Count == 0);
            Assert.True(exceptionWasThrown);
        }

        [Fact]
        public void ResolveQueueRequestByLocalConfig_NullEventConfig_Test()
        {
            // Arrange
            UserInQueueServiceMock mock = new UserInQueueServiceMock();
            KnownUser._UserInQueueService = (mock);
            bool exceptionWasThrown = false;

            // Act
            try
            {
                KnownUser.ResolveQueueRequestByLocalConfig("targetUrl", "queueitToken", null, "customerId", "secretKey");
            }
            catch (ArgumentException ex)
            {
                exceptionWasThrown = ex.Message == "eventConfig can not be null.";
            }

            // Assert
            Assert.True(mock.validateQueueRequestCalls.Count == 0);
            Assert.True(exceptionWasThrown);
        }

        [Fact]
        public void ResolveRequestByLocalEventConfigNullEventIdTest()
        {
            // Arrange
            var httpContextMock = new HttpContextMock() { MockRequest = new MockHttpRequest() { } };
            KnownUser._HttpContextBase = httpContextMock;
            UserInQueueServiceMock mock = new UserInQueueServiceMock();
            KnownUser._UserInQueueService = (mock);
            bool exceptionWasThrown = false;

            QueueEventConfig eventConfig = new QueueEventConfig();
            eventConfig.CookieDomain = "cookieDomain";
            eventConfig.LayoutName = "layoutName";
            eventConfig.Culture = "culture";
            //eventConfig.EventId = "eventId";
            eventConfig.QueueDomain = "queueDomain";
            eventConfig.ExtendCookieValidity = true;
            eventConfig.CookieValidityMinute = 10;
            eventConfig.Version = 12;

            // Act
            try
            {
                KnownUser.ResolveQueueRequestByLocalConfig("targetUrl", "queueitToken", eventConfig, "customerId", "secretKey");
            }
            catch (ArgumentException ex)
            {
                exceptionWasThrown = ex.Message == "EventId from eventConfig can not be null or empty.";
            }

            // Assert
            Assert.True(mock.validateQueueRequestCalls.Count == 0);
            Assert.True(exceptionWasThrown);
        }

        [Fact]
        public void ResolveRequestByLocalEventConfig_NullQueueDomain_Test()
        {
            // Arrange
            UserInQueueServiceMock mock = new UserInQueueServiceMock();
            KnownUser._UserInQueueService = (mock);
            bool exceptionWasThrown = false;

            QueueEventConfig eventConfig = new QueueEventConfig();
            eventConfig.CookieDomain = "cookieDomain";
            eventConfig.LayoutName = "layoutName";
            eventConfig.Culture = "culture";
            eventConfig.EventId = "eventId";
            //eventConfig.QueueDomain = "queueDomain";
            eventConfig.ExtendCookieValidity = true;
            eventConfig.CookieValidityMinute = 10;
            eventConfig.Version = 12;

            // Act
            try
            {
                KnownUser.ResolveQueueRequestByLocalConfig("targetUrl", "queueitToken", eventConfig, "customerId", "secretKey");
            }
            catch (ArgumentException ex)
            {
                exceptionWasThrown = ex.Message == "QueueDomain from eventConfig can not be null or empty.";
            }

            // Assert
            Assert.True(mock.validateQueueRequestCalls.Count == 0);
            Assert.True(exceptionWasThrown);
        }

        [Fact]
        public void ResolveQueueRequestByLocalConfig_InvalidCookieValidityMinute_Test()
        {
            // Arrange
            UserInQueueServiceMock mock = new UserInQueueServiceMock();
            KnownUser._UserInQueueService = (mock);
            bool exceptionWasThrown = false;

            QueueEventConfig eventConfig = new QueueEventConfig();
            eventConfig.CookieDomain = "cookieDomain";
            eventConfig.LayoutName = "layoutName";
            eventConfig.Culture = "culture";
            eventConfig.EventId = "eventId";
            eventConfig.QueueDomain = "queueDomain";
            eventConfig.ExtendCookieValidity = true;
            //eventConfig.CookieValidityMinute = 10;
            eventConfig.Version = 12;

            // Act
            try
            {
                KnownUser.ResolveQueueRequestByLocalConfig("targetUrl", "queueitToken", eventConfig, "customerId", "secretKey");
            }
            catch (ArgumentException ex)
            {
                exceptionWasThrown = ex.Message == "CookieValidityMinute from eventConfig should be greater than 0.";
            }

            // Assert
            Assert.True(mock.validateQueueRequestCalls.Count == 0);
            Assert.True(exceptionWasThrown);
        }

        [Fact]
        public void ResolveRequestByLocalEventConfig_Test()
        {
            // Arrange
            UserInQueueServiceMock mock = new UserInQueueServiceMock();
            KnownUser._UserInQueueService = (mock);

            QueueEventConfig eventConfig = new QueueEventConfig();
            eventConfig.CookieDomain = "cookieDomain";
            eventConfig.LayoutName = "layoutName";
            eventConfig.Culture = "culture";
            eventConfig.EventId = "eventId";
            eventConfig.QueueDomain = "queueDomain";
            eventConfig.ExtendCookieValidity = true;
            eventConfig.CookieValidityMinute = 10;
            eventConfig.Version = 12;

            // Act
            KnownUser.ResolveQueueRequestByLocalConfig("targetUrl", "queueitToken", eventConfig, "customerId", "secretKey");

            // Assert
            Assert.Equal("targetUrl", mock.validateQueueRequestCalls[0][0]);
            Assert.Equal("queueitToken", mock.validateQueueRequestCalls[0][1]);
            Assert.Equal("cookieDomain:layoutName:culture:eventId:queueDomain:true:10:12", mock.validateQueueRequestCalls[0][2]);
            Assert.Equal("customerId", mock.validateQueueRequestCalls[0][3]);
            Assert.Equal("secretKey", mock.validateQueueRequestCalls[0][4]);
        }

        [Fact]
        public void ValidateRequestByIntegrationConfig_EmptyCurrentUrl_Test()
        {
            var httpContextMock = new HttpContextMock() { MockRequest = new MockHttpRequest() { } };
            KnownUser._HttpContextBase = httpContextMock;
            // Arrange        
            UserInQueueServiceMock mock = new UserInQueueServiceMock();
            KnownUser._UserInQueueService = (mock);
            bool exceptionWasThrown = false;

            // Act
            try
            {
                KnownUser.ValidateRequestByIntegrationConfig("", null, null, null, null);
            }
            catch (Exception ex)
            {
                exceptionWasThrown = ex.Message == "currentUrlWithoutQueueITToken can not be null or empty.";
            }

            // Assert
            Assert.True(mock.validateQueueRequestCalls.Count == 0);
            Assert.True(exceptionWasThrown);
        }

        [Fact]
        public void ValidateRequestByIntegrationConfig_EmptyIntegrationsConfig_Test()
        {
            // Arrange 
            var httpContextMock = new HttpContextMock() { MockRequest = new MockHttpRequest() { } };
            KnownUser._HttpContextBase = httpContextMock;
            UserInQueueServiceMock mock = new UserInQueueServiceMock();
            KnownUser._UserInQueueService = (mock);
            bool exceptionWasThrown = false;

            // Act
            try
            {
                KnownUser.ValidateRequestByIntegrationConfig("currentUrl", "queueitToken", null, null, null);
            }
            catch (Exception ex)
            {
                exceptionWasThrown = ex.Message == "customerIntegrationInfo can not be null.";
            }

            // Assert
            Assert.True(mock.validateQueueRequestCalls.Count == 0);
            Assert.True(exceptionWasThrown);
        }

        [Fact]
        public void ValidateRequestByIntegrationConfig_Test()
        {
            // Arrange
            UserInQueueServiceMock mock = new UserInQueueServiceMock();
            KnownUser._UserInQueueService = (mock);

            TriggerPart triggerPart1 = new TriggerPart();
            triggerPart1.Operator = "Contains";
            triggerPart1.ValueToCompare = "event1";
            triggerPart1.UrlPart = "PageUrl";
            triggerPart1.ValidatorType = "UrlValidator";
            triggerPart1.IsNegative = false;
            triggerPart1.IsIgnoreCase = true;

            TriggerPart triggerPart2 = new TriggerPart();
            triggerPart2.Operator = "Contains";
            triggerPart2.ValueToCompare = "googlebot";
            triggerPart2.ValidatorType = "UserAgentValidator";
            triggerPart2.IsNegative = false;
            triggerPart2.IsIgnoreCase = false;

            TriggerModel trigger = new TriggerModel();
            trigger.LogicalOperator = "And";
            trigger.TriggerParts = new TriggerPart[] { triggerPart1, triggerPart2 };

            IntegrationConfigModel config = new IntegrationConfigModel();
            config.Name = "event1action";
            //config.ActionType = "Queue";
            config.EventId = "event1";
            config.CookieDomain = ".test.com";
            config.LayoutName = "Christmas Layout by Queue-it";
            config.Culture = "da-DK";
            config.ExtendCookieValidity = true;
            config.CookieValidityMinute = 20;
            config.Triggers = new TriggerModel[] { trigger };
            config.QueueDomain = "knownusertest.queue-it.net";
            config.RedirectLogic = "AllowTParameter";
            config.ForcedTargetUrl = "";
            config.ActionType = ActionType.QueueAction;

            CustomerIntegration customerIntegration = new CustomerIntegration();
            customerIntegration.Integrations = new IntegrationConfigModel[] { config };
            customerIntegration.Version = 3;
            var httpContextMock = new HttpContextMock() { MockRequest = new MockHttpRequest() { UserAgentValue = "googlebot", CookiesValue = new HttpCookieCollection() } };
            KnownUser._HttpContextBase = httpContextMock;
            // Act
            KnownUser.ValidateRequestByIntegrationConfig("http://test.com?event1=true", "queueitToken", customerIntegration, "customerId", "secretKey");

            // Assert
            Assert.True(mock.validateQueueRequestCalls.Count == 1);
            Assert.Equal("http://test.com?event1=true", mock.validateQueueRequestCalls[0][0]);
            Assert.Equal("queueitToken", mock.validateQueueRequestCalls[0][1]);
            Assert.Equal(".test.com:Christmas Layout by Queue-it:da-DK:event1:knownusertest.queue-it.net:true:20:3", mock.validateQueueRequestCalls[0][2]);
            Assert.Equal("customerId", mock.validateQueueRequestCalls[0][3]);
            Assert.Equal("secretKey", mock.validateQueueRequestCalls[0][4]);
            KnownUser._HttpContextBase = null;
        }

        [Fact]
        public void ValidateRequestByIntegrationConfig_NotMatch_Test()
        {
            // Arrange
            var httpContextMock = new HttpContextMock() { MockRequest = new MockHttpRequest() { } };
            KnownUser._HttpContextBase = httpContextMock;
            UserInQueueServiceMock mock = new UserInQueueServiceMock();
            KnownUser._UserInQueueService = (mock);

            CustomerIntegration customerIntegration = new CustomerIntegration();
            customerIntegration.Integrations = new IntegrationConfigModel[0];
            customerIntegration.Version = 3;

            // Act
            RequestValidationResult result = KnownUser.ValidateRequestByIntegrationConfig("http://test.com?event1=true", "queueitToken", customerIntegration, "customerId", "secretKey");

            // Assert
            Assert.True(mock.validateQueueRequestCalls.Count == 0);
            Assert.False(result.DoRedirect);
        }

        [Theory]
        [InlineData("ForcedTargetUrl", "http://forcedtargeturl.com")]
        [InlineData("ForecedTargetUrl", "http://forcedtargeturl.com")]
        [InlineData("EventTargetUrl", "")]
        public void ValidateRequestByIntegrationConfig_RedirectLogic_Test(string redirectLogic, string forcedTargetUrl)
        {
            // Arrange
            var httpContextMock = new HttpContextMock() { MockRequest = new MockHttpRequest() { } };
            KnownUser._HttpContextBase = httpContextMock;
            UserInQueueServiceMock mock = new UserInQueueServiceMock();
            KnownUser._UserInQueueService = (mock);

            TriggerPart triggerPart = new TriggerPart();
            triggerPart.Operator = "Contains";
            triggerPart.ValueToCompare = "event1";
            triggerPart.UrlPart = "PageUrl";
            triggerPart.ValidatorType = "UrlValidator";
            triggerPart.IsNegative = false;
            triggerPart.IsIgnoreCase = true;

            TriggerModel trigger = new TriggerModel();
            trigger.LogicalOperator = "And";
            trigger.TriggerParts = new TriggerPart[] { triggerPart };

            IntegrationConfigModel config = new IntegrationConfigModel();
            config.Name = "event1action";
            //config.ActionType = "Queue";
            config.EventId = "event1";
            config.CookieDomain = ".test.com";
            config.LayoutName = "Christmas Layout by Queue-it";
            config.Culture = "da-DK";
            config.ExtendCookieValidity = true;
            config.CookieValidityMinute = 20;
            config.Triggers = new TriggerModel[] { trigger };
            config.QueueDomain = "knownusertest.queue-it.net";
            config.RedirectLogic = redirectLogic;
            config.ForcedTargetUrl = forcedTargetUrl;
            config.ActionType = ActionType.QueueAction;

            CustomerIntegration customerIntegration = new CustomerIntegration();
            customerIntegration.Integrations = new IntegrationConfigModel[] { config };
            customerIntegration.Version = 3;

            // Act
            KnownUser.ValidateRequestByIntegrationConfig("http://test.com?event1=true", "queueitToken", customerIntegration, "customerId", "secretKey");

            // Assert
            Assert.True(mock.validateQueueRequestCalls.Count == 1);
            Assert.Equal(forcedTargetUrl, mock.validateQueueRequestCalls[0][0]);
        }

        [Fact]
        public void ValidateRequestByIntegrationConfig_CancelAction()
        {
            // Arrange

            TriggerPart triggerPart = new TriggerPart();
            triggerPart.Operator = "Contains";
            triggerPart.ValueToCompare = "event1";
            triggerPart.UrlPart = "PageUrl";
            triggerPart.ValidatorType = "UrlValidator";
            triggerPart.IsNegative = false;
            triggerPart.IsIgnoreCase = true;

            TriggerModel trigger = new TriggerModel();
            trigger.LogicalOperator = "And";
            trigger.TriggerParts = new TriggerPart[] { triggerPart };

            IntegrationConfigModel config = new IntegrationConfigModel();
            config.Name = "event1action";
            config.EventId = "eventid";
            config.CookieDomain = "cookiedomain";
            config.Triggers = new TriggerModel[] { trigger };
            config.QueueDomain = "queuedomain";
            config.ActionType = ActionType.CancelAction;

            CustomerIntegration customerIntegration = new CustomerIntegration();
            customerIntegration.Integrations = new IntegrationConfigModel[] { config };
            customerIntegration.Version = 3;

            UserInQueueServiceMock mock = new UserInQueueServiceMock();
            KnownUser._UserInQueueService = (mock);
                      // Act
            KnownUser.ValidateRequestByIntegrationConfig("http://test.com?event1=true", "queueitToken", customerIntegration, "customerid", "secretkey");

            // Assert
            Assert.Equal("http://test.com?event1=true", mock.cancelRequestCalls[0][0]);
            Assert.Equal("cookiedomain:eventid:queuedomain:3", mock.cancelRequestCalls[0][1]);
            Assert.Equal("customerid", mock.cancelRequestCalls[0][2]);
            Assert.Equal("secretkey", mock.cancelRequestCalls[0][3]);
        }

        [Fact]
        public void ValidateRequestByIntegrationConfig_Debug()
        {
            // Arrange 
            var httpContextMock = new HttpContextMock()
            {
                MockRequest = new MockHttpRequest()
                {
                    QueryStringValue = new NameValueCollection()
                    {
                        { "queueittoken", "queueittoken_value"},
                        { "queueitdebug", "queueitdebug_value"}

                    },
                    UrlValue = new Uri("http://test.com/?event1=true&queueittoken=queueittokenvalue"),
                    CookiesValue = new HttpCookieCollection()
                }
                ,
                MockResponse = new MockHttpResponse()
                {
                    CookiesValue = new HttpCookieCollection()
                }
            };
            KnownUser._HttpContextBase = httpContextMock;
            UserInQueueServiceMock mock = new UserInQueueServiceMock();
            KnownUser._UserInQueueService = (mock);

            TriggerPart triggerPart1 = new TriggerPart();
            triggerPart1.Operator = "Contains";
            triggerPart1.ValueToCompare = "event1";
            triggerPart1.UrlPart = "PageUrl";
            triggerPart1.ValidatorType = "UrlValidator";
            triggerPart1.IsNegative = false;
            triggerPart1.IsIgnoreCase = true;



            TriggerModel trigger = new TriggerModel();
            trigger.LogicalOperator = "And";
            trigger.TriggerParts = new TriggerPart[] { triggerPart1 };

            IntegrationConfigModel config = new IntegrationConfigModel();
            config.Name = "event1action";
            //config.ActionType = "Queue";
            config.EventId = "event1";
            config.CookieDomain = ".test.com";
            config.LayoutName = "Christmas Layout by Queue-it";
            config.Culture = "da-DK";
            config.ExtendCookieValidity = true;
            config.CookieValidityMinute = 20;
            config.Triggers = new TriggerModel[] { trigger };
            config.QueueDomain = "knownusertest.queue-it.net";
            config.RedirectLogic = "AllowTParameter";
            config.ForcedTargetUrl = "";

            CustomerIntegration customerIntegration = new CustomerIntegration();
            customerIntegration.Integrations = new IntegrationConfigModel[] { config };
            customerIntegration.Version = 3;
            var queueitToken = QueueITTokenGenerator.GenerateToken(DateTime.UtcNow, "event1",
                Guid.NewGuid().ToString(), true, null, "secretKey", out var hash, "debug");
            
            // Act
            RequestValidationResult result = KnownUser
                .ValidateRequestByIntegrationConfig($"http://test.com?event1=true",
                queueitToken, customerIntegration, "customerId", "secretKey");

            Assert.True(httpContextMock.MockResponse.Cookies["queueitdebug"].Values["pureUrl"]== 
                "http://test.com?event1=true");
            Assert.True(httpContextMock.MockResponse.Cookies["queueitdebug"].Values["queueitToken"] ==
                queueitToken);
            Assert.True(httpContextMock.MockResponse.Cookies["queueitdebug"].Values["configVersion"] ==
                "3");
            Assert.True(httpContextMock.MockResponse.Cookies["queueitdebug"].Values["OriginalUrl"] ==
                "http://test.com/?event1=true&queueittoken=queueittokenvalue");
            Assert.True(httpContextMock.MockResponse.Cookies["queueitdebug"].Values["matchedConfig"] ==
                "event1action");
            Assert.True(httpContextMock.MockResponse.Cookies["queueitdebug"].Values["targetUrl"] ==
                "http://test.com?event1=true");
            Assert.True(httpContextMock.MockResponse.Cookies["queueitdebug"].Values["queueConfig"] ==
                "EventId:event1&Version:3&QueueDomain:knownusertest.queue-it.net&CookieDomain:.test.com&ExtendCookieValidity:True&CookieValidityMinute:20&LayoutName:Christmas Layout by Queue-it&Culture:da-DK");
        }

        [Fact]
        public void ValidateRequestByIntegrationConfig_Debug_WithoutMatch()
        {
            // Arrange 
            var httpContextMock = new HttpContextMock()
            {
                MockRequest = new MockHttpRequest()
                {
                    QueryStringValue = new NameValueCollection()
                    {
                        { "queueittoken", "queueittoken_value"},
                        { "queueitdebug", "queueitdebug_value"}

                    },
                    UrlValue = new Uri("http://test.com/?event1=true&queueittoken=queueittokenvalue"),
                    CookiesValue = new HttpCookieCollection()
                }
                ,
                MockResponse = new MockHttpResponse()
                {
                    CookiesValue = new HttpCookieCollection()
                }
            };
            KnownUser._HttpContextBase = httpContextMock;
            UserInQueueServiceMock mock = new UserInQueueServiceMock();
            KnownUser._UserInQueueService = (mock);


            CustomerIntegration customerIntegration = new CustomerIntegration();
            customerIntegration.Integrations = new IntegrationConfigModel[] {  };
            customerIntegration.Version = 10;

            var queueitToken = QueueITTokenGenerator.GenerateToken(DateTime.UtcNow, "event1",
                        Guid.NewGuid().ToString(), true, null, "secretKey", out var hash, "debug");
            // Act
            RequestValidationResult result = KnownUser
                .ValidateRequestByIntegrationConfig("http://test.com?event1=true",
                queueitToken, customerIntegration, "customerId", "secretKey");

            Assert.True(httpContextMock.MockResponse.Cookies["queueitdebug"].Values["pureUrl"] ==
                "http://test.com?event1=true");
            Assert.True(httpContextMock.MockResponse.Cookies["queueitdebug"].Values["queueitToken"] ==
                queueitToken);
            Assert.True(httpContextMock.MockResponse.Cookies["queueitdebug"].Values["configVersion"] ==
                "10");
            Assert.True(httpContextMock.MockResponse.Cookies["queueitdebug"].Values["OriginalUrl"] ==
                "http://test.com/?event1=true&queueittoken=queueittokenvalue");
            Assert.True(httpContextMock.MockResponse.Cookies["queueitdebug"].Values["matchedConfig"] ==
                "NULL");
           }
        [Fact]
        public void ValidateRequestByIntegrationConfig_Debug_WithoutMatch_NotValidHash()
        {
            // Arrange 
            var httpContextMock = new HttpContextMock()
            {
                MockRequest = new MockHttpRequest()
                {
                    QueryStringValue = new NameValueCollection()
                    {
                        { "queueittoken", "queueittoken_value"},
                        { "queueitdebug", "queueitdebug_value"}

                    },
                    UrlValue = new Uri("http://test.com/?event1=true&queueittoken=queueittokenvalue"),
                    CookiesValue = new HttpCookieCollection()
                }
                ,
                MockResponse = new MockHttpResponse()
                {
                    CookiesValue = new HttpCookieCollection()
                }
            };
            KnownUser._HttpContextBase = httpContextMock;
            UserInQueueServiceMock mock = new UserInQueueServiceMock();
            KnownUser._UserInQueueService = (mock);


            CustomerIntegration customerIntegration = new CustomerIntegration();
            customerIntegration.Integrations = new IntegrationConfigModel[] { };
            customerIntegration.Version = 10;

            var queueitToken = QueueITTokenGenerator.GenerateToken(DateTime.UtcNow, "event1",
                        Guid.NewGuid().ToString(), true, null, "secretKey", out var hash, "debug");
            // Act
            RequestValidationResult result = KnownUser
                .ValidateRequestByIntegrationConfig("http://test.com?event1=true",
                queueitToken+"test", customerIntegration, "customerId", "secretKey");

            Assert.True(httpContextMock.MockResponse.Cookies.AllKeys.Length == 0);
        }
        [Fact]
        public void ResolveQueueRequestByLocalConfig_Debug()
        {
            // Arrange 
            var httpContextMock = new HttpContextMock()
            {
                MockRequest = new MockHttpRequest()
                {
                    QueryStringValue = new NameValueCollection()
                    {
                        { "queueittoken", "queueittoken_value"},
                        { "queueitdebug", "queueitdebug_value"}

                    },
                    UrlValue = new Uri("http://test.com/?event1=true&queueittoken=queueittokenvalue"),
                    CookiesValue = new HttpCookieCollection()
                }
                ,
                MockResponse = new MockHttpResponse()
                {
                    CookiesValue = new HttpCookieCollection()
                }
            };
            KnownUser._HttpContextBase = httpContextMock;
            UserInQueueServiceMock mock = new UserInQueueServiceMock();
            KnownUser._UserInQueueService = (mock);


            QueueEventConfig eventConfig = new QueueEventConfig();
            eventConfig.CookieDomain = "cookieDomain";
            eventConfig.LayoutName = "layoutName";
            eventConfig.Culture = "culture";
            eventConfig.EventId = "eventId";
            eventConfig.QueueDomain = "queueDomain";
            eventConfig.ExtendCookieValidity = true;
            eventConfig.CookieValidityMinute = 10;
            eventConfig.Version = 12;
            var queueitToken = QueueITTokenGenerator.GenerateToken(DateTime.UtcNow, "event1",
            Guid.NewGuid().ToString(), true, null, "secretKey", out var hash, "debug");

            // Act
            RequestValidationResult result = KnownUser
                .ResolveQueueRequestByLocalConfig("http://test.com?event1=true",
                queueitToken, eventConfig, "customerId", "secretKey");


            Assert.True(httpContextMock.MockResponse.Cookies["queueitdebug"].Values["queueitToken"] ==
                queueitToken);
            Assert.True(httpContextMock.MockResponse.Cookies["queueitdebug"].Values["OriginalUrl"] ==
                "http://test.com/?event1=true&queueittoken=queueittokenvalue");
            Assert.True(httpContextMock.MockResponse.Cookies["queueitdebug"].Values["targetUrl"] ==
               "http://test.com?event1=true");
            Assert.True(httpContextMock.MockResponse.Cookies["queueitdebug"].Values["queueConfig"] ==
                "EventId:eventId&Version:12&QueueDomain:queueDomain&CookieDomain:cookieDomain&ExtendCookieValidity:True&CookieValidityMinute:10&LayoutName:layoutName&Culture:culture");
        }

        [Fact]
        public void CancelRequestByLocalConfig_Debug()
        {
            // Arrange 
            var httpContextMock = new HttpContextMock()
            {
                MockRequest = new MockHttpRequest()
                {
                    QueryStringValue = new NameValueCollection()
                    {
                        { "queueittoken", "queueittoken_value"},
                        { "queueitdebug", "queueitdebug_value"}

                    },
                    UrlValue = new Uri("http://test.com/?event1=true&queueittoken=queueittokenvalue"),
                    CookiesValue = new HttpCookieCollection()
                }
                ,
                MockResponse = new MockHttpResponse()
                {
                    CookiesValue = new HttpCookieCollection()
                }
            };
            KnownUser._HttpContextBase = httpContextMock;
            UserInQueueServiceMock mock = new UserInQueueServiceMock();
            KnownUser._UserInQueueService = (mock);


            CancelEventConfig eventConfig = new CancelEventConfig();
            eventConfig.CookieDomain = "cookieDomain";
            eventConfig.EventId = "eventId";
            eventConfig.QueueDomain = "queueDomain";
            eventConfig.Version = 12;

            var queueitToken = QueueITTokenGenerator.GenerateToken(DateTime.UtcNow, "event1",
            Guid.NewGuid().ToString(), true, null, "secretKey", out var hash, "debug");

            // Act
            RequestValidationResult result = KnownUser
                .CancelRequestByLocalConfig("http://test.com?event1=true",
                queueitToken, eventConfig, "customerId", "secretKey");


            Assert.True(httpContextMock.MockResponse.Cookies["queueitdebug"].Values["queueitToken"] ==
                queueitToken);
            Assert.True(httpContextMock.MockResponse.Cookies["queueitdebug"].Values["OriginalUrl"] ==
                "http://test.com/?event1=true&queueittoken=queueittokenvalue");
            Assert.True(httpContextMock.MockResponse.Cookies["queueitdebug"].Values["targetUrl"] ==
               "http://test.com?event1=true");
            Assert.True(httpContextMock.MockResponse.Cookies["queueitdebug"].Values["cancelConfig"] ==
                "EventId:eventId&Version:12&QueueDomain:queueDomain&CookieDomain:cookieDomain");
        }
    }
}
