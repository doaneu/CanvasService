using CanvasService.Models.Canvas;
using Conductor.Client.Extensions;
using Conductor.Client.Interfaces;
using Conductor.Client.Models;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CanvasService.Tasks
{
    internal class CheckCanvasUserExistsTask : IWorkflowTask
    {
        public CheckCanvasUserExistsTask() { }

        public string TaskType => "check_canvas_user_exists";
        public int? Priority => null;

        public async Task<TaskResult> Execute(Conductor.Client.Models.Task task, CancellationToken token)
        {
            List<TaskExecLog> logList = new List<TaskExecLog>();

            //Create the RESTClient
            var client = new RestClient(Environment.GetEnvironmentVariable("CANVAS-API"));

            //Do a GET of the current logins
            RestRequest request = new RestRequest("/users/sis_user_id:" + task.InputData["sis_user_id"] + "/logins");
            request.Method = Method.GET;
            request.AddHeader("Authorization", "Bearer " + Environment.GetEnvironmentVariable("CANVAS-API-KEY"));

            IRestResponse<List<Login>> logResponse = client.Get<List<Login>>(request);
            Dictionary<string, object> output = new Dictionary<string, object>();

            //not a 404 or 200, then error
            if (!logResponse.StatusCode.Equals(HttpStatusCode.NotFound) && !logResponse.StatusCode.Equals(HttpStatusCode.OK))
            {
                task.Failed("("+ logResponse.StatusCode.ToString() + ") "+logResponse.ErrorMessage, null, null);
            }

                //If a 404, login does not exist, nothing to do but end
                if (logResponse.StatusCode.Equals(HttpStatusCode.NotFound))
            {            
                output.Add("exists", false);
                return task.Completed(output, logList);
            }
            else
            {
                //Get the Ids needed to update the login
                int accountId = logResponse.Data[0].account_id;
                int loginId = logResponse.Data[0].id;

                output.Add("exists", true);
                output.Add("account_id", accountId);
                output.Add("login_id", loginId);
                return task.Completed(output, logList);
            }
        }
    }
}
