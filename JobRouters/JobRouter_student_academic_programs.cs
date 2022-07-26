using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using EthosClient;
using System.Collections.Specialized;
using CanvasService.Jobs;

namespace CanvasService.JobRouters
{
    public class JobRouter_student_academic_programs
    {

        public JobRouter_student_academic_programs()
        {
        }

        [Queue("jobrouter")]
        public void Start(Dictionary<string, string> appSettings, ChangeNotificationV2 changeNotification)
        {
            
        }
    }
}
