#Queue-it Security .Net Framework
The Queue-it Security Framework is used to ensure that end users cannot bypass the queue by adding a server-side integration to your server. 
## Introduction
When a user is redirected back from the queue to your website, the queue engine can attache a query string parameter (`queueittoken`) containing some information about the user. 
The most important fields of the `queueittoken` are:
q - the users unique queue identifier
ts - a timestamp of how long this redirect is valid
h - a hash of the token

The high level logic is as follows:

 1. User requests a page on your server
 2. The validation method sees that the has no Queue-it session cookie and no `queueittoken` and sends him to the correct queue based on the configuration
 3. User waits in the queue
 4. User is redirected back to your website, now with a `queueittoken`
 5. The validation method validates the `queueittoken` and creates a Queue-it session cookie
 6. The user browses to a new page and the Queue-it session cookie will let him go there without queuing again

To validate that the current user is allowed to enter your website (has been through the queue) these steps are needed:

 1. Configure the event (queue) to use the KnownUser validation
 2. Provide a queue configuration
 3. Validate the `queueittoken` and store a session cookie


### Providing the queue configuration
The recommended way is to use the Go Queue-it self-service portal to setup the configuration. This configuration can then be downloaded to your application server as shown in the IntegrationConfigProvider.cs example. The configuration will be downloaded and cached for 5 minutes. 
The configuration specifies a set of Triggers and Actions. A Trigger is an expression matching one, more or all URLs on your website. When a user enter your website and the URL matches a Trigger-expression the corresponding Action will be triggered. The Action specifies which queue the users should be send to. 
In this way you can specify which queue(s) should protect which page(s) on the fly without changing the server-side integration.

### Validate the "queueittoken" and store a session cookie
To validate that the user has been through the queue, use the `KnownUser.ValidateRequestByIntegrationConfig()` method. 
This call will validate the timestamp and hash and if valid create a "QueueITAccepted-SDFrts345E-V3_[EventId]" cookie with a TTL as specified in the configuration.
If the timestamp or hash is invalid, the user is send back to the queue.

## Implementation
The KnownUser validation must *only* be done on *page requests*. This can be done in asp.net mvc by adding it as an ActionFilter on the page controllers **or** if using aspx webforms then in the Master Page's Init() method **or** with a proper filtering on the Global.asax Application_BeginRequest(). 

This example is using the *[IntegrationConfigProvider](https://github.com/queueit/QueueIT.Security-NetFramework/blob/master/QueueIT.Security.Examples.Webforms/Advanced.aspx.cs)* to download the queue configuration. example The most simple example is just to put it on the page load request:
```
public class AdvancedController : Controller
{
	if (IntegrationConfigProvider.Instance.Exp != null)
    {
	    // Log KnownUser.LastIntegratinProviderException to queueit
        IntegrationConfigProvider.Instance.Exp = null;
        }
            try
            {
                var queueitToken = 
                HttpContext.Current.Request.QueryString[KnownUser.QueueITTokenKey];
                var pureUrl = Regex.Replace(Request.Url.ToString(),
                @"([\?&])(" + KnownUser.QueueITTokenKey + "=[^&]*)",
                string.Empty, RegexOptions.IgnoreCase);

                var validationResult = 
                KnownUser.ValidateRequestByIntegrationConfig(pureUrl, queueitToken,                 IntegrationConfigProvider.Instance.GetCachedIntegrationConfig("customerid"), 
                                        "customerId", "secretKey");

                if (validationResult.DoRedirect)
                {
                    Response.Redirect(validationResult.RedirectUrl);
                }
                else
                {
                    //Request can continue - we remove queueittoken form querystring parameter to avoid sharing of user specific token
                    if(HttpContext.Current.Request.Url.ToString().Contains(KnownUser.QueueITTokenKey))
                        Response.Redirect(pureUrl);
                }
            }
            catch (Exception ex)
            {
                //it was an error validationg the request
                //please log the error and lets uses continue 
            }
		}
	}
}
```
