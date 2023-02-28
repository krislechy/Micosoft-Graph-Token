using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Yolva.MicrosoftServices
{
    public interface IAuthService
    {
        string getAccessToken();
    }
    public class AuthService : IAuthService
    {
        private const string microsoftLoginLink = "https://login.microsoftonline.com/";
        private readonly HttpClient client;
        private Guid tenantId { get; set; }
        private Guid clientId { get; set; }
        private string? thumbprint{ get; set; }
 
        public AuthService(
            Guid tenantId,
            Guid clientId,
            string? thumbprint
            )
        {
            this.tenantId = tenantId;
            this.clientId = clientId;
            this.thumbprint = thumbprint;

            client= new HttpClient();
        }
        (HttpStatusCode status, string? value, DateTime? expiredIn)? result;
        object sync=new object();
        public string getAccessToken()
        {
            lock (sync)
            {
                result ??= _getAccessToken().GetAwaiter().GetResult();

                if (DateTime.Now >= result.Value.expiredIn)
                    result = _getAccessToken().GetAwaiter().GetResult();
            }
            return result.Value.value;
        }
        private async Task<(HttpStatusCode status, string? value, DateTime? expiredIn)> _getAccessToken()
        {
            var requestToken = getRequestToken();
            if (requestToken == null) return (HttpStatusCode.NotFound, null, null);

            var body = new FormUrlEncodedContent(new Dictionary<string, string>()
            {
                { "scope","https://graph.microsoft.com/.default"},
                { "client_id",clientId.ToString()},
                { "client_assertion_type","urn:ietf:params:oauth:client-assertion-type:jwt-bearer"},
                { "client_assertion",requestToken},
                { "grant_type","client_credentials"},
            });
            var post = await client.PostAsync($"{microsoftLoginLink}/{tenantId}/oauth2/v2.0/token", body);
            if (post.IsSuccessStatusCode)
            {
                var content = await post.Content.ReadAsStringAsync();
                var json = JObject.Parse(content);
                var accessToken = json["access_token"]?.Value<string>();
                var expiredIn = json["expires_in"]?.Value<double>() -60;
                return (post.StatusCode, accessToken, expiredIn == null ? null : DateTime.Now.AddSeconds((double)expiredIn));
            }
            return default;
        }

        private string? getRequestToken()
        {
            var jwt = new JwtTokenRequest(DateTime.Now.AddDays(1), DateTime.Now, tenantId, clientId, thumbprint);
            string getContentFile(string path) => File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory+path);
            var requestToken = jwt.CreateToken(getContentFile("Keys\\publicKey.pem"), getContentFile("Keys\\privateKey.pem"));
            return requestToken;
        }
    }
}
