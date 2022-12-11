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
    internal class CreateCanvasUserTask : IWorkflowTask
    {
        public CreateCanvasUserTask() { }

        public string TaskType => "create_canvas_user";
        public int? Priority => null;

        public async Task<TaskResult> Execute(Conductor.Client.Models.Task task, CancellationToken token)
        {
            List<TaskExecLog> logList = new List<TaskExecLog>();
            Dictionary<string, object> output = new Dictionary<string, object>();

            //Assign the input vars
            string account_id = task.InputData["account_id"].ToString();
            string auth_provider = task.InputData["auth_provider"].ToString();
            string name = task.InputData["name"].ToString();
            string short_name = task.InputData["short_name"].ToString();
            string sortable_name = task.InputData["sortable_name"].ToString();
            string username = task.InputData["username"].ToString();
            string sis_user_id = task.InputData["sis_user_id"].ToString();
            string pronoun = task.InputData["pronoun"].ToString();

            string email = string.Empty;
            if(!string.IsNullOrEmpty(username))
            {
                email = username + "@doane.edu";
            }

            //If any of these three variables are null stop the job reporting success
            if (string.IsNullOrEmpty(sis_user_id) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email))
            {
                return task.Completed(output, logList);
            }

            //Create the RESTClient
            var client = new RestClient(Environment.GetEnvironmentVariable("CANVAS-API"));

            //Do a GET of the current logins
            RestRequest request = new RestRequest("/accounts/" + account_id + "/users");
            request.Method = Method.POST;
            request.AddHeader("Authorization", "Bearer " + Environment.GetEnvironmentVariable("CANVAS-API-KEY"));
            request.AddParameter("user[name]", name, ParameterType.QueryString);
            request.AddParameter("user[short_name]", short_name, ParameterType.QueryString);
            request.AddParameter("user[sortable_name]", sortable_name, ParameterType.QueryString);
            request.AddParameter("pseudonym[unique_id]", username, ParameterType.QueryString);
            request.AddParameter("pseudonym[sis_user_id]", sis_user_id, ParameterType.QueryString);
            request.AddParameter("pseudonym[send_confirmation]", false, ParameterType.QueryString);
            request.AddParameter("pseudonym[authentication_provider_id]", auth_provider, ParameterType.QueryString);
            request.AddParameter("communication_channel[type]", "email", ParameterType.QueryString);
            request.AddParameter("communication_channel[address]", email, ParameterType.QueryString);
            request.AddParameter("communication_channel[skip_confirmation]", true, ParameterType.QueryString);
            request.AddParameter("enable_sis_reactivation", true, ParameterType.QueryString); //If the user was deleted, reactivate
            IRestResponse userAdd = client.Post(request);

            //PUT to user for pronouns
            request = null;
            request = new RestRequest("/users/sis_user_id:" + sis_user_id);
            request.RequestFormat = DataFormat.None;
            request.Method = Method.PUT;
            request.AddHeader("Authorization", "Bearer " + Environment.GetEnvironmentVariable("CANVAS-API-KEY"));

            request.AddParameter("user[pronouns]", pronoun, ParameterType.QueryString);
            //https://community.canvaslms.com/t5/Idea-Conversations/Personal-Pronouns-should-be-editable-through-Canvas-API-without/idi-p/464190#:~:text=Current%20Solution&text=Have%20an%20admin%20manually%20check,change%20their%20pronouns%20in%20Canvas%E2%80%9D

            IRestResponse userResponse = client.Put(request);

            return task.Completed(output, logList);

        }
    }
}
