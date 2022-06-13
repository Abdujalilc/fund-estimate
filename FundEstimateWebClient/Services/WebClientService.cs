using Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ViewModel;

namespace FundEstimateWebClient
{
    internal class WebClientService
    {

        public async Task<PostResult> FundEstimateOut(StateViewModel<List<FundEstimateModel>> stateViewModel, string url)
        {
            PostResult resultObject=new PostResult();
            if (stateViewModel.Response.Count != 0)
            {
                var fundEstimateResponse = new
                {
                    Response = stateViewModel.Response
                };
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(60);
                var readyFrStatusJson = JsonConvert.SerializeObject(fundEstimateResponse);

                var result = client.PostAsync(url, new StringContent(readyFrStatusJson, Encoding.UTF8, "application/json")).Result;

                if (result.IsSuccessStatusCode)
                {
                    var postResult = await result.Content.ReadAsStringAsync();

                    resultObject = JsonConvert.DeserializeObject<PostResult>(postResult);                    
                }
            }
            return resultObject;
        }        
    }
}