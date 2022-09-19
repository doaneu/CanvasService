using EthosClient;
using Hangfire;
using CanvasService.Jobs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NewRelic.Api.Agent;

namespace SalesforceService.JobRouters
{
    public class JobRouter_academic_periods
    {

        public void JobRouter_academic_periods_Job()
        {

        }

        [Queue("jobrouter")]
        [Transaction]
        public void Start(Dictionary<string, string> appSettings, ChangeNotificationV2 changeNotification)
        {
            BackgroundJob.Enqueue(() => new Job_EnrollmentTerm().Start(appSettings, changeNotification));
        }
    }
}
