using System.Collections.Generic;
using QueryToAPI.Models;
using Newtonsoft.Json;
using RestSharp;
using QueryToAPI.Services.Processors;


namespace QueryToAPI.Services
{
    partial class Service
    {
        public string GET(string url, string resource, object value, List<Header> headers)
        {
            var dataJson = JsonConvert.SerializeObject(value);
            RestClient client = new RestClient(url);
            RestRequest request = new RestRequest(resource, Method.GET);
            HeaderProcess.Process(headers, request);
            request.AddParameter("application/json", dataJson, ParameterType.RequestBody);
            var res = client.Execute(request);
            return res.Content;
        }
        public string GET(string url, string resource, List<Header> headers)
        {

            RestClient client = new RestClient(url);
            RestRequest request = new RestRequest(resource, Method.GET);
            HeaderProcess.Process(headers, request);
            var res = client.Execute(request);
            return res.Content;
        }
    }
}
