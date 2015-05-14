using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Checkers.Services
{
    public class AuthorizeService
    {
        public async Task<string> AuthorizeServiceCall(string methodRequestType, string uriQuery, FormUrlEncodedContent postParams = null)
        {
            //string ServiceURI = "http://localhost:14658/AuthorizeService.svc/" + uriQuery;
            string ServiceURI = "http://gameservice.prilaga.by/AuthorizeService.svc/" + uriQuery + "?" + Guid.NewGuid();
            HttpClient httpClient = new HttpClient();
            HttpRequestMessage request = null;
            HttpResponseMessage response = null;
            request = new HttpRequestMessage(HttpMethod.Get, ServiceURI);
            response = await httpClient.SendAsync(request);
            var result = await response.Content.ReadAsStringAsync();
            return result;
        }
    }
}
