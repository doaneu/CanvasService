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
    internal class CreateCanvasEnrollmentTask : IWorkflowTask
    {
        public CreateCanvasEnrollmentTask() { }

        public string TaskType => "create_canvas_enrollment";
        public int? Priority => null;

        public async Task<TaskResult> Execute(Conductor.Client.Models.Task task, CancellationToken token)
        {
            List<TaskExecLog> logList = new List<TaskExecLog>();
            Dictionary<string, object> output = new Dictionary<string, object>();

            //Assign the input vars
            string user_id = task.InputData["user_id"].ToString();
            string role_id = task.InputData["role_id"].ToString();
            string enrollment_state = task.InputData["enrollment_state"].ToString();
            string sis_section_id = task.InputData["sis_section_id"].ToString();

            //Create the RESTClient
            var client = new RestClient(Environment.GetEnvironmentVariable("CANVAS-API"));


            RestRequest request = new RestRequest("/sections/sis_section_id:" + sis_section_id + "/enrollments");
            request.Method = Method.POST;
            request.AddHeader("Authorization", "Bearer " + Environment.GetEnvironmentVariable("CANVAS-API-KEY"));
            request.AddParameter("enrollment[user_id]", user_id, ParameterType.QueryString);
            request.AddParameter("enrollment[role_id]", role_id, ParameterType.QueryString);
            request.AddParameter("enrollment[enrollment_state]", enrollment_state, ParameterType.QueryString);

            IRestResponse enrAdd = client.Post(request);

            if (!enrAdd.IsSuccessful)
            {
                return task.Failed("("+enrAdd.StatusCode+") "+enrAdd.Content);
            }

            return task.Completed(output, logList);

        }
    }
}
