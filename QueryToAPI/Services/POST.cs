using System.Collections.Generic;
using QueryToAPI.Models;
using Newtonsoft.Json;
using RestSharp;
using QueryToAPI.Services.Processors;

namespace QueryToAPI.Services
{
    partial class Service
    {
        public string POST(string url, string resource, object value, List<Header> headers)
        {
            //var dataJson = JsonConvert.SerializeObject(value);
            var dataJson = value.ToString();
            RestClient client = new RestClient(url);
            RestRequest request = new RestRequest(resource, Method.POST);
            HeaderProcess.Process(headers, request);
            request.AddParameter("application/json", dataJson, ParameterType.RequestBody);
            var res = client.Execute(request);
            return res.Content;
        }

  

        public string POST(string url, string resource, List<Header> headers)
        {
            RestClient client = new RestClient(url);
            RestRequest request = new RestRequest(resource, Method.POST);
            HeaderProcess.Process(headers, request);
            var res = client.Execute(request);
            return res.Content;
        }
    }
}
