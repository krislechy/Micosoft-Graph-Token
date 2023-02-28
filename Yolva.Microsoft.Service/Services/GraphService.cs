using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yolva.MicrosoftServices
{
    public interface IGraphService
    {
        Task<byte[]?> getPhotoUser(string email, int? sizePhoto = null);
        Task<JToken?> getSpecificField(string email, params string[] fields);
        void setAccessToken(string accessToken);
    }
    public class GraphService: IGraphService
    {
        private HttpClient client;

        private string _accessToken;
        public string accessToken
        {
            get => _accessToken; 
            set
            {
                _accessToken = value;
                initClient();
            }
        }
        private const string link = "https://graph.microsoft.com/v1.0";
        public GraphService()
        {
            
            
        }
        public void setAccessToken(string accessToken) => this.accessToken = accessToken;
        private void initClient()
        {
            client = new HttpClient();
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        }
        public async Task<byte[]?> getPhotoUser(string email,int? sizePhoto=null)
        {
            if (String.IsNullOrEmpty(email?.Trim())) return null;
            var get=await client.GetAsync($"{link}/users/{email}/{(sizePhoto == null? "photo/$value":$"photos/{sizePhoto}x{sizePhoto}/$value")}");
            if(get.IsSuccessStatusCode)
            {
                var content = await get.Content.ReadAsByteArrayAsync();
                return content;
            }
            return null;
        }

        public async Task<JToken?> getSpecificField(string email, params string[] fields)
        {
            string selectedFields = null;
            if (fields != null && fields.Length > 0)
            {
                selectedFields = String.Join(",", fields);
                if (selectedFields.EndsWith(','))
                    selectedFields = selectedFields.Substring(0, selectedFields.Length - 1);
            }
            var get = await client.GetAsync($"{link}/users/{email}{(selectedFields == null ? String.Empty : $"/?$select={selectedFields}")}");
            if (get.IsSuccessStatusCode)
            {
                var content = await get.Content.ReadAsStringAsync();
                var json = JToken.Parse(content);
                return json;
            }
            return null;
        }
    }
}
