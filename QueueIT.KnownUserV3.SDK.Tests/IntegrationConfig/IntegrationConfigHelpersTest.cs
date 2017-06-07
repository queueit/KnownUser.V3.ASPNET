using QueueIT.KnownUserV3.SDK.IntegrationConfig;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino;
using Rhino.Mocks;
using System.Web;
using Xunit;

namespace QueueIT.KnownUserV3.SDK.Tests.IntegrationConfig
{
    public class ComparisonOperatorHelperTest
    {
        [Fact]
        public void Evaluate_Equals()
        {
            Assert.True(ComparisonOperatorHelper.Evaluate(ComparisonOperatorType.EqualS, false, false, "test1", "test1"));
            Assert.False(ComparisonOperatorHelper.Evaluate(ComparisonOperatorType.EqualS, false, false, "test1", "Test1"));
            Assert.True(ComparisonOperatorHelper.Evaluate(ComparisonOperatorType.EqualS, false, true, "test1", "Test1"));
            Assert.True(ComparisonOperatorHelper.Evaluate(ComparisonOperatorType.EqualS, true, false, "test1", "Test1"));
            Assert.False(ComparisonOperatorHelper.Evaluate(ComparisonOperatorType.EqualS, true, false, "test1", "test1"));
            Assert.False(ComparisonOperatorHelper.Evaluate(ComparisonOperatorType.EqualS, true, true, "test1", "Test1"));
        }

        [Fact]
        public void Evaluate_Contains()
        {
            Assert.True(ComparisonOperatorHelper.Evaluate(ComparisonOperatorType.Contains, false, false, "test_test1_test", "test1"));
            Assert.False(ComparisonOperatorHelper.Evaluate(ComparisonOperatorType.Contains, false, false, "test_test1_test", "Test1"));
            Assert.True(ComparisonOperatorHelper.Evaluate(ComparisonOperatorType.Contains, false, true, "test_test1_test", "Test1"));
            Assert.True(ComparisonOperatorHelper.Evaluate(ComparisonOperatorType.Contains, true, false, "test_test1_test", "Test1"));
            Assert.False(ComparisonOperatorHelper.Evaluate(ComparisonOperatorType.Contains, true, true, "test_test1", "Test1"));
            Assert.False(ComparisonOperatorHelper.Evaluate(ComparisonOperatorType.Contains, true, false, "test_test1", "test1"));
            Assert.True(ComparisonOperatorHelper.Evaluate(ComparisonOperatorType.Contains, false, false, "test_dsdsdsdtest1", "*"));
        }

        [Fact]
        public void Evaluate_StartsWith()
        {
            Assert.True(ComparisonOperatorHelper.Evaluate(ComparisonOperatorType.StartsWith, false, false, "test1_test1_test", "test1"));
            Assert.False(ComparisonOperatorHelper.Evaluate(ComparisonOperatorType.StartsWith, false, false, "test1_test1_test", "Test1"));
            Assert.True(ComparisonOperatorHelper.Evaluate(ComparisonOperatorType.StartsWith, false, true, "test1_test1_test", "Test1"));
            Assert.False(ComparisonOperatorHelper.Evaluate(ComparisonOperatorType.StartsWith, true, true, "test1_test1_test", "Test1"));
        }


        [Fact]
        public void Evaluate_EndsWith()
        {
            Assert.True(ComparisonOperatorHelper.Evaluate(ComparisonOperatorType.EndsWith, false, false, "test1_test1_testshop", "shop"));
            Assert.False(ComparisonOperatorHelper.Evaluate(ComparisonOperatorType.EndsWith, false, false, "test1_test1_testshop2", "shop"));
            Assert.True(ComparisonOperatorHelper.Evaluate(ComparisonOperatorType.EndsWith, false, true, "test1_test1_testshop", "Shop"));
            Assert.False(ComparisonOperatorHelper.Evaluate(ComparisonOperatorType.EndsWith, true, true, "test1_test1_testshop", "Shop"));
        }

