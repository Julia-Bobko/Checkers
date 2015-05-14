using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Checkers.Services
{
    public class GameService
    {
        //public async Task<string> OnlineGameServiceCall(string methodRequestType, string uriQuery, FormUrlEncodedContent postParams = null)
        //{
        //    string ServiceURI = "http://localhost:14658/OnlineGameService.svc/" + uriQuery;
        //    HttpClient httpClient = new HttpClient();
        //    HttpRequestMessage request = null;
        //    HttpResponseMessage response = null;
        //    switch (methodRequestType)
        //    {
        //        case "GET":
        //            {
        //                request = new HttpRequestMessage(HttpMethod.Get, ServiceURI);
        //                response = await httpClient.SendAsync(request);
        //                break;
        //            }
        //        case "POST":
        //            {
        //                response = await httpClient.PostAsync(ServiceURI, postParams);
        //                //request = new HttpRequestMessage(HttpMethod.Post, ServiceURI);
        //                //request.Content = postParams;
        //                //response = await httpClient.SendAsync(request);
        //                break;
        //            }
        //        default:
        //            {
        //                request = new HttpRequestMessage(HttpMethod.Get, ServiceURI);
        //                response = await httpClient.SendAsync(request);
        //                break;
        //            }

        //    }
        //    var result = await response.Content.ReadAsStringAsync();
        //    return result;
        //}

        public async Task<string> OnlineGameServiceCall(string methodRequestType, string uriQuery, HttpContent postParams = null)
        {
            //string ServiceURI = "http://localhost:14658/OnlineGameService.svc/" + uriQuery;
            string ServiceURI = "http://gameservice.prilaga.by/OnlineGameService.svc/" + uriQuery + "?" + Guid.NewGuid();
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
