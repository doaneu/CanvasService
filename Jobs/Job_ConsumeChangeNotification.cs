using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EthosClient;
using Newtonsoft.Json;
using Hangfire;
using CanvasService.JobRouters;
using System.Collections.Specialized;

namespace CanvasService.Jobs
{
    public class Job_ConsumeChangeNotification
    {
        private readonly IConfiguration Configuration;
        private readonly IWebHostEnvironment WebHostEnvironment;
        private Dictionary<string,string> _appSettings;

        public Job_ConsumeChangeNotification(IWebHostEnvironment env, IConfiguration configuration)
        {

            _appSettings = new Dictionary<string, string>();


            if (env.IsDevelopment())
            {
                Configuration = configuration;
                WebHostEnvironment = env;
                _appSettings.Add("ETHOS-API-KEY", Configuration["ETHOS-API-KEY"]);
                _appSettings.Add("CANVAS-API-KEY", Configuration["CANVAS-API-KEY"]);
                _appSettings.Add("CANVAS-API", Configuration["CANVAS-API"]);
                _appSettings.Add("USER-SIS-USER-ID", Configuration["USER-SIS-USER-ID"]);
                _appSettings.Add("USER-LOGIN-ID", Configuration["USER-LOGIN-ID"]);
                _appSettings.Add("USER-EMAIL", Configuration["USER-EMAIL"]);
                _appSettings.Add("USER-AUTH-PROVIDER", Configuration["USER-AUTH-PROVIDER"]);
                _appSettings.Add("DEFAULT-ACCOUNT", Configuration["DEFAULT-ACCOUNT"]);
                _appSettings.Add("TERM-SIS-TERM-ID", Configuration["TERM-SIS-TERM-ID"]);
                _appSettings.Add("TERM-SIS-TERM-NAME", Configuration["TERM-SIS-TERM-NAME"]);
            }
            else
            {
                _appSettings.Add("ETHOS-API-KEY", Environment.GetEnvironmentVariable("ETHOS-API-KEY"));
                _appSettings.Add("CANVAS-API-KEY", Environment.GetEnvironmentVariable("CANVAS-API-KEY"));
                _appSettings.Add("CANVAS-API", Environment.GetEnvironmentVariable("CANVAS-API"));
                _appSettings.Add("USER-SIS-USER-ID", Environment.GetEnvironmentVariable("USER-SIS-USER-ID"));
                _appSettings.Add("USER-LOGIN-ID", Environment.GetEnvironmentVariable("USER-LOGIN-ID"));
                _appSettings.Add("USER-EMAIL", Environment.GetEnvironmentVariable("USER-EMAIL"));
                _appSettings.Add("USER-AUTH-PROVIDER", Environment.GetEnvironmentVariable("USER-AUTH-PROVIDER"));
                _appSettings.Add("DEFAULT-ACCOUNT", Environment.GetEnvironmentVariable("DEFAULT-ACCOUNT"));
                _appSettings.Add("TERM-SIS-TERM-ID", Environment.GetEnvironmentVariable("TERM-SIS-TERM-ID"));
                _appSettings.Add("TERM-SIS-TERM-NAME", Environment.GetEnvironmentVariable("TERM-SIS-TERM-NAME"));
            }
        
        }

        [Queue("consume")]
        public void Start()
        {

            EthosHttpClient ethosHttpClient = new EthosHttpClient();
            ethosHttpClient.ApiKey = _appSettings["ETHOS-API-KEY"];
            ethosHttpClient.BaseAddress = new Uri(EthosConstants.EthosBaseUrl);
            ethosHttpClient.MaxMessagesToConsume = 10;

            List<ChangeNotificationV2> changeNotifications = ethosHttpClient.ConsumeChangeNotifications().ToList();

            List<string> items = new List<string>();

            foreach(ChangeNotificationV2 changeNotification in changeNotifications)
            {
                
                string resourceName = changeNotification.Resource.Name;

                switch (resourceName)
                {

                    case "persons":
                        BackgroundJob.Enqueue(() => new JobRouter_persons().Start(_appSettings, changeNotification));
                        break;
                    /*case "academic-periods":
                        BackgroundJob.Enqueue(() => new JobRouter_academic_periods().Start(_appSettings, changeNotification));
                        break;*/
                }

                
            }

            return;
        }

    }
}