        [Fact]
        public void Evaluate_MatchesWith()
        {
            Assert.True(ComparisonOperatorHelper.Evaluate(ComparisonOperatorType.MatchesWith, false, false, "test1_test1_testshop", ".*shop.*"));
            Assert.False(ComparisonOperatorHelper.Evaluate(ComparisonOperatorType.MatchesWith, false, false, "test1_test1_testshop2", ".*Shop.*"));
            Assert.True(ComparisonOperatorHelper.Evaluate(ComparisonOperatorType.MatchesWith, false, true, "test1_test1_testshop", ".*Shop.*"));
            Assert.False(ComparisonOperatorHelper.Evaluate(ComparisonOperatorType.MatchesWith, true, true, "test1_test1_testshop", ".*Shop.*"));
        }
    }
    public class CookieValidatorHelperTest
    {
        [Fact]
        public void Evaluate_Test()
        {
            var triggerPart = new TriggerPart()
            {
                CookieName = "c1",
                Operator = ComparisonOperatorType.Contains,
                ValueToCompare = "1"
            };
            var cookieCollection = new System.Web.HttpCookieCollection() { };
            Assert.False(CookieValidatorHelper.Evaluate(triggerPart, cookieCollection));

            cookieCollection.Add(new System.Web.HttpCookie("c5", "5"));
            cookieCollection.Add(new System.Web.HttpCookie("c1", "1"));
            cookieCollection.Add(new System.Web.HttpCookie("c2", "test"));
            Assert.True(CookieValidatorHelper.Evaluate(triggerPart, cookieCollection));

            triggerPart.ValueToCompare = "5";
            Assert.False(CookieValidatorHelper.Evaluate(triggerPart, cookieCollection));


            triggerPart.ValueToCompare = "Test";
            triggerPart.IsIgnoreCase = true;
            triggerPart.CookieName = "c2";
            Assert.True(CookieValidatorHelper.Evaluate(triggerPart, cookieCollection));

            triggerPart.ValueToCompare = "Test";
            triggerPart.IsIgnoreCase = true;
            triggerPart.IsNegative = true;
            triggerPart.CookieName = "c2";
            Assert.False(CookieValidatorHelper.Evaluate(triggerPart, cookieCollection));
        }
    }

    public class UrlValidatorHelperTest
    {
        [Fact]
        public void Evaluate_Test()
        {
            var triggerPart = new TriggerPart()
            {
                UrlPart = UrlPartType.PageUrl,
                Operator = ComparisonOperatorType.Contains,
                ValueToCompare = "http://test.tesdomain.com:8080/test?q=1"
            };
            Assert.False(UrlValidatorHelper.Evaluate(triggerPart, "http://test.tesdomain.com:8080/test?q=2"));

            triggerPart.ValueToCompare = "/Test/t1";
            triggerPart.UrlPart = UrlPartType.PagePath;
            triggerPart.Operator = ComparisonOperatorType.EqualS;
            triggerPart.IsIgnoreCase = true;
            Assert.True(UrlValidatorHelper.Evaluate(triggerPart, "http://test.tesdomain.com:8080/test/t1?q=2&y02"));


            triggerPart.UrlPart = UrlPartType.HostName;
            triggerPart.ValueToCompare = "test.tesdomain.com";
            triggerPart.Operator = ComparisonOperatorType.Contains;
            Assert.True(UrlValidatorHelper.Evaluate(triggerPart, "http://m.test.tesdomain.com:8080/test?q=2"));


            triggerPart.UrlPart = UrlPartType.HostName;
            triggerPart.ValueToCompare = "test.tesdomain.com";
            triggerPart.IsNegative = true;
            triggerPart.Operator = ComparisonOperatorType.Contains;
            Assert.False(UrlValidatorHelper.Evaluate(triggerPart, "http://m.test.tesdomain.com:8080/test?q=2"));

        }
    }



