﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using EthosClient;
using System.Collections.Specialized;
using CanvasService.Jobs;

namespace CanvasService.JobRouters
{
    public class JobRouter_persons
    {

        public JobRouter_persons()
        {
        }

        [Queue("jobrouter")]
        public void Start(Dictionary<string, string> appSettings, ChangeNotificationV2 changeNotification)
        {
            BackgroundJob.Enqueue(() => new Job_User().Start(appSettings, changeNotification));
        }
    }
}
