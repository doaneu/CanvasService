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
    internal class DeleteCanvasEnrollmentTask : IWorkflowTask
    {
        public DeleteCanvasEnrollmentTask() { }

        public string TaskType => "delete_canvas_enrollment";
        public int? Priority => null;

        public async Task<TaskResult> Execute(Conductor.Client.Models.Task task, CancellationToken token)
        {
            List<TaskExecLog> logList = new List<TaskExecLog>();
            Dictionary<string, object> output = new Dictionary<string, object>();

            //Assign the input vars
            string enrollment_id = task.InputData["enrollment_id"].ToString();
            string sis_section_id = task.InputData["sis_section_id"].ToString();

            //Create the RESTClient
            var client = new RestClient(Environment.GetEnvironmentVariable("CANVAS-API"));

            //Delete the enrollment
            RestRequest request = new RestRequest("/courses/sis_course_id:" + sis_section_id + "/enrollments/" + enrollment_id);
            request.Method = Method.DELETE;
            request.AddHeader("Authorization", "Bearer " + Environment.GetEnvironmentVariable("CANVAS-API-KEY"));
            request.AddParameter("task", "delete", ParameterType.QueryString);
            IRestResponse enrDea = client.Delete(request);

            return task.Completed(output, logList);

        }
    }
}