    public class IntegrationEvaluatorTest
    {
        [Fact]
        public void GetMatchedIntegrationConfig_OneTrigger_And_NotMatched()
        {
            var testObject = new IntegrationEvaluator();

            var customerIntegration = new CustomerIntegration()
            {
                Integrations = new List<IntegrationConfigModel> {
                     new IntegrationConfigModel()
                     {

                         Triggers = new List<TriggerModel>() {
                                                new TriggerModel() {
                                                    LogicalOperator = LogicalOperatorType.And,
                                                    TriggerParts = new List<TriggerPart>() {
                                                            new TriggerPart() {
                                                                CookieName ="c1",
                                                                Operator = ComparisonOperatorType.EqualS,
                                                                ValueToCompare ="value1",
                                                                ValidatorType= ValidatorType.CookieValidator
                                                            },
                                                            new TriggerPart() {
                                                                UrlPart = UrlPartType.PageUrl,
                                                                ValidatorType= ValidatorType.UrlValidator,
                                                                ValueToCompare= "test",
                                                                Operator= ComparisonOperatorType.Contains
                                                                }
                                                        }
                                                    }
                    }
                }
              }
            };

            var url = new Uri("http://test.tesdomain.com:8080/test?q=2");

            Assert.True(testObject.GetMatchedIntegrationConfig(customerIntegration, url.AbsoluteUri, new HttpCookieCollection()) == null);
        }
        [Fact]
        public void GetMatchedIntegrationConfig_OneTrigger_And_Matched()
        {
            var testObject = new IntegrationEvaluator();

            var customerIntegration = new CustomerIntegration()
            {

                Integrations = new List<IntegrationConfigModel> {
                                             new IntegrationConfigModel()
                                             {
                                                 Name= "integration1",
                                                 Triggers = new List<TriggerModel>() {
                                                            new TriggerModel() {
                                                                LogicalOperator = LogicalOperatorType.And,
                                                                TriggerParts = new List<TriggerPart>() {
                                                                    new TriggerPart() {
                                                                        CookieName ="c1",
                                                                        Operator = ComparisonOperatorType.EqualS,
                                                                        IsIgnoreCase= true,
                                                                        ValueToCompare ="value1",
                                                                        ValidatorType= ValidatorType.CookieValidator
                                                                    },
                                                                    new TriggerPart() {
                                                                        UrlPart = UrlPartType.PageUrl,
                                                                        ValidatorType= ValidatorType.UrlValidator,
                                                                        ValueToCompare= "test",
                                                                        Operator= ComparisonOperatorType.Contains
                                                                        }

                                                                }
                                                    }
                                              }
                                            }

            }
            };


            var url = new Uri("http://test.tesdomain.com:8080/test?q=2");
       

            Assert.True(testObject.GetMatchedIntegrationConfig(customerIntegration, url.AbsoluteUri,
                new HttpCookieCollection() { new HttpCookie("c1", "Value1") }).Name == "integration1");
        }
        [Fact]
        public void GetMatchedIntegrationConfig_OneTrigger_Or_NotMatched()
        {
            var testObject = new IntegrationEvaluator();
            var customerIntegration = new CustomerIntegration()
            {

                Integrations = new List<IntegrationConfigModel> {
                                             new IntegrationConfigModel()
                                             {
                                                 Name= "integration1",
                                                 Triggers = new List<TriggerModel>() {
                                                                 new TriggerModel() {
                                                                    LogicalOperator = LogicalOperatorType.Or,
                                                                    TriggerParts = new List<TriggerPart>() {
                                                                        new TriggerPart() {
                                                                            CookieName ="c1",
                                                                            Operator = ComparisonOperatorType.EqualS,
                                                                            ValueToCompare ="value1",
                                                                            ValidatorType= ValidatorType.CookieValidator
                                                                        },
                                                                        new TriggerPart() {
                                                                            UrlPart = UrlPartType.PageUrl,
                                                                            ValidatorType= ValidatorType.UrlValidator,
                                                                             IsIgnoreCase= true,
                                                                            IsNegative= true,
                                                                            ValueToCompare= "tesT",
                                                                            Operator= ComparisonOperatorType.Contains
                                                                            }

                                                                    }
                                                                }
                                                    }
                                              }
                                            }

            };


            var url = new Uri("http://test.tesdomain.com:8080/test?q=2");
 


            Assert.True(testObject.GetMatchedIntegrationConfig(customerIntegration, url.AbsoluteUri,
                new HttpCookieCollection() { new HttpCookie("c2", "value1") }) == null);
        }
        [Fact]
        public void GetMatchedIntegrationConfig_OneTrigger_Or_Matched()
        {
            var testObject = new IntegrationEvaluator();
            var customerIntegration = new CustomerIntegration()
            {

                Integrations = new List<IntegrationConfigModel> {
                                             new IntegrationConfigModel()
                                             {
                                                 Name= "integration1",
                                                 Triggers = new List<TriggerModel>() {
                                                           new TriggerModel() {
                                                                LogicalOperator = LogicalOperatorType.Or,
                                                                TriggerParts = new List<TriggerPart>() {
                                                                    new TriggerPart() {
                                                                        CookieName ="c1",
                                                                        Operator = ComparisonOperatorType.EqualS,
                                                                        ValueToCompare ="value1",
                                                                        ValidatorType= ValidatorType.CookieValidator
                                                                    },
                                                                    new TriggerPart() {
                                                                        UrlPart = UrlPartType.PageUrl,
                                                                        ValidatorType= ValidatorType.UrlValidator,
                                                                        ValueToCompare= "tesT",
                                                                        Operator= ComparisonOperatorType.Contains
                                                                        }

                                                                }
                                                        }
                                                    }
                                              }
                                            }

            };


            var url = new Uri("http://test.tesdomain.com:8080/test?q=2");
            var httpRequestMock = MockRepository.GenerateMock<HttpRequestBase>();


            Assert.True(testObject.GetMatchedIntegrationConfig(customerIntegration, url.AbsoluteUri, 
                new HttpCookieCollection() { new HttpCookie("c1", "value1") }).Name == "integration1");
        }

