using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using System.Net.Http.Headers;
using TriangleProject.Shared.Models.Portelem;

namespace Portelem.Auth
{
    public class AuthHelper
    {
        private readonly NavigationManager _nav;
        private readonly IJSRuntime _Js;
        private readonly HttpClient _http;

        public AuthHelper(NavigationManager nav, IJSRuntime Js, HttpClient Http)
        {
            _nav = nav;
            _Js = Js;
            _http = Http;
        }

        public async Task<int> Check()
        {
            var getSystemId = await _http.GetAsync("api/Portelem/data");
            if (!getSystemId.IsSuccessStatusCode)
            {
                //no service Id OR Url in project
                return 0;
            }

            DataForSystem data = getSystemId.Content.ReadFromJsonAsync<DataForSystem>().Result;
            var PortelemResponse = await _http.GetAsync(data.Url + "api/Services/status/" + data.SystemId);

            if (!PortelemResponse.IsSuccessStatusCode)
            {
                //no service found - redirect to portelem main page
                _nav.NavigateTo(data.Url);
                return 0;
            }

            string systemStatus = PortelemResponse.Content.ReadAsStringAsync().Result;

            if (systemStatus != "QA" && systemStatus != "Complete") //Develop - no need cookie check
            {
                UserFromPortelem devUser = new UserFromPortelem();
                devUser.PortelemId = -1;
                var getUser = await _http.PostAsJsonAsync("api/Portelem/login/", devUser);
                if (getUser.IsSuccessStatusCode)
                    return getUser.Content.ReadFromJsonAsync<int>().Result;

                return 0;

            }
            
            var tokenRes = await _http.GetAsync("api/Portelem/token");
            if (!tokenRes.IsSuccessStatusCode)
            {
                _nav.NavigateTo(data.Url + "login/" + data.SystemId);
                return 0;
                //no cookie
            }

            string token = tokenRes.Content.ReadAsStringAsync().Result;
            //parse the token to claims
            IEnumerable<Claim> claims = ParseClaimsFromJwt(token);

            //get expire date of the token
            long exp = (long)Convert.ToDouble(claims.SingleOrDefault(c => c.Type == "exp").Value);
            long now = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
            //if token is not expired
            if (exp <= now)
            {
                _nav.NavigateTo(data.Url + "login/" + data.SystemId);
                return 0;
                //token is expired - redirect to portelem login
            }

            //get the portelemId from the token
            int portelemId = Convert.ToInt32(claims.SingleOrDefault(c => c.Type == "nameid").Value);

            //sent Http request to the portelem -> check if the user is logged in to portelem
            HttpResponseMessage checkPortelem;
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Get, data.Url + "api/services/students/" + data.SystemId + "/" + portelemId))
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                checkPortelem = await _http.SendAsync(requestMessage);
            }

            if (!checkPortelem.IsSuccessStatusCode)
            {
                _nav.NavigateTo(data.Url + "login/" + data.SystemId);
                return 0;
                //Unauthorize/no user - redirect to portelem login
            }
            UserFromPortelem studentUser = checkPortelem.Content.ReadFromJsonAsync<UserFromPortelem>().Result;
            var userRes = await _http.PostAsJsonAsync("api/Portelem/login", studentUser);
            if (userRes.IsSuccessStatusCode)
                return userRes.Content.ReadFromJsonAsync<int>().Result;

            else
            {
                Console.WriteLine("user creation/get error");
                return 0;
            }

        }


        private IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
        {
            var claims = new List<Claim>();
            var payload = jwt.Split('.')[1];

            var jsonBytes = ParseBase64WithoutPadding(payload);

            var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

            ExtractRolesFromJWT(claims, keyValuePairs);

            claims.AddRange(keyValuePairs.Select(kvp => new Claim(kvp.Key, kvp.Value.ToString())));

            return claims;
        }

        private void ExtractRolesFromJWT(List<Claim> claims, Dictionary<string, object> keyValuePairs)
        {
            keyValuePairs.TryGetValue(ClaimTypes.Role, out object roles);
            if (roles != null)
            {
                var parsedRoles = roles.ToString().Trim().TrimStart('[').TrimEnd(']').Split(',');
                if (parsedRoles.Length > 1)
                {
                    foreach (var parsedRole in parsedRoles)
                    {
                        claims.Add(new Claim(ClaimTypes.Role, parsedRole.Trim('"')));
                    }
                }
                else
                {
                    claims.Add(new Claim(ClaimTypes.Role, parsedRoles[0]));
                }
                keyValuePairs.Remove(ClaimTypes.Role);
            }
        }

        private byte[] ParseBase64WithoutPadding(string base64)
        {
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }
            return Convert.FromBase64String(base64);
        }
    }
}

