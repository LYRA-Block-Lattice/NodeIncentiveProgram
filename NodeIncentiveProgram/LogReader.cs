using Lyra.Core.API;
using Nebula.Store.NodeViewUseCase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NodeIncentiveProgram
{
    public class LogReader
    {
        private HttpClient _client;
        private CancellationTokenSource _cancel;

        public LogReader(string networkId)
        {
            var httpClientHandler = new HttpClientHandler();
            _client = new HttpClient(httpClientHandler);
            var url = "https://blockexplorer.testnet.lyra.live/api/nebula/";
            _client.BaseAddress = new Uri(url);
            //_client.DefaultRequestHeaders.Accept.Clear();
            //_client.DefaultRequestHeaders.Accept.Add(
            //    new MediaTypeWithQualityHeaderValue("application/json"));
            //#if DEBUG
            //            _client.Timeout = new TimeSpan(0, 0, 30);
            //#else
            _client.Timeout = new TimeSpan(0, 0, 15);
            //#endif

            _cancel = new CancellationTokenSource();
        }

        public async Task<IEnumerable<NodeViewState>> GetHistoryAsync()
        {
            return await Get<IEnumerable<NodeViewState>>("history", null);
        }

        private async Task<T> Get<T>(string action, Dictionary<string, string> args)
        {
            var url = $"{action}/?" + args?.Aggregate(new StringBuilder(),
                          (sb, kvp) => sb.AppendFormat("{0}{1}={2}",
                                       sb.Length > 0 ? "&" : "", kvp.Key, kvp.Value),
                          sb => sb.ToString());
            HttpResponseMessage response = await _client.GetAsync(url, _cancel.Token);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsAsync<T>();
                return result;
            }
            else
                throw new Exception($"Web Api Failed: {response.StatusCode} {await response.Content.ReadAsStringAsync()}");
        }
    }
}
