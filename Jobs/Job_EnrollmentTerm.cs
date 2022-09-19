using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Policy;
using System.Threading.Tasks;
using System.Web;
using CanvasService.Models.Canvas;
using EthosClient;
using Hangfire;
using NewRelic.Api.Agent;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace CanvasService.Jobs
{
    public class Job_EnrollmentTerm
    {
        public void Job_EnrollmentTermJob()
        {

        }

        [Queue("job")]
        [DisableConcurrentExecution(timeoutInSeconds: 10 * 60)]
        [Transaction]
        public async Task Start(Dictionary<string, string> appSettings, ChangeNotificationV2 changeNotification)
        {
            //Create the Ethos Client
            EthosHttpClient ethosHttpClient = new EthosHttpClient();
            ethosHttpClient.ApiKey = appSettings["ETHOS-API-KEY"];
            ethosHttpClient.BaseAddress = new Uri(EthosConstants.EthosBaseUrl);

            //Create the RESTClient
            var client = new RestClient(appSettings["CANVAS-API"]);

            bool termExists = false;
            GetResponse response;
            RestRequest request;

            //Get the content info
            JObject content = changeNotification.Content;

            string name = string.Empty;
            string sis_term_id = string.Empty;
            DateTime? start_at;
            DateTime? end_at;

            //Resolve name
            name = JSONPather.ResolveTemplate(appSettings["TERM-SIS-TERM-NAME"], content);

            //Resolve sis_user_id
            sis_term_id = JSONPather.ResolveTemplate(appSettings["TERM-SIS-TERM-ID"], content);

            //Resolve dates
            string sDate = JSONPather.GetValue("$.startOn", content);
            string eDate = JSONPather.GetValue("$.endOn", content);

            //Do a GET of the term
            request = new RestRequest("/accounts/"+appSettings["DEFAULT-ACCOUNT"]+"/terms/sis_term_id:"+HttpUtility.UrlEncode(sis_term_id));
            request.Method = Method.Get;
            request.AddHeader("Authorization", "Bearer " + appSettings["CANVAS-API-KEY"]);

            RestResponse termResponse = await client.ExecuteGetAsync(request);

            if(termResponse.StatusCode.Equals(HttpStatusCode.OK))
            {
                //Term exists in Canvas
                termExists = true;
            }
            else
            {
                termExists = false;
            }
        }

    }
}
