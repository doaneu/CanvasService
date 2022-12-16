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
    internal class CheckCanvasEnrollmentExistsTask : IWorkflowTask
    {
        public CheckCanvasEnrollmentExistsTask() { }

        public string TaskType => "check_canvas_enrollment_exists";
        public int? Priority => null;

        public async Task<TaskResult> Execute(Conductor.Client.Models.Task task, CancellationToken token)
        {
            List<TaskExecLog> logList = new List<TaskExecLog>();

            //Create the RESTClient
            var client = new RestClient(Environment.GetEnvironmentVariable("CANVAS-API"));

            //Do a GET of the current logins
            RestRequest request = new RestRequest("/users/sis_user_id:" + task.InputData["sis_user_id"] + "/enrollments");
            request.Method = Method.GET;
            request.AddHeader("Authorization", "Bearer " + Environment.GetEnvironmentVariable("CANVAS-API-KEY"));
            request.AddParameter("state[]", "active", ParameterType.QueryString);
            request.AddParameter("state[]", "inactive", ParameterType.QueryString);
            request.AddParameter("state[]", "deleted", ParameterType.QueryString);

            IRestResponse<List<Enrollment>> enrResponse = client.Get<List<Enrollment>>(request);
            Dictionary<string, object> output = new Dictionary<string, object>();

            //not a 404 or 200, then error
            if (!enrResponse.StatusCode.Equals(HttpStatusCode.NotFound) && !enrResponse.StatusCode.Equals(HttpStatusCode.OK))
            {
                task.Failed("("+ enrResponse.StatusCode.ToString() + ") "+ enrResponse.ErrorMessage, null, null);
            }

            //If a 404, login does not exist, nothing to do but end
            if (enrResponse.StatusCode.Equals(HttpStatusCode.NotFound))
            {                          
                return task.Failed("User does not exist in Canvas");
            }
            else
            {

                string lookup_sis_section_id = task.InputData["sis_section_id"].ToString();
                int lookup_role_id = int.Parse(task.InputData["role_id"].ToString());

                //Get the Ids needed to perform operations against the enrollment
                foreach (Enrollment enr in enrResponse.Data)
                {
                    //Does the role/sis_section_id match?
                    if(enr.sis_section_id.Equals(lookup_sis_section_id) && enr.role_id.Equals(lookup_role_id))
                    {
                        output.Add("user_exists", true);
                        output.Add("enrollment_exists", true);
                        output.Add("enrollment_id", enr.id);
                        output.Add("enrollment_state", enr.enrollment_state);
                        output.Add("sis_section_id", enr.sis_section_id);
                        output.Add("course_id", enr.course_id);
                        output.Add("course_section_id", enr.course_section_id);
                        output.Add("role_id", enr.role_id);

                        return task.Completed(output, logList);
                    }
                }

                output.Add("user_exists", true);
                output.Add("enrollment_exists", false);

                return task.Completed(output, logList);

            }
        }
    }
}
