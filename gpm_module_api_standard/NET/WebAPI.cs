using Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
namespace gpm_vibration_module_api.NET
{
    internal class WebAPI
    {
        internal static async void DeviceDataPost(DeviceTestData kxTestSta)
        {
            var json_string = kxTestSta.ToJson();
            var reply = await Post("https://gpmwebapplication.herokuapp.com/", "api/OfficeTest/KxTestDataUpdate", json_string);
            //var reply = await Post("http://localhost:8080/", "api/OfficeTest/KxTestDataUpdate", json_string);
            Console.WriteLine(json_string + "::" + reply.Item1);
        }

        private static async Task<Tuple<bool, string>> Post(string host, string api_route, string JsonString)
        {
            using (var client = new HttpClient())
            {
                try
                {

                    client.BaseAddress = new Uri(host);
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    HttpContent httpContent = new StringContent(JsonString);
                    httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                    // HTTP POST
                    HttpResponseMessage response = await client.PostAsync(api_route, httpContent);
                    string server_ret = "";
                    if (response.IsSuccessStatusCode)
                    {
                        server_ret = await response.Content.ReadAsStringAsync();
                        return new Tuple<bool, string>(true, server_ret);
                    }
                    else
                    {
                        server_ret = await response.Content.ReadAsStringAsync();
                        return new Tuple<bool, string>(false, server_ret);
                    }
                }
                catch (Exception ex)
                {
                    //.WriteLine(ex.Message);
                    return new Tuple<bool, string>(false, ex.Message);
                }
            }
        }

    }
    public class DeviceTestData
    {
        public string PCName { get; set; } = "NAN";
        public string IP { get; set; } = "0.0.0.0";
        public bool ModuleConnected { get; set; } = false;
        public int DataLenSet { get; set; } = 512;
        public string MEASRangeSet { get; set; } = "8G";
        public int SendRequestNumber { get; set; } = -1;
        public int DeviceReplyOKNumber { get; set; } = -1;
        public int ErrorCode { get; set; } = -1;
        public int MeasureTime { get; set; } = -1;
        public double[] XAxisRawData { get; set; }
        public double[] YAxisRawData { get; set; }
        public double[] ZAxisRawData { get; set; }
    }
}
