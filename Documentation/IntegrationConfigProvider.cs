using QueueIT.KnownUserV3.SDK.IntegrationConfig;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Timers;
using System.Web.Script.Serialization;

namespace QueueIT.KnownUserV3.SDK.IntegrationConfigLoader
{


    internal class IntegrationConfigProvider
    {
        private static readonly Lazy<IntegrationConfigProvider> _instance = new Lazy<IntegrationConfigProvider>(
            () => new IntegrationConfigProvider());

        private int _downloadTimeoutMS = 4000;
        private Timer _timer;
        private readonly object _lockObject = new object();
        CustomerIntegration _cachedIntegrationConfig;
        private bool _isInitialized = false;
        public CustomerIntegration GetCachedIntegrationConfig(string customerId)
        {
            if(!this._isInitialized)
            {
                this.CustomerId = customerId;
                lock (_lockObject)
                {
                    if (!this._isInitialized)
                    {
                        this.RefreshCache(true);
                        if (this.Exp != null)
                            throw this.Exp;
                        _timer = new Timer();
                        _timer.Interval = _RefreshIntervalS * 1000;
                        _timer.AutoReset = false;
                        _timer.Elapsed += TimerElapsed;
                        _timer.Start();
                        this._isInitialized = true;
                    }
                }
            }
            return this._cachedIntegrationConfig;
        }

        internal static int _RefreshIntervalS = 5 * 60;
        internal static double _RetryExceptionSleepS = 5;
        public Exception Exp { get; set; }
        private string CustomerId { get; set; }

        private IntegrationConfigProvider()
        {
        }
        public static IntegrationConfigProvider Instance
        {
            get
            {
                return _instance.Value;
            }
        }


        public void Init(string customerId)
        {
            this.CustomerId = customerId;
            lock (_lockObject)
            {
                if (_timer == null)
                {
                    this.RefreshCache(true);
                    if (this.Exp != null)
                        throw this.Exp;
                    _timer = new Timer();
                    _timer.Interval = _RefreshIntervalS * 1000;
                    _timer.AutoReset = false;
                    _timer.Elapsed += TimerElapsed;
                    _timer.Start();
                }
            }
        }



        private void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            this.RefreshCache(false);
            _timer.Start();
        }

        private void RefreshCache(bool init)
        {
            int tryCount = 0;
            while (tryCount < 5)
            {
                var configUrl = $"https://assets.queue-it.net/{this.CustomerId}/integrationconfig/json/integrationInfo.json";
                try
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(configUrl);
                    request.Timeout = _downloadTimeoutMS;
                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    {
                        if (response.StatusCode != HttpStatusCode.OK)
                            throw new Exception($"It was not sucessful retriving config file status code {response.StatusCode} from {configUrl}");
                        using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                        {
                            //this.CachedIntegrationConfig = reader.ReadToEnd();
                            JavaScriptSerializer deserializer = new JavaScriptSerializer();
                            var deserialized = deserializer.Deserialize<CustomerIntegration>(reader.ReadToEnd());
                            if (deserialized == null)
                                throw new Exception("CustomerIntegration is null");
                             _cachedIntegrationConfig = deserialized;


                        }
                        this.Exp = null;
                        return;
                    }
                }
                catch (Exception ex)
                {
                    ++tryCount;
                    if (tryCount >= 5)
                    {
                        this.Exp = new Exception($"Error in loading config file at {DateTime.UtcNow.ToString("o")}", ex);
                        break;
                    }
                    if (!init)
                        System.Threading.Thread.Sleep(TimeSpan.FromSeconds(_RetryExceptionSleepS));
                    else
                        System.Threading.Thread.Sleep(TimeSpan.FromSeconds(0.200 * tryCount));

                }
            }
        }


    }
}
