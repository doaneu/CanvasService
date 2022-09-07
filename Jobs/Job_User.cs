using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CanvasService.Models.Canvas;
using EthosClient;
using Hangfire;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace CanvasService.Jobs
{
    public class Job_User
    {
        public void Job_UserJob()
        {

        }

        [Queue("job")]
        //[DisableConcurrentExecution(timeoutInSeconds: 5)]
        public async Task Start(Dictionary<string, string> appSettings, ChangeNotificationV2 changeNotification)
        {
            //Delay job for a random amount
            int delay = new Random().Next(1, 30) * 1000;
            System.Threading.Thread.Sleep(delay);

            //JSON output of job info to console, for logging services like New Relic
            Event serviceEvent = new Event();
            serviceEvent.ServiceName = "Canvas.Service";
            serviceEvent.EventName = "User Job - Start";
            serviceEvent.EventKey = changeNotification.Resource.Id;
            serviceEvent.EventDetails = changeNotification.Content;

            Console.WriteLine(JsonConvert.SerializeObject(serviceEvent));


            //Create the Ethos Client
            EthosHttpClient ethosHttpClient = new EthosHttpClient();
            ethosHttpClient.ApiKey = appSettings["ETHOS-API-KEY"];
            ethosHttpClient.BaseAddress = new Uri(EthosConstants.EthosBaseUrl);

            //Create the RESTClient
            var client = new RestClient(appSettings["CANVAS-API"]);

            bool userExists = false;
            GetResponse response;
            RestRequest request;

            //Get the content info
            JObject content = changeNotification.Content;

            string name = string.Empty;
            string short_name = string.Empty;
            string sortable_name = string.Empty;
            string sis_user_id = string.Empty;
            string login_id = string.Empty;
            string pronoun = string.Empty;
            string email = string.Empty;
            string time_zone = string.Empty;

            //Only try this if there is content
            if (content != null)
            {
                //Resolve sis_user_id
                sis_user_id = JSONPather.ResolveTemplate(appSettings["USER-SIS-USER-ID"], content);

                serviceEvent = new Event();
                serviceEvent.ServiceName = "Canvas.Service";
                serviceEvent.EventName = "User Job - Resolve SIS User ID";
                serviceEvent.EventKey = changeNotification.Resource.Id;
                serviceEvent.EventDetails = sis_user_id;

                Console.WriteLine(JsonConvert.SerializeObject(serviceEvent));


                //Resolve login_id
                login_id = JSONPather.ResolveTemplate(appSettings["USER-LOGIN-ID"], content);

                serviceEvent = new Event();
                serviceEvent.ServiceName = "Canvas.Service";
                serviceEvent.EventName = "User Job - Resolve Login ID";
                serviceEvent.EventKey = changeNotification.Resource.Id;
                serviceEvent.EventDetails = login_id;

                Console.WriteLine(JsonConvert.SerializeObject(serviceEvent));

                //Resolve email
                email = JSONPather.ResolveTemplate(appSettings["USER-EMAIL"], content);

                serviceEvent = new Event();
                serviceEvent.ServiceName = "Canvas.Service";
                serviceEvent.EventName = "User Job - Resolve Email";
                serviceEvent.EventKey = changeNotification.Resource.Id;
                serviceEvent.EventDetails = email;

                Console.WriteLine(JsonConvert.SerializeObject(serviceEvent));

            }

            //If this is a delete, remove the login, or if the user does not have a login_id
            if ((changeNotification.Operation.Equals("deleted") || string.IsNullOrEmpty(login_id)) && (!changeNotification.Operation.Equals("created") && !changeNotification.Operation.Equals("updated") && !changeNotification.Operation.Equals("replaced")))
            {
                //Do a GET of the current logins
                request = new RestRequest("/users/sis_user_id:" + sis_user_id + "/logins");
                request.Method = Method.Get;
                request.AddHeader("Authorization", "Bearer " + appSettings["CANVAS-API-KEY"]);

                RestResponse<List<Login>> logResponse = await client.ExecuteGetAsync<List<Login>>(request);

                //If a 404, login does not exist, nothing to do but end
                if (logResponse.StatusCode.Equals(HttpStatusCode.NotFound))
                {
                    serviceEvent = new Event();
                    serviceEvent.ServiceName = "Canvas.Service";
                    serviceEvent.EventName = "User Job - End";
                    serviceEvent.EventKey = changeNotification.Resource.Id;
                    serviceEvent.EventDetails = "No Canvas User to Delete";

                    Console.WriteLine(JsonConvert.SerializeObject(serviceEvent));

                    return;
                }
                else
                {
                    //Get the Ids needed to update the login
                    int accountId = logResponse.Data[0].account_id;
                    int loginId = logResponse.Data[0].id;

                    //Otherwise delete the login
                    request = null;
                    request = new RestRequest("/users/sis_user_id:" + sis_user_id + "/logins/" + loginId);
                    request.Method = Method.Delete;
                    request.AddHeader("Authorization", "Bearer " + appSettings["CANVAS-API-KEY"]);

                    await client.DeleteAsync(request);

                    serviceEvent = new Event();
                    serviceEvent.ServiceName = "Canvas.Service";
                    serviceEvent.EventName = "User Job - End";
                    serviceEvent.EventKey = changeNotification.Resource.Id;
                    serviceEvent.EventDetails = "User with sis_user_id of " + sis_user_id + " was deleted";

                    Console.WriteLine(JsonConvert.SerializeObject(serviceEvent));

                    return;
                }
            }

           

            //If any of the above three resolutions comes back blank, stop the job. Don't create or update incomplete records.
            if (string.IsNullOrEmpty(sis_user_id) || string.IsNullOrEmpty(login_id) || string.IsNullOrEmpty(email))
            {
                serviceEvent = new Event();
                serviceEvent.ServiceName = "Canvas.Service";
                serviceEvent.EventName = "User Job - End";
                serviceEvent.EventKey = changeNotification.Resource.Id;
                serviceEvent.EventDetails = "sis_user_id, login_id, or email was not resolved to a value. Stopping job so incomplete records are not created.";

                Console.WriteLine(JsonConvert.SerializeObject(serviceEvent));
                return;
            }

            //Start Names // // // // / / / // / / / / / / / / / / / / // / / / / / / /  / / / / / / / / /
            //Get the names for the person we have to loop through them and set vars as we go
            string legalFirstName = string.Empty;
            string legalLastName = string.Empty;
            string nickname = string.Empty;
            string chosenFirstName = string.Empty;
            string chosenLastName = string.Empty;

            foreach (JObject nameObject in content.SelectTokens("names[*]"))
            {
                string typeId = JSONPather.GetValue("$.type.detail.id", nameObject);

                Guid typeGuid = Guid.Parse(typeId);
                string version = null;
                response = ethosHttpClient.Get(typeGuid, "person-name-types", version);
                string nameType = JSONPather.GetValue("$.code", JObject.Parse(response.Data));

                if (nameType.Equals("LEGAL"))
                {
                    legalFirstName = JSONPather.GetValue("$.firstName", nameObject);
                    legalLastName = JSONPather.GetValue("$.lastName", nameObject);
                }

                if (nameType.Equals("NICKNAME"))
                {
                    nickname = JSONPather.GetValue("$.fullName", nameObject);
                }

                if (nameType.Equals("CHOSEN"))
                {
                    chosenFirstName = JSONPather.GetValue("$.firstName", nameObject);
                    chosenLastName = JSONPather.GetValue("$.lastName", nameObject);
                }
            }

            //Build the names using a hierarchy (Default: Chosen, Nickname, Legal)
            string resolvedFirstName = string.Empty;
            string resolvedLastName = string.Empty;

            //Legal Name
            resolvedFirstName = legalFirstName;
            resolvedLastName = legalLastName;

            //Override First Name with Nickname if there is one
            if (!string.IsNullOrEmpty(nickname))
            {
                resolvedFirstName = nickname;
            }

            //Override First Name with Chosen First Name if there is one
            if (!string.IsNullOrEmpty(chosenFirstName))
            {
                resolvedFirstName = chosenFirstName;
            }

            //Override Last Name with Chosen Last Name if there is one
            if (!string.IsNullOrEmpty(chosenLastName))
            {
                resolvedLastName = chosenLastName;
            }

            //Finally set the vars that are sent to Canvas
            name = resolvedFirstName + " " + resolvedLastName;
            short_name = resolvedFirstName + " " + resolvedLastName;
            sortable_name = resolvedLastName + ", " + resolvedFirstName;

            serviceEvent = new Event();
            serviceEvent.ServiceName = "Canvas.Service";
            serviceEvent.EventName = "User Job - Resolve Name";
            serviceEvent.EventKey = changeNotification.Resource.Id;

            var loggableNames = new { name=name, short_name=short_name, sortable_name=sortable_name };

            serviceEvent.EventDetails = loggableNames;

            Console.WriteLine(JsonConvert.SerializeObject(serviceEvent));

            //End Names // // // // / / / // / / / / / / / / / / / / // / / / / / / /  / / / / / / / / /

            //Get the personal pronoun, but only if one is selected by the person
            string pronounId = JSONPather.GetValue("$.personalPronoun.id", content);

            if (!string.IsNullOrEmpty(pronounId))
            {
                Guid pronounGuid = Guid.Parse(pronounId);
                string version = null;
                response = ethosHttpClient.Get(pronounGuid, "personal-pronouns", version);
                pronoun = JSONPather.GetValue("$.title", JObject.Parse(response.Data));

                serviceEvent = new Event();
                serviceEvent.ServiceName = "Canvas.Service";
                serviceEvent.EventName = "User Job - Resolve Preferred Pronoun";
                serviceEvent.EventKey = changeNotification.Resource.Id;
                serviceEvent.EventDetails = "Pronoun resolved to "+pronoun;

                Console.WriteLine(JsonConvert.SerializeObject(serviceEvent));
            }

            //Handle an update
            //Do a GET of the current logins
            request = new RestRequest("/users/sis_user_id:" + sis_user_id + "/logins");
            request.Method = Method.Get;
            request.AddHeader("Authorization", "Bearer " + appSettings["CANVAS-API-KEY"]);

            RestResponse<List<Login>> loginResponse = await client.ExecuteGetAsync<List<Login>>(request);

            //If a 200 continue, otherwise user needs to be created
            if (loginResponse.StatusCode.Equals(HttpStatusCode.OK))
            {
                //Get the Ids needed to update the login
                int accountId = loginResponse.Data[0].account_id;
                int loginId = loginResponse.Data[0].id;

                //PUT to accounts to update username, auth provider
                request = null;
                request = new RestRequest("/accounts/" + accountId + "/logins/" + loginId);
                request.Method = Method.Put;
                request.AddHeader("Authorization", "Bearer " + appSettings["CANVAS-API-KEY"]);
                request.AddParameter("login[unique_id]", login_id, ParameterType.QueryString);
                request.AddParameter("login[sis_user_id]", sis_user_id, ParameterType.QueryString);
                request.AddParameter("login[authentication_provider_id]", appSettings["USER-AUTH-PROVIDER"], ParameterType.QueryString);
                request.AddParameter("login[workflow_state]", "active", ParameterType.QueryString, true);

                RestResponse loginPutResponse = await client.ExecutePutAsync(request);

                //PUT to users can update name and pronouns
                request = null;
                request = new RestRequest("/users/sis_user_id:" + sis_user_id);
                request.RequestFormat = DataFormat.None;
                request.Method = Method.Put;
                request.AddHeader("Authorization", "Bearer " + appSettings["CANVAS-API-KEY"]);

                request.AddParameter("user[pronouns]", pronoun, ParameterType.QueryString);
                //https://community.canvaslms.com/t5/Idea-Conversations/Personal-Pronouns-should-be-editable-through-Canvas-API-without/idi-p/464190#:~:text=Current%20Solution&text=Have%20an%20admin%20manually%20check,change%20their%20pronouns%20in%20Canvas%E2%80%9D

                request.AddParameter("user[name]", name, ParameterType.QueryString);
                request.AddParameter("user[short_name]", short_name, ParameterType.QueryString);
                request.AddParameter("user[sortable_name]", sortable_name, ParameterType.QueryString);

                RestResponse userResponse = await client.ExecutePutAsync(request);

                //GET a list of communication methods
                request = null;
                request = new RestRequest("/users/sis_user_id:" + sis_user_id + "/communication_channels");
                request.Method = Method.Get;
                request.AddHeader("Authorization", "Bearer " + appSettings["CANVAS-API-KEY"]);

                RestResponse<List<CommunicationChannel>> channelResponse = await client.ExecuteGetAsync<List<CommunicationChannel>>(request);

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
                            request.Method = Method.Delete;
                            request.AddHeader("Authorization", "Bearer " + appSettings["CANVAS-API-KEY"]);
                            RestResponse chanDelete = await client.DeleteAsync(request);
                        }
                    }
                }

                //Add the new email if it was not found
                if (!emailExists)
                {
                    request = null;
                    request = new RestRequest("/users/sis_user_id:" + sis_user_id + "/communication_channels");
                    request.Method = Method.Post;
                    request.AddHeader("Authorization", "Bearer " + appSettings["CANVAS-API-KEY"]);
                    request.AddParameter("communication_channel[address]", email, ParameterType.QueryString);
                    request.AddParameter("communication_channel[type]", "email", ParameterType.QueryString);
                    request.AddParameter("skip_confirmation", true, ParameterType.QueryString); //skip confirmation email
                    RestResponse chanAdd = await client.PostAsync(request);
                }

                userExists = true;

                serviceEvent = new Event();
                serviceEvent.ServiceName = "Canvas.Service";
                serviceEvent.EventName = "User Job - Update User";
                serviceEvent.EventKey = changeNotification.Resource.Id;
                serviceEvent.EventDetails = "User with sis_user_id of " + sis_user_id + " has been updated";

                Console.WriteLine(JsonConvert.SerializeObject(serviceEvent));

                serviceEvent = new Event();
                serviceEvent.ServiceName = "Canvas.Service";
                serviceEvent.EventName = "User Job - End";
                serviceEvent.EventKey = changeNotification.Resource.Id;
                serviceEvent.EventDetails = "Job has ended";

                Console.WriteLine(JsonConvert.SerializeObject(serviceEvent));

                return;
            }
            else
            {
                userExists = false;

                serviceEvent = new Event();
                serviceEvent.ServiceName = "Canvas.Service";
                serviceEvent.EventName = "User Job - Force User Create";
                serviceEvent.EventKey = changeNotification.Resource.Id;
                serviceEvent.EventDetails = "User with sis_user_id of " + sis_user_id + " does not exist to be updated. Forcing a creation.";

                Console.WriteLine(JsonConvert.SerializeObject(serviceEvent));
            }


            //Handle a create
            if (!userExists)
            {
                request = new RestRequest("/accounts/" + appSettings["USER-DEFAULT-ACCOUNT"] + "/users");
                request.Method = Method.Post;
                request.AddHeader("Authorization", "Bearer " + appSettings["CANVAS-API-KEY"]);
                request.AddParameter("user[name]", name, ParameterType.QueryString);
                request.AddParameter("user[short_name]", short_name, ParameterType.QueryString);
                request.AddParameter("user[sortable_name]", sortable_name, ParameterType.QueryString);
                request.AddParameter("pseudonym[unique_id]", login_id, ParameterType.QueryString);
                request.AddParameter("pseudonym[sis_user_id]", sis_user_id, ParameterType.QueryString);
                request.AddParameter("pseudonym[send_confirmation]", false, ParameterType.QueryString);
                request.AddParameter("pseudonym[authentication_provider_id]", appSettings["USER-AUTH-PROVIDER"], ParameterType.QueryString);
                request.AddParameter("communication_channel[type]", "email", ParameterType.QueryString);
                request.AddParameter("communication_channel[address]", email, ParameterType.QueryString);
                request.AddParameter("communication_channel[skip_confirmation]", true, ParameterType.QueryString);
                request.AddParameter("enable_sis_reactivation", true, ParameterType.QueryString); //If the user was deleted, reactivate
                RestResponse userAdd = await client.PostAsync(request);

                //PUT to user for pronouns
                request = null;
                request = new RestRequest("/users/sis_user_id:" + sis_user_id);
                request.RequestFormat = DataFormat.None;
                request.Method = Method.Put;
                request.AddHeader("Authorization", "Bearer " + appSettings["CANVAS-API-KEY"]);

                request.AddParameter("user[pronouns]", pronoun, ParameterType.QueryString);
                //https://community.canvaslms.com/t5/Idea-Conversations/Personal-Pronouns-should-be-editable-through-Canvas-API-without/idi-p/464190#:~:text=Current%20Solution&text=Have%20an%20admin%20manually%20check,change%20their%20pronouns%20in%20Canvas%E2%80%9D

                RestResponse userResponse = await client.ExecutePutAsync(request);

                serviceEvent = new Event();
                serviceEvent.ServiceName = "Canvas.Service";
                serviceEvent.EventName = "User Job - Create User";
                serviceEvent.EventKey = changeNotification.Resource.Id;
                serviceEvent.EventDetails = "User with sis_user_id of "+sis_user_id+" has been created";

                Console.WriteLine(JsonConvert.SerializeObject(serviceEvent));
            }

            serviceEvent = new Event();
            serviceEvent.ServiceName = "Canvas.Service";
            serviceEvent.EventName = "User Job - End";
            serviceEvent.EventKey = changeNotification.Resource.Id;
            serviceEvent.EventDetails = "Job has ended";

            Console.WriteLine(JsonConvert.SerializeObject(serviceEvent));
        }

    }
}
