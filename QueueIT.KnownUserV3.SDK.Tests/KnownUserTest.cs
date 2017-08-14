using QueueIT.KnownUserV3.SDK.IntegrationConfig;
using System;
using System.Collections.Generic;
using System.Web;
using Xunit;

namespace QueueIT.KnownUserV3.SDK.Tests
{
    public class KnowUserTest
    {
        class MockHttpRequest : HttpRequestBase
        {
            public HttpCookieCollection CookiesValue { get; set; }
            public string UserAgentValue { get; set; }
            public override string UserAgent
            {
                get
                {
                    return UserAgentValue;
                }
            }
            public override HttpCookieCollection Cookies => this.CookiesValue;
        }
        class HttpContextMock : HttpContextBase
        {
            public MockHttpRequest MockRequest { get; set; }
            public override HttpRequestBase Request {
                get
                {
                    return MockRequest;
                }
            }
        }
        class UserInQueueServiceMock : IUserInQueueService
        {
            public List<List<string>> validateRequestCalls = new List<List<string>>();
            public List<List<string>> cancelQueueCookieCalls = new List<List<string>>();
            public List<List<string>> extendQueueCookieCalls = new List<List<string>>();

            public RequestValidationResult ValidateRequest(string targetUrl, string queueitToken, EventConfig config, string customerId, string secretKey)
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
                validateRequestCalls.Add(args);

                return null;
            }

            public void CancelQueueCookie(string eventId)
            {
                List<string> args = new List<string>();
                args.Add(eventId);
                cancelQueueCookieCalls.Add(args);
            }

            public void ExtendQueueCookie(string eventId, int cookieValidityMinute, string secretKey)
            {
                List<string> args = new List<string>();
                args.Add(eventId);
                args.Add(cookieValidityMinute.ToString());
                args.Add(secretKey);
                extendQueueCookieCalls.Add(args);
            }
        }

        [Fact]
        public void CancelQueueCookie_Test()
        {
            // Arrange
            UserInQueueServiceMock mock = new UserInQueueServiceMock();
            KnownUser._UserInQueueService = (mock);

            // Act
            KnownUser.CancelQueueCookie("eventId");

            // Assert
            Assert.Equal("eventId", mock.cancelQueueCookieCalls[0][0]);
        }

        [Fact]
        public void CancelQueueCookie_NullEventId_Test()
        {
            // Arrange        
            UserInQueueServiceMock mock = new UserInQueueServiceMock();
            KnownUser._UserInQueueService = (mock);
            bool exceptionWasThrown = false;

            // Act
            try
            {
                KnownUser.CancelQueueCookie(null);
            }
            catch (ArgumentException ex)
            {
                exceptionWasThrown = ex.Message == "eventId can not be null or empty.";
            }

            // Assert
            Assert.True(mock.cancelQueueCookieCalls.Count == 0);
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
        public void ValidateRequestByLocalEventConfig_NullCustomerId_Test()
        {
            // Arrange
            UserInQueueServiceMock mock = new UserInQueueServiceMock();
            KnownUser._UserInQueueService = (mock);
            bool exceptionWasThrown = false;

            // Act
            try
            {
                KnownUser.ValidateRequestByLocalEventConfig("targetUrl", "queueitToken", null, null, "secretKey");
            }
            catch (ArgumentException ex)
            {
                exceptionWasThrown = ex.Message == "customerId can not be null or empty.";
            }

            // Assert
            Assert.True(mock.validateRequestCalls.Count == 0);
            Assert.True(exceptionWasThrown);
        }

        [Fact]
        public void ValidateRequestByLocalEventConfig_NullSecretKey_Test()
        {
            // Arrange
            UserInQueueServiceMock mock = new UserInQueueServiceMock();
            KnownUser._UserInQueueService = (mock);
            bool exceptionWasThrown = false;

            // Act
            try
            {
                KnownUser.ValidateRequestByLocalEventConfig("targetUrl", "queueitToken", null, "customerId", null);
            }
            catch (ArgumentException ex)
            {
                exceptionWasThrown = ex.Message == "secretKey can not be null or empty.";
            }

            // Assert
            Assert.True(mock.validateRequestCalls.Count == 0);
            Assert.True(exceptionWasThrown);
        }

        [Fact]
        public void ValidateRequestByLocalEventConfig_NullEventConfig_Test()
        {
            // Arrange
            UserInQueueServiceMock mock = new UserInQueueServiceMock();
            KnownUser._UserInQueueService = (mock);
            bool exceptionWasThrown = false;

            // Act
            try
            {
                KnownUser.ValidateRequestByLocalEventConfig("targetUrl", "queueitToken", null, "customerId", "secretKey");
            }
            catch (ArgumentException ex)
            {
                exceptionWasThrown = ex.Message == "eventConfig can not be null.";
            }

            // Assert
            Assert.True(mock.validateRequestCalls.Count == 0);
            Assert.True(exceptionWasThrown);
        }

        [Fact]
        public void validateRequestByLocalEventConfigNullEventIdTest()
        {
            // Arrange
            UserInQueueServiceMock mock = new UserInQueueServiceMock();
            KnownUser._UserInQueueService = (mock);
            bool exceptionWasThrown = false;

            EventConfig eventConfig = new EventConfig();
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
                KnownUser.ValidateRequestByLocalEventConfig("targetUrl", "queueitToken", eventConfig, "customerId", "secretKey");
            }
            catch (ArgumentException ex)
            {
                exceptionWasThrown = ex.Message == "EventId from eventConfig can not be null or empty.";
            }

            // Assert
            Assert.True(mock.validateRequestCalls.Count == 0);
            Assert.True(exceptionWasThrown);
        }

