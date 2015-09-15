using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Checkers.Services
{
    public class AuthorizeService
    {
        public async Task<string> AuthorizeServiceCall(string methodRequestType, string uriQuery, HttpContent postParams = null)
        {
            //string ServiceURI = "http://localhost:14658/AuthorizeService.svc/" + uriQuery;
            string ServiceURI = "http://gameservice.prilaga.by/AuthorizeService.svc/" + uriQuery + "?" + Guid.NewGuid();
            HttpClient httpClient = new HttpClient();
            HttpRequestMessage request = null;
            HttpResponseMessage response = null;
            switch (methodRequestType)
            {
                case "GET":
                    {
                        request = new HttpRequestMessage(HttpMethod.Get, ServiceURI);
                        response = await httpClient.SendAsync(request);
                        break;
                    }
                case "POST":
                    {
                        //response = await httpClient.PostAsync(ServiceURI, postParams);
                        request = new HttpRequestMessage(HttpMethod.Post, ServiceURI);
                        request.Content = postParams;
                        response = await httpClient.SendAsync(request);
                        break;
                    }
                default:
                    {
                        request = new HttpRequestMessage(HttpMethod.Get, ServiceURI);
                        response = await httpClient.SendAsync(request);
                        break;
                    }

            }
            var result = await response.Content.ReadAsStringAsync();
            return result;
        }
    }
}
