#Help and examples
This folder contains some extra helper functions and examples.


##Downloading the Integration Configuration
The KnownUser library needs the Triggers and Actions to know which pages to protect and which queues to use. 
These Triggers and Actions are specified in the Go Queue-it self-service portal.

The [IntegrationConfigProvider.cs](https://github.com/queueit/KnownUser.V3.Net_beta/blob/master/Documentation/IntegrationConfigProvider.cs) file is an example of how 
the download and caching of the configuration can be done. 
*This is just an example*, but if you make your own downloader, please cache the result for 5 minutes to limit number of download requests.
![Configuration Provider flow](https://github.com/queueit/KnownUser.V3.Net_beta/blob/master/Documentation/ConfigurationProviderExample.PNG)

##queueittoken helper functions
The [QueueITHelpsers.cs](https://github.com/queueit/KnownUser.V3.Net_beta/blob/master/Documentation/QueueITHelpers.cs) file includes some helper function 
to make the reading of the token easier. 


##Alternative Implementation
If your application server (maybe due to security reasons) is not allowed to do external GET requests, then you have three options:

1. Manually download the configuration file from Queue-it Go self-service portal, save it on your application server and load it from local disk
2. Use and internal gateway server to download the configuration file and save to application server
3. Specify the configuration in code without using the Trigger/Action paradime. In this case it is important *only to queue-up page requests* and not requests for resources or AJAX calls. 
This can be done by adding custom filtering logic to Global.asax 

**or** if using asp.net mvc by adding it as an ActionFilter on the page controllers 

**or** if using aspx webforms then in the Master Page's Init() method 

**or** with a proper filtering on the Global.asax Application_BeginRequest(). 


The following is an example of how to specify the configuration in code:
 
```
private void DoValidationByLocalEventConfig()
{
    try
    {
	    var queueitToken = HttpContext.Current.Request.QueryString[KnownUser.QueueITTokenKey];
	    var pureUrl = Regex.Replace(Request.Url.ToString(), @"([\?&])(" + KnownUser.QueueITTokenKey + "=[^&]*)", string.Empty, RegexOptions.IgnoreCase);
	    var eventConfig = new EventConfig()
        {
	        EventId = "event1", //ID of the queue to use
            CookieDomain = ".mydomain.com", //Domain name where the Queue-it session cookie should be saved
            QueueDomain = "myqueue.com", //Domian name of the queue - usually in the format [CustomerId].queue-it.net
            CookieValidityMinute = 15, //Validity of the Queue-it session cookie 
            ExtendCookieValidity = false, //Should the Queue-it session cookie validity time be extended each time the validation is run
            Culture = "en-US", //Culture of the queue ticket layout in the format specified here: https://msdn.microsoft.com/en-us/library/ee825488(v=cs.20).aspx
            LayoutName = "MyCustomLayoutName" //Name of the queue ticket layout - e.g. "Default layout by Queue-it"
       };
       var validationResult = KnownUser.ValidateRequestByLocalEventConfig(pureUrl, queueitToken, eventConfig, "customerId","secretKey");
       if (validationResult.DoRedirect)
	       {
	           Response.Redirect(validationResult.RedirectUrl);
           }
      else
           {
	           //Request can continue if you want to remove queueittoken form querystring parameter uncomment below codes
	           if(HttpContext.Current.Request.Url.ToString().Contains(KnownUser.QueueITTokenKey))
                    Response.Redirect(pureUrl);
           }
     }
     catch (Exception ex)
     {
         //There was an error validationg the request
        //Please log the error and let uses continue 
    }
}
```