        [Fact]
        public void GetMatchedIntegrationConfig_TwoTriggers_Matched()
        {
            var testObject = new IntegrationEvaluator();
            var customerIntegration = new CustomerIntegration()
            {

                Integrations = new List<IntegrationConfigModel> {
                                             new IntegrationConfigModel()
                                             {
                                                 Name= "integration1",
                                                 Triggers = new List<TriggerModel>() {
                                                        new TriggerModel() {
                                                        LogicalOperator = LogicalOperatorType.And,
                                                        TriggerParts = new List<TriggerPart>() {
                                                            new TriggerPart() {
                                                                CookieName ="c1",
                                                                Operator = ComparisonOperatorType.EqualS,
                                                                ValueToCompare ="value1",
                                                                ValidatorType= ValidatorType.CookieValidator
                                                            }


                                                        }
                                                    },
                                                        new TriggerModel()
                                                        {
                                                            LogicalOperator = LogicalOperatorType.And,
                                                            TriggerParts = new List<TriggerPart>()
                                                            {
                                                              new TriggerPart() {
                                                                    UrlPart = UrlPartType.PageUrl,
                                                                    ValidatorType= ValidatorType.UrlValidator,
                                                                    ValueToCompare= "*",
                                                                    Operator= ComparisonOperatorType.Contains
                                                                    }
                                                            }
                                                         }
                                                  }
                                              }
                                     }

            };

            
            var url = new Uri("http://test.tesdomain.com:8080/test?q=2");


            Assert.True(testObject.GetMatchedIntegrationConfig(customerIntegration, url.AbsoluteUri,
                new HttpCookieCollection() { }).Name=="integration1");
        }

