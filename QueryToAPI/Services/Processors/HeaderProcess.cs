using QueryToAPI.Models;
using RestSharp;
using System.Collections.Generic;

namespace QueryToAPI.Services.Processors
{
    public class HeaderProcess
    {
        public static void Process(List<Header> headers, RestRequest request)
        {
            if (headers != null && headers.Count > 0)
                headers.ForEach(x =>
                    request.AddHeader(x.Name, x.Value)
                );
            request.AddHeader("Accept", "application/json");
            request.AddHeader("Content-Type", "application/json");
        }
    }
}
