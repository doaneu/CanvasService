using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using EthosClient;
using System.Collections.Specialized;
using CanvasService.Jobs;
using NewRelic.Api.Agent;

namespace CanvasService.JobRouters
{
    public class JobRouter_student_academic_programs
    {

        public JobRouter_student_academic_programs()
        {
        }

        [Queue("jobrouter")]
        [Transaction]
        public void Start(Dictionary<string, string> appSettings, ChangeNotificationV2 changeNotification)
        {
            
        }
    }
}
