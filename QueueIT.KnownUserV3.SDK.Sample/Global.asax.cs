using QueueIT.KnownUserV3.SDK;
using QueueIT.KnownUserV3.SDK.IntegrationConfigLoader;
using System;
using System.Text.RegularExpressions;
using System.Web;

namespace QueueIT.KnownUser3.SDK.Sample
{
    public class Global : System.Web.HttpApplication
    {
        bool isQueueItEnabled = true; // move this flag to your config file or database for easy enabling / disabling of Queue-it integration

        protected void Application_BeginRequest(object sender, EventArgs e)
        {
            // All page requests are validated against the KnownUser library to ensure only users that have been through the queue are allowed in
            // This example is using the IntegrationConfigProvider example to download and cache the integration configuration from Queue-it
            // The downloaded integration configuration will make sure that only configured pages are protected
            if (isQueueItEnabled)
                DoValidation();  //Default Queue-it integration

            // This shows an alternative implementation
            // This example is manually specifiying the configuartion to use, using the EventConfig() class
            // Here you also manually need to ensure that only the relevant page requests are validated
            // Also ensure that only page requests (and not e.g. image requests) are validated
            //if (isQueueItEnabled)
            //    DoValidationByLocalEventConfig(); //Example of alternative implementation using local event configuration
        }

        private void DoValidation()
        {
            try
            {
                var customerId = "Your Queue-it customer ID";
                var secretKey = "Your 72 char secrete key as specified in Go Queue-it self-service platform";

                var queueitToken = Request.QueryString[KnownUser.QueueITTokenKey];
                var pureUrl = Regex.Replace(Request.Url.ToString(), @"([\?&])(" + KnownUser.QueueITTokenKey + "=[^&]*)", string.Empty, RegexOptions.IgnoreCase);
                var integrationConfig = IntegrationConfigProvider.GetCachedIntegrationConfig(customerId);


                //Verify if the user has been through the queue
                var validationResult = KnownUser.ValidateRequestByIntegrationConfig(pureUrl, queueitToken, integrationConfig, customerId, secretKey);

                if (validationResult.DoRedirect)
                {
                    //Send the user to the queue - either becuase hash was missing or becuase is was invalid
                    Response.Redirect(validationResult.RedirectUrl);
                }
                else
                {
                    //Request can continue - we remove queueittoken form querystring parameter to avoid sharing of user specific token
                    if (HttpContext.Current.Request.Url.ToString().Contains(KnownUser.QueueITTokenKey))
                        Response.Redirect(pureUrl);

                }
            }
            catch (System.Threading.ThreadAbortException)
            {
                //Response.Redirect will raise System.Threading.ThreadAbortException to end the exceution, no need to log the error
            }
            catch (Exception ex)
            {
                //There was an error validationg the request
                //Use your own logging framework to log the Exception
                //This was a configuration exception, so we let the user continue
            }
        }

        private void DoValidationByLocalEventConfig()
        {
            try
            {
                var customerId = "Your Queue-it customer ID";
                var secretKey = "Your 72 char secrete key as specified in Go Queue-it self-service platform";

                var queueitToken = Request.QueryString[KnownUser.QueueITTokenKey];
                var pureUrl = Regex.Replace(Request.Url.ToString(), @"([\?&])(" + KnownUser.QueueITTokenKey + "=[^&]*)", string.Empty, RegexOptions.IgnoreCase);
                var eventConfig = new EventConfig()
                {
                    EventId = "event1", //ID of the queue to use
                    CookieDomain = ".mydomain.com", //Optional - Domain name where the Queue-it session cookie should be saved. Default is to save on the domain of the request
                    QueueDomain = "queue.mydomain.com", //Optional - Domian name of the queue. Default is [CustomerId].queue-it.net
                    CookieValidityMinute = 15, //Optional - Validity of the Queue-it session cookie. Default is 10 minutes
                    ExtendCookieValidity = false, //Optional - Should the Queue-it session cookie validity time be extended each time the validation runs? Default is true.
                    Culture = "en-US", //Optional - Culture of the queue ticket layout in the format specified here: https://msdn.microsoft.com/en-us/library/ee825488(v=cs.20).aspx Default is to use what is specified on Event
                    LayoutName = "MyCustomLayoutName" //Optional - Name of the queue ticket layout - e.g. "Default layout by Queue-it". Default is to use what is specified on the Event
                };

                //Verify if the user has been through the queue
                var validationResult = KnownUser.ValidateRequestByLocalEventConfig(pureUrl, queueitToken, eventConfig, customerId, secretKey);

                if (validationResult.DoRedirect)
                {
                    //Send the user to the queue - either becuase hash was missing or becuase is was invalid
                    Response.Redirect(validationResult.RedirectUrl);
                }
                else
                {
                    //Request can continue - we remove queueittoken form querystring parameter to avoid sharing of user specific token
                    if (HttpContext.Current.Request.Url.ToString().Contains(KnownUser.QueueITTokenKey))
                        Response.Redirect(pureUrl);
                }
            }
            catch (System.Threading.ThreadAbortException)
            {
                //Response.Redirect will raise System.Threading.ThreadAbortException to end the exceution, no need to log the error
            }
            catch (Exception ex)
            {
                //There was an error validationg the request
                //Please log the error and let user continue 
            }
        }
    }
}