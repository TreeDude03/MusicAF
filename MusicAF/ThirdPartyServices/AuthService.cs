using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static Google.Apis.Requests.BatchRequest;

namespace MusicAF.ThirdPartyServices
{
    internal class AuthService
    {
        private static readonly string ApiKey = "AIzaSyDX9VYUOnkEI62tTdvbgw7TZijK806yGEg";
        private static readonly string BaseUrl = "https://identitytoolkit.googleapis.com/v1/accounts:";

        // Singleton instance
        private static AuthService _instance;
        public static AuthService Instance => _instance ??= new AuthService();

        // Sign-in method
        public async Task<AuthStatus> SignInWithEmailAndPassword(string email, string password)
        {
            using var client = new HttpClient();
            var requestData = new
            {
                email,
                password,
                returnSecureContent = true
            };

            var content = new StringContent(JsonSerializer.Serialize(requestData), Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"{BaseUrl}signInWithPassword?key={ApiKey}", content);

            if (response.IsSuccessStatusCode)
            {
                var responseData = await response.Content.ReadAsStringAsync();

                // Parse the JSON response
                JObject temp = JObject.Parse(responseData);
                // Extract the idToken
                string accessToken = temp["idToken"]?.ToString();
                Console.WriteLine(responseData);
                //
                return new AuthStatus
                {
                    Successful = true,
                    Content = accessToken
                };
            }
            else
            {
                var responseData = await response.Content.ReadAsStringAsync();
                Console.WriteLine(responseData);

                return new AuthStatus
                {
                    Successful = false,
                    Content = response.ReasonPhrase
                };
            }
        }

        // Sign-up method
        public async Task<AuthStatus> SignUpWithEmailAndPassword(string email, string password)
        {
            using var client = new HttpClient();
            var requestData = new
            {
                email,
                password,
                returnSecureContent = true
            };

            var content = new StringContent(JsonSerializer.Serialize(requestData), Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"{BaseUrl}signUp?key={ApiKey}", content);

            if (response.IsSuccessStatusCode)
            {
                var responseData = await response.Content.ReadAsStringAsync();

                // Parse the JSON response
                JObject temp = JObject.Parse(responseData);
                // Extract the idToken
                string accessToken = temp["idToken"]?.ToString();
                Console.WriteLine(responseData);
                //
                return new AuthStatus
                {
                    Successful = true,
                    Content = accessToken
                };
            }
            else
            {
                var responseData = await response.Content.ReadAsStringAsync();
                Console.WriteLine(responseData);
                return new AuthStatus
                {
                    Successful = false,
                    Content = response.ReasonPhrase
                };
            }
        }
    }
    internal class AuthStatus
    {
        public bool Successful { get; set;}
        public string Content { get; set; }
    }
}