        [Fact]
        public void GetMatchedIntegrationConfig_TwoTriggers_NotMatched()
        {
            var testObject = new IntegrationEvaluator();
            var customerIntegration = new CustomerIntegration()
            {

                Integrations = new List<IntegrationConfigModel> {
                                             new IntegrationConfigModel()
                                             {
                                                 Name= "integration1",
                                                 Triggers = new List<TriggerModel>() {
                                                        new TriggerModel() {
                                                            LogicalOperator = LogicalOperatorType.And,
                                                            TriggerParts = new List<TriggerPart>() {
                                                                new TriggerPart() {
                                                                    CookieName ="c1",
                                                                    Operator = ComparisonOperatorType.EqualS,
                                                                    ValueToCompare ="value1",
                                                                    ValidatorType= ValidatorType.CookieValidator
                                                                }


                                                            }
                                                        },
                                                        new TriggerModel()
                                                        {
                                                            LogicalOperator = LogicalOperatorType.And,
                                                            TriggerParts = new List<TriggerPart>()
                                                            {
                                                                 new TriggerPart() {
                                                                    UrlPart = UrlPartType.PageUrl,
                                                                    ValidatorType= ValidatorType.UrlValidator,
                                                                    ValueToCompare= "tesT",
                                                                    Operator= ComparisonOperatorType.Contains
                                                                    }
                                                            }
                                                        }
                                                  }
                                              }
                                     }

            };

            var url = new Uri("http://test.tesdomain.com:8080/test?q=2");


            Assert.True(testObject.GetMatchedIntegrationConfig(customerIntegration, url.AbsoluteUri, new HttpCookieCollection() { }) ==null);
        }

        [Fact]
        public void GetMatchedIntegrationConfig_ThreeIntegrationsInOrder_SecondMatched()
        {
            var testObject = new IntegrationEvaluator();
            var customerIntegration = new CustomerIntegration()
            {

                Integrations = new List<IntegrationConfigModel> {
                                             new IntegrationConfigModel()
                                             {
                                                 Name= "integration0",
                                                 Triggers = new List<TriggerModel>() {
                                                        new TriggerModel() {
                                                            LogicalOperator = LogicalOperatorType.And,
                                                            TriggerParts = new List<TriggerPart>() {
                                                                new TriggerPart() {
                                                                    CookieName ="c1",
                                                                    Operator = ComparisonOperatorType.EqualS,
                                                                    ValueToCompare ="value1",
                                                                    ValidatorType= ValidatorType.CookieValidator
                                                                }


                                                            }
                                                        }
                                                  }
                                              },
                                               new IntegrationConfigModel()
                                             {
                                                 Name= "integration1",
                                                 Triggers = new List<TriggerModel>() {
                                                        new TriggerModel() {
                                                            LogicalOperator = LogicalOperatorType.And,
                                                            TriggerParts = new List<TriggerPart>() {
                                                                new TriggerPart() {
                                                                    CookieName ="c1",
                                                                    Operator = ComparisonOperatorType.EqualS,
                                                                    ValueToCompare ="Value1",
                                                                    ValidatorType= ValidatorType.CookieValidator
                                                                }


                                                            }
                                                        }
                                                  }
                                              },
                                              new IntegrationConfigModel()
                                             {
                                                 Name= "integration2",
                                                 Triggers = new List<TriggerModel>() {
                                                        new TriggerModel() {
                                                            LogicalOperator = LogicalOperatorType.And,
                                                            TriggerParts = new List<TriggerPart>() {
                                                                new TriggerPart() {
                                                                    UrlPart= UrlPartType.PageUrl,
                                                                    Operator = ComparisonOperatorType.Contains,

                                                                    ValueToCompare ="test",
                                                                    ValidatorType= ValidatorType.UrlValidator
                                                                }


                                                            }
                                                        }
                                                  }
                                              }
                                     }

            };

            var url = new Uri("http://test.tesdomain.com:8080/test?q=2");
            var httpRequestMock = MockRepository.GenerateMock<HttpRequestBase>();

            Assert.False(testObject.GetMatchedIntegrationConfig(customerIntegration, url.AbsoluteUri, 
                new HttpCookieCollection() { new HttpCookie("c1") { Value = "Value1" } }).Name=="integration2");
        }
    }
}