        [Fact]
        public void ValidateRequestByLocalEventConfig_NullQueueDomain_Test()
        {
            // Arrange
            UserInQueueServiceMock mock = new UserInQueueServiceMock();
            KnownUser._UserInQueueService = (mock);
            bool exceptionWasThrown = false;

            EventConfig eventConfig = new EventConfig();
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
                KnownUser.ValidateRequestByLocalEventConfig("targetUrl", "queueitToken", eventConfig, "customerId", "secretKey");
            }
            catch (ArgumentException ex)
            {
                exceptionWasThrown = ex.Message == "QueueDomain from eventConfig can not be null or empty.";
            }

            // Assert
            Assert.True(mock.validateRequestCalls.Count == 0);
            Assert.True(exceptionWasThrown);
        }

        [Fact]
        public void ValidateRequestByLocalEventConfig_InvalidCookieValidityMinute_Test()
        {
            // Arrange
            UserInQueueServiceMock mock = new UserInQueueServiceMock();
            KnownUser._UserInQueueService = (mock);
            bool exceptionWasThrown = false;

            EventConfig eventConfig = new EventConfig();
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
                KnownUser.ValidateRequestByLocalEventConfig("targetUrl", "queueitToken", eventConfig, "customerId", "secretKey");
            }
            catch (ArgumentException ex)
            {
                exceptionWasThrown = ex.Message == "CookieValidityMinute from eventConfig should be greater than 0.";
            }

            // Assert
            Assert.True(mock.validateRequestCalls.Count == 0);
            Assert.True(exceptionWasThrown);
        }

        [Fact]
        public void ValidateRequestByLocalEventConfig_Test()
        {
            // Arrange
            UserInQueueServiceMock mock = new UserInQueueServiceMock();
            KnownUser._UserInQueueService = (mock);

            EventConfig eventConfig = new EventConfig();
            eventConfig.CookieDomain = "cookieDomain";
            eventConfig.LayoutName = "layoutName";
            eventConfig.Culture = "culture";
            eventConfig.EventId = "eventId";
            eventConfig.QueueDomain = "queueDomain";
            eventConfig.ExtendCookieValidity = true;
            eventConfig.CookieValidityMinute = 10;
            eventConfig.Version = 12;

            // Act
            KnownUser.ValidateRequestByLocalEventConfig("targetUrl", "queueitToken", eventConfig, "customerId", "secretKey");

            // Assert
            Assert.Equal("targetUrl", mock.validateRequestCalls[0][0]);
            Assert.Equal("queueitToken", mock.validateRequestCalls[0][1]);
            Assert.Equal("cookieDomain:layoutName:culture:eventId:queueDomain:true:10:12", mock.validateRequestCalls[0][2]);
            Assert.Equal("customerId", mock.validateRequestCalls[0][3]);
            Assert.Equal("secretKey", mock.validateRequestCalls[0][4]);
        }

        [Fact]
        public void ValidateRequestByIntegrationConfig_EmptyCurrentUrl_Test()
        {
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
            Assert.True(mock.validateRequestCalls.Count == 0);
            Assert.True(exceptionWasThrown);
        }

        [Fact]
        public void ValidateRequestByIntegrationConfig_EmptyIntegrationsConfig_Test()
        {
            // Arrange        
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
            Assert.True(mock.validateRequestCalls.Count == 0);
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

            CustomerIntegration customerIntegration = new CustomerIntegration();
            customerIntegration.Integrations = new IntegrationConfigModel[] { config };
            customerIntegration.Version = 3;
            var httpContextMock = new HttpContextMock() { MockRequest = new MockHttpRequest() { UserAgentValue = "googlebot", CookiesValue= new HttpCookieCollection()} };
             KnownUser._HttpContextBase = httpContextMock;
            // Act
            KnownUser.ValidateRequestByIntegrationConfig("http://test.com?event1=true", "queueitToken", customerIntegration, "customerId", "secretKey");

            // Assert
            Assert.True(mock.validateRequestCalls.Count == 1);
            Assert.Equal("http://test.com?event1=true", mock.validateRequestCalls[0][0]);
            Assert.Equal("queueitToken", mock.validateRequestCalls[0][1]);
            Assert.Equal(".test.com:Christmas Layout by Queue-it:da-DK:event1:knownusertest.queue-it.net:true:20:3", mock.validateRequestCalls[0][2]);
            Assert.Equal("customerId", mock.validateRequestCalls[0][3]);
            Assert.Equal("secretKey", mock.validateRequestCalls[0][4]);
            KnownUser._HttpContextBase = null;
        }

        [Fact]
        public void ValidateRequestByIntegrationConfig_NotMatch_Test()
        {
            // Arrange
            UserInQueueServiceMock mock = new UserInQueueServiceMock();
            KnownUser._UserInQueueService = (mock);

            CustomerIntegration customerIntegration = new CustomerIntegration();
            customerIntegration.Integrations = new IntegrationConfigModel[0];
            customerIntegration.Version = 3;

            // Act
            RequestValidationResult result = KnownUser.ValidateRequestByIntegrationConfig("http://test.com?event1=true", "queueitToken", customerIntegration, "customerId", "secretKey");

            // Assert
            Assert.True(mock.validateRequestCalls.Count == 0);
            Assert.False(result.DoRedirect);
        }

        [Theory]
        [InlineData("ForcedTargetUrl", "http://forcedtargeturl.com")]
        [InlineData("ForecedTargetUrl", "http://forcedtargeturl.com")]
        [InlineData("EventTargetUrl", "")]
        public void ValidateRequestByIntegrationConfig_RedirectLogic_Test(string redirectLogic, string forcedTargetUrl)
        {
            // Arrange
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

            CustomerIntegration customerIntegration = new CustomerIntegration();
            customerIntegration.Integrations = new IntegrationConfigModel[] { config };
            customerIntegration.Version = 3;

            // Act
            KnownUser.ValidateRequestByIntegrationConfig("http://test.com?event1=true", "queueitToken", customerIntegration, "customerId", "secretKey");

            // Assert
            Assert.True(mock.validateRequestCalls.Count == 1);
            Assert.Equal(forcedTargetUrl, mock.validateRequestCalls[0][0]);
        }
    }
}
