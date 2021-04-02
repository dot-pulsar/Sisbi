using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace Sisbi.Services
{
    public class VkontakteService
    {
        public HttpClient Client { get; }

        public VkontakteService(HttpClient client)
        {
            client.BaseAddress = new Uri("https://api.vk.com/oauth/");
            // GitHub API versioning
            client.DefaultRequestHeaders.Add("Accept",
                "text/html; charset=utf-8");
            // GitHub requires a user-agent
            /*client.DefaultRequestHeaders.Add("User-Agent",
                "HttpClientFactory-Sample");*/

            Client = client;
        }

        public async Task<string> Get()
        {
            var clientId = "7799405";
            var scope = "offline";
            var redirectUri = "https://localhost:5001/account/response";
            var display = "popup";
            var responseType = "code";
            //var version = "5.60";
            var revoke = "1";
            var uri =
                $"https://oauth.vk.com/authorize?client_id={clientId}&scope={scope}&display={display}&redirect_uri={redirectUri}&response_type={responseType}&revoke={revoke}";
            var resp = await Client.GetAsync(uri);

            return await resp.Content.ReadAsStringAsync();
        }
    }
}