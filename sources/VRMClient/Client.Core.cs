using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace VoterDB
{
    // Core functionality for sending / receiving rest calls.
    public partial class Client
    {
        private IEnumerable<TData> GetPagedData<TData>(string url, params object[] args)
        {
            int c = 0;
            int page = 1;

            url = string.Format(url, args);
            char appendChar = url.Contains("?") ? '&' : '?';

            while (true)
            {
                string url2 = string.Format("{0}{1}page={2}", url, appendChar, page);
                var req = MakeRequest(HttpMethod.Get, url2);

                var x = Send<Pages<TData>>(req);

                foreach (TData record in x.results)
                {
                    c++;
                    yield return record;
                }

                if (x.page >= x.pages)
                {
                    break;
                }
                page++;
            }
        }

        static void AddBody(HttpRequestMessage req, object body)
        {
            string json = JsonConvert.SerializeObject(body);
            req.Content = new StringContent(json);
            req.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        }

        // Throw exception if the result failed.
        void Verify(Results result)
        {
            if (!result.IsValid)
            {
                // There was an error
                throw new InvalidOperationException("Requested failed: " + result.message);
            }
        }

        private TData Send<TData>(HttpRequestMessage request)
        {
            var result = SendRaw<Results<TData>>(request);

            Verify(result);

            return result.data;
        }

        private void Send(HttpRequestMessage request)
        {
            var result = SendRaw<Results>(request);

            Verify(result);            
        }

        private T SendRaw<T>(HttpRequestMessage req)
        {
            string content = SendRawJson(req);
            var x = JsonConvert.DeserializeObject<T>(content);

            return x;
        }

        // Get raw JSON back. 
        private string SendRawJson(HttpRequestMessage req)
        {
            var resp = _client.SendAsync(req).Result;
            string content = resp.Content.ReadAsStringAsync().Result;

            return content;
        }
    }
}