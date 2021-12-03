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
                Response.AddHeader("Access-Control-Expose-Headers", validationResult.AjaxQueueRedirectHeaderKey);
            }
            else
            {
               //Send the user to the queue - either becuase hash was missing or becuase is was invalid
               Response.Redirect(validationResult.RedirectUrl, false);
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

        var eventConfig = new QueueEventConfig
        {
            EventId = "event1", // ID of the queue to use
            CookieDomain = ".mydomain.com", // Optional - Domain name where the Queue-it session cookie should be saved. Default is to save on the domain of the request
            QueueDomain = "queue.mydomain.com", // Optional - Domian name of the queue. Default is [CustomerId].queue-it.net
            CookieValidityMinute = 15, // Optional - Validity of the Queue-it session cookie. Default is 10 minutes
            ExtendCookieValidity = false, // Optional - Should the Queue-it session cookie validity time be extended each time the validation runs? Default is true.
            Culture = "en-US", // Optional - Culture of the queue ticket layout in the format specified here: https://msdn.microsoft.com/en-us/library/ee825488(v=cs.20).aspx Default is to use what is specified on Event
            LayoutName = "MyCustomLayoutName" // Optional - Name of the queue ticket layout - e.g. "Default layout by Queue-it". Default is to use what is specified on the Event
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
                Response.AddHeader("Access-Control-Expose-Headers", validationResult.AjaxQueueRedirectHeaderKey);
            }
             else
            {
               //Send the user to the queue - either becuase hash was missing or becuase is was invalid
               Response.Redirect(validationResult.RedirectUrl, false);
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

## Advanced Features
### Request body trigger

The connector supports triggering on request body content. An example could be a POST call with specific item ID where you want end-users to queue up for.
For this to work, you will need to contact Queue-it support or enable request body triggers in your integration settings in your GO Queue-it platform account.
Once enabled you will need to update your integration so request body is available for the connector.  
You need to create a custom HttpRequest similar to this one:

```CSharp
public class CustomHttpRequest : QueueIT.KnownUserV3.SDK.HttpRequest
{
    public string RequestBody { get; set; }

    public override string GetRequestBodyAsString()
    {
        return RequestBody ?? base.GetRequestBodyAsString();
    }
}
```

Then, on each request, before calling the `DoValidation()` method, you should initialize the SDK with your custom HttpRequest implementation:

```CSharp
var customHttpRequest = new CustomHttpRequest
{
    RequestBody = GetBody()
};
SDKInitializer.SetHttpRequest(customHttpRequest);
```

The `GetBody()` function could be implemented like below. Make sure to set the `maxBytesToRead` to something appropriate for your needs.

```CSharp
/*
 * Example of how the request body can be read and rewinded
 */
private string GetBody()
{
    // Limit the number of bytes needed to read, from the body, to avoid reading large requests
    var maxBytesToRead = 1024 * 50;
    var resultBuffer = new byte[maxBytesToRead];

    // Rewind the stream in case it has already been consumed
    HttpContext.Current.Request.InputStream.Seek(0, SeekOrigin.Begin);
    var actualBytesRead = HttpContext.Current.Request.InputStream.Read(resultBuffer, 0, maxBytesToRead);

    // Rewind the stream to allow next consumer to consume it
    HttpContext.Current.Request.InputStream.Seek(0, SeekOrigin.Begin);

    resultBuffer = resultBuffer.Take(actualBytesRead).ToArray();

    return new string(HttpContext.Current.Request.ContentEncoding.GetChars(resultBuffer));
}
```

### Ignore specific HTTP verbs
You can ignore specific HTTP methods, by checking the request method, before calling `DoValidation()`. If you are using CORS, it might be best to ignore `OPTIONS` requests, since they are used by CORS to retrieve your server's configuration.
You can ignore `OPTIONS` requests using the following method:

```CSharp
private bool IsIgnored()
{
    return string.Equals(Request.HttpMethod, "options", StringComparison.OrdinalIgnoreCase);
}
```