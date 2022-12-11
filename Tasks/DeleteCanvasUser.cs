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
    internal class DeleteCanvasUserTask : IWorkflowTask
    {
        public DeleteCanvasUserTask() { }

        public string TaskType => "delete_canvas_user";
        public int? Priority => null;

        public async Task<TaskResult> Execute(Conductor.Client.Models.Task task, CancellationToken token)
        {
            List<TaskExecLog> logList = new List<TaskExecLog>();
            Dictionary<string, object> output = new Dictionary<string, object>();

            //Assign the input vars
            string account_id = task.InputData["account_id"].ToString();
            string sis_user_id = task.InputData["sis_user_id"].ToString();

            //Create the RESTClient
            var client = new RestClient(Environment.GetEnvironmentVariable("CANVAS-API"));

            //Do a GET of the current logins
            RestRequest request = new RestRequest("/accounts/"+account_id+"/users/sis_user_id:" + sis_user_id);
            request.Method = Method.DELETE;
            request.AddHeader("Authorization", "Bearer " + Environment.GetEnvironmentVariable("CANVAS-API-KEY"));

            IRestResponse response = client.Delete(request);

            return task.Completed(output, logList);

        }
    }
}
