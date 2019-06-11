using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net.Http;
using System.Net.Http.Headers;
using System.Diagnostics;
using Newtonsoft.Json;

namespace WebMonitor
{
    class Program
    {
        public static HttpClient client;
        public static WebMonitorEntities context = new WebMonitorEntities();
        public static string slackChannelUrl = "https://hooks.slack.com/services/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";

        static void Main(string[] args)
        {
            InitialiseClient();

            PerformCheck().GetAwaiter().GetResult();
        }

        static async Task PerformCheck()
        {
            var monitorList = GetMonitorList();

            foreach (var monitor in monitorList)
                Console.WriteLine(monitor.Url);

            foreach (var monitor in monitorList)
            {
                var response = await CheckStatus(monitor.Url);
                //Console.WriteLine(monitor.Monitor_Name + " " + response.statusCode + " " + response.responseTime);

                if (response.pageDown || Int32.Parse(response.responseTime) > 30000)
                {
                    await SendAlert(response, monitor.Monitor_Name, monitor.Url);
                }
            }
        }


        public static List<PageList> GetMonitorList()
        {
            return context.PageLists.ToList();

        }

        //method to check for response
        public static async Task<Response> CheckStatus(string url)
        {
            var response = new Response();
            var stopWatch = Stopwatch.StartNew();

            var test = await client.GetAsync(url);

            response.responseTime = stopWatch.ElapsedMilliseconds.ToString();
            response.pageDown = test.IsSuccessStatusCode == true ? false : true;
            response.statusCode = test.StatusCode.ToString();

            return response;
        }

        //method to send notification to Slack
        public static async Task SendAlert(Response response, string monitorName, string monitorUrl)
        {
            StringBuilder message = new StringBuilder("");
            message.Append(monitorName + " is down. \n");
            message.Append("<" + monitorUrl + ">\n");
            message.Append("ResponseTime: " + response.responseTime + "ms \n");

            if (Int32.Parse(response.responseTime) < 30000)
                message.Append("Status Code: " + response.statusCode);
            else
                message.Append("Timed out. >60secs");

            var content = JsonConvert.SerializeObject(new { text = message.ToString() });

            Console.WriteLine(content);

            var res = await client.PostAsync(slackChannelUrl, new StringContent(content));
        }

        public static void InitialiseClient()
        {
            client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }
    }
}
