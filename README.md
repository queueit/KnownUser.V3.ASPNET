# Queue-it KnownUser SDK for ASP.NET
Before getting started please read the [documentation](https://github.com/queueit/Documentation/tree/main/serverside-connectors) to get acquainted with server-side connectors.

This connector supports .NET Framework 4.0+.

You can find the latest released version [here](https://github.com/queueit/KnownUser.V3.ASPNET/releases/latest) or download latest version [![NuGet](http://img.shields.io/nuget/v/QueueIT.KnownUserV3.SDK.svg)](https://www.nuget.org/packages/QueueIT.KnownUserV3.SDK/)

## Implementation
The KnownUser validation must be done on *all requests except requests for static and cached pages, resources like images, css files and ...*. 
So, if you add the KnownUser validation logic to a central place like in Global.asax, then be sure that the Triggers only fire on page requests (including ajax requests) and not on e.g. image.

This example is using the *[IntegrationConfigProvider](https://github.com/queueit/KnownUser.V3.ASPNET/blob/master/Documentation/IntegrationConfigProvider.cs)* to download the queue configuration. The provider is an example of how the download and caching of the configuration can be done. This is just an example, but if you make your own downloader, please cache the result for 5 - 10 minutes to limit number of download requests. **You should NEVER download the configuration as part of the request handling**.

The following method is all that is needed to validate that a user has been through the queue:
```CSharp
private void DoValidation()
{
    try
    {
        var customerId = "Your Queue-it customer ID";
        var secretKey = "Your 72 char secrete key as specified in Go Queue-it self-service platform";
        var apiKey = "Your api-key as specified in Go Queue-it self-service platform";

        var queueitToken = Request.QueryString[KnownUser.QueueITTokenKey];
        var currentUrlWithoutQueueITToken = Regex.Replace(Request.Url.AbsoluteUri, @"([\?&])(" + KnownUser.QueueITTokenKey + "=[^&]*)", string.Empty, RegexOptions.IgnoreCase);
        // The currentUrlWithoutQueueITToken is used to match Triggers and as the Target url (where to return the users to)
        // It is therefor important that the currentUrlWithoutQueueITToken is exactly the url of the users browsers. So if your webserver is 
        // e.g. behind a load balancer that modifies the host name or port, reformat the currentUrlWithoutQueueITToken before proceeding
        var integrationConfig = IntegrationConfigProvider.GetCachedIntegrationConfig(customerId, apiKey);
  
        //Verify if the user has been through the queue
        var validationResult = KnownUser.ValidateRequestByIntegrationConfig(currentUrlWithoutQueueITToken, queueitToken, integrationConfig, customerId, secretKey);

        if (validationResult.DoRedirect)
        {
            //Adding no cache headers to prevent browsers to cache requests
            Response.Headers.Add("Cache-Control", "no-cache, no-store, must-revalidate, max-age=0"); 
            Response.Headers.Add("Pragma", "no-cache");
            Response.Headers.Add("Expires", "Fri, 01 Jan 1990 00:00:00 GMT");
            //end
           
            if (validationResult.IsAjaxResult)
            {
                //In case of ajax call send the user to the queue by sending a custom queue-it header and redirecting user to queue from javascript
               Response.Headers.Add(validationResult.AjaxQueueRedirectHeaderKey, validationResult.AjaxRedirectUrl);
            }
            else
            {
               //Send the user to the queue - either becuase hash was missing or becuase is was invalid
               Response.Redirect(validationResult.RedirectUrl,false);
            }
            HttpContext.Current.ApplicationInstance.CompleteRequest();
        }
        else
        {
            //Request can continue - we remove queueittoken form querystring parameter to avoid sharing of user specific token
            if (HttpContext.Current.Request.Url.AbsoluteUri.Contains(KnownUser.QueueITTokenKey) && validationResult.ActionType == "Queue")
            {
                Response.Redirect(currentUrlWithoutQueueITToken, false);
                HttpContext.Current.ApplicationInstance.CompleteRequest();
            }
        }
    }
    catch (Exception ex)
    {
        // There was an error validating the request
        // Use your own logging framework to log the error
        // This was a configuration error, so we let the user continue
    }
}
```

## Implementation using inline queue configuration
Specify the configuration in code without using the Trigger/Action paradigm. In this case it is important *only to queue-up page requests* and not requests for resources. 
This can be done by 

   - Adding custom filtering logic to Global.asax 

   - **or** if using asp.net mvc by adding it as an ActionFilter on the page controllers 

   - **or** if using aspx webforms then in the Master Page's Init() method 

   - **or** with a proper filtering on the Global.asax Application_BeginRequest(). 

The following is an example of how to specify the configuration in code:
 
```CSharp
private void DoValidationByLocalEventConfig()
{
    try
    {
        var customerId = "Your Queue-it customer ID";
        var secretKey = "Your 72 char secret key as specified in Go Queue-it self-service platform";

        var queueitToken = Request.QueryString[KnownUser.QueueITTokenKey];
        var currentUrlWithoutQueueITToken = Regex.Replace(Request.Url.AbsoluteUri, @"([\?&])(" + KnownUser.QueueITTokenKey + "=[^&]*)", string.Empty, RegexOptions.IgnoreCase);

        var eventConfig = new QueueEventConfig()
        {
            EventId = "event1", //ID of the queue to use
            //CookieDomain = ".mydomain.com", //Optional - Domain name where the Queue-it session cookie should be saved.
            QueueDomain = "queue.mydomain.com", //Domain name of the queue.
            CookieValidityMinute = 15, //Validity of the Queue-it session cookie.
            ExtendCookieValidity = true, //Should the Queue-it session cookie validity time be extended each time the validation runs? 
            //Culture = "en-US", //Optional - Culture of the queue layout in the format specified here: https://msdn.microsoft.com/en-us/library/ee825488(v=cs.20).aspx. If unspecified then settings from Event will be used.
            //LayoutName = "MyCustomLayoutName" //Optional - Name of the queue layout. If unspecified then settings from Event will be used.
        };

        //Verify if the user has been through the queue
        var validationResult = KnownUser.ResolveQueueRequestByLocalConfig(currentUrlWithoutQueueITToken, queueitToken, eventConfig, customerId, secretKey);

        if (validationResult.DoRedirect)
        {
            //Adding no cache headers to prevent browsers to cache requests
            Response.Headers.Add("Cache-Control", "no-cache, no-store, must-revalidate, max-age=0"); 
            Response.Headers.Add("Pragma", "no-cache");
            Response.Headers.Add("Expires", "Fri, 01 Jan 1990 00:00:00 GMT");
            //end
            if (validationResult.IsAjaxResult)
            {
                //In case of ajax call send the user to the queue by sending a custom queue-it header and redirecting user to queue from javascript
               Response.Headers.Add(validationResult.AjaxQueueRedirectHeaderKey, validationResult.AjaxRedirectUrl);
            }
             else
            {
               //Send the user to the queue - either becuase hash was missing or becuase is was invalid
               Response.Redirect(validationResult.RedirectUrl,false);
            }
            HttpContext.Current.ApplicationInstance.CompleteRequest();
        }
        else
        {
            //Request can continue - we remove queueittoken form querystring parameter to avoid sharing of user specific token
            if (HttpContext.Current.Request.Url.AbsoluteUri.Contains(KnownUser.QueueITTokenKey) && validationResult.ActionType == "Queue")
            {
                Response.Redirect(currentUrlWithoutQueueITToken);
                HttpContext.Current.ApplicationInstance.CompleteRequest();
            }
        }
    }
    catch (System.Threading.ThreadAbortException)
    {
        //Response.Redirect will raise System.Threading.ThreadAbortException to end the exceution, no need to log the error
    }
    catch (Exception ex)
    {
        // There was an error validating the request
        // Use your own logging framework to log the error
        // This was a configuration error, so we let the user continue
    }
}
```

## Helper functions
The [QueueITHelpers.cs](https://github.com/queueit/KnownUser.V3.ASPNET/blob/master/Documentation/QueueITHelpers.cs) file includes some helper functions 
to make the reading of the `queueittoken` easier. 
