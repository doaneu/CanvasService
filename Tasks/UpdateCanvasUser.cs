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
    internal class UpdateCanvasUserTask : IWorkflowTask
    {
        public UpdateCanvasUserTask() { }

        public string TaskType => "update_canvas_user";
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
            string login_id = task.InputData["login_id"].ToString();

            string sis_user_id = String.Empty;
            try
            {
                sis_user_id = task.InputData["sis_user_id"].ToString();
            }
            catch
            {

            }

            string username = String.Empty;
            try
            {
                username = task.InputData["username"].ToString();
            }
            catch
            {

            }

            string pronoun = String.Empty;
            try
            {
             pronoun = task.InputData["pronoun"].ToString();
            }
            catch
            {

            }

            string email = string.Empty;
            if (!string.IsNullOrEmpty(username))
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

            //PUT to accounts to update username, auth provider
            RestRequest request = new RestRequest("/accounts/" + account_id + "/logins/" + login_id);
            request.Method = Method.PUT;
            request.AddHeader("Authorization", "Bearer " + Environment.GetEnvironmentVariable("CANVAS-API-KEY"));
            request.AddParameter("login[unique_id]", username, ParameterType.QueryString);
            request.AddParameter("login[sis_user_id]", sis_user_id, ParameterType.QueryString);
            request.AddParameter("login[authentication_provider_id]", auth_provider, ParameterType.QueryString);
            request.AddParameter("login[workflow_state]", "active", ParameterType.QueryString);

            IRestResponse loginPutResponse = client.Put(request);

            //PUT to users can update name and pronouns
            request = null;
            request = new RestRequest("/users/sis_user_id:" + sis_user_id);
            request.RequestFormat = DataFormat.None;
            request.Method = Method.PUT;
            request.AddHeader("Authorization", "Bearer " + Environment.GetEnvironmentVariable("CANVAS-API-KEY"));

            if (!string.IsNullOrEmpty(pronoun))
            {
                request.AddParameter("user[pronouns]", pronoun, ParameterType.QueryString);
                //https://community.canvaslms.com/t5/Idea-Conversations/Personal-Pronouns-should-be-editable-through-Canvas-API-without/idi-p/464190#:~:text=Current%20Solution&text=Have%20an%20admin%20manually%20check,change%20their%20pronouns%20in%20Canvas%E2%80%9D
            }

            request.AddParameter("user[name]", name, ParameterType.QueryString);
            request.AddParameter("user[short_name]", short_name, ParameterType.QueryString);
            request.AddParameter("user[sortable_name]", sortable_name, ParameterType.QueryString);

            IRestResponse userResponse = client.Put(request);

            //GET a list of communication methods
            request = null;
            request = new RestRequest("/users/sis_user_id:" + sis_user_id + "/communication_channels");
            request.Method = Method.GET;
            request.AddHeader("Authorization", "Bearer " + Environment.GetEnvironmentVariable("CANVAS-API-KEY"));

            IRestResponse<List<CommunicationChannel>> channelResponse = client.Get<List<CommunicationChannel>>(request);

            //Determine the email domain
            string emailDomain = email.Substring(email.IndexOf('@'));

            //Loop, find the emails we need to delete, if any
            bool emailExists = false;
            foreach (CommunicationChannel channel in channelResponse.Data)
            {
                if (channel.address.Contains(emailDomain))
                {
                    if (channel.address.Equals(email))
                    {
                        emailExists = true;
                    }
                    else
                    {
                        //Delete this email, it may be old
                        request = null;
                        request = new RestRequest("/users/sis_user_id:" + sis_user_id + "/communication_channels/" + channel.id);
                        request.Method = Method.DELETE;
                        request.AddHeader("Authorization", "Bearer " + Environment.GetEnvironmentVariable("CANVAS-API-KEY"));
                        IRestResponse chanDelete = client.Delete(request);
                    }
                }
            }

            //Add the new email if it was not found
            if (!emailExists)
            {
                request = null;
                request = new RestRequest("/users/sis_user_id:" + sis_user_id + "/communication_channels");
                request.Method = Method.POST;
                request.AddHeader("Authorization", "Bearer " + Environment.GetEnvironmentVariable("CANVAS-API-KEY"));
                request.AddParameter("communication_channel[address]", email, ParameterType.QueryString);
                request.AddParameter("communication_channel[type]", "email", ParameterType.QueryString);
                request.AddParameter("skip_confirmation", true, ParameterType.QueryString); //skip confirmation email
                IRestResponse chanAdd = client.Post(request);
            }

            return task.Completed(output, logList);

        }
    }
}
