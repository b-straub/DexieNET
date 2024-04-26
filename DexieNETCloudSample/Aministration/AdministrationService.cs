using DexieNETCloudSample.Logic;
using RxBlazorLightCore;
using System.Text.Json;
using System.Text;
using DexieNET;
using DexieCloudNET;

namespace DexieNETCloudSample.Aministration
{
    public sealed partial class AdministrationService(IServiceProvider serviceProvider) : RxBLService
    {
        public List<UserResponse> Users { get; } = [];
        public DexieCloudService DBService { get; } = serviceProvider.GetRequiredService<DexieCloudService>();

        private readonly HttpClient _httpClient = serviceProvider.GetRequiredService<HttpClient>();

        private async Task DoGetUsers(CloudKeyData data, CancellationToken cancellationToken)
        {
            var body = new ClientCredentialsTokenRequest([DBScopes.AccessDB, DBScopes.GlobalRead, DBScopes.GlobalWrite], data.ClientId, data.ClientSecret);
            var bodyJson = JsonSerializer.Serialize(body, ClientCredentialsTokenRequestContext.Default.ClientCredentialsTokenRequest);

            using StringContent jsonContent = new(bodyJson, Encoding.UTF8, "application/json");
            using HttpResponseMessage tokenResponse = await _httpClient.PostAsync($"{DBService.CloudURL}/token", jsonContent, cancellationToken);

            if (!tokenResponse.IsSuccessStatusCode)
            {
                throw new InvalidOperationException("Administration - Can not get token!");
            }
            else
            {
                var jsonStringToken = await tokenResponse.Content.ReadAsStringAsync(cancellationToken);
                var accessToken = JsonSerializer.Deserialize(jsonStringToken, TokenFinalResponseContext.Default.TokenFinalResponse);

                var token = accessToken?.AccessToken;

                using var request = new HttpRequestMessage(HttpMethod.Get,
                $"{DBService.CloudURL}/users");
                request.Headers.Add("Authorization", $"Bearer {token}");

                using var userResponse = await _httpClient.SendAsync(request, cancellationToken);

                if (!userResponse.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException("Administration - Can not get users!");
                }
                else
                {
                    var jsonStringUser = await userResponse.Content.ReadAsStringAsync(cancellationToken);
                    var usersResponse = JsonSerializer.Deserialize(jsonStringUser, UsersResponseContext.Default.UsersResponse);

                    if (usersResponse is not null)
                    {
                        Users.Clear();
                        Users.AddRange(usersResponse.Data);
                    }
                }
            }
        }

        private async Task DoDeleteUser(CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(DBService.UserLogin.Value);

            using var request = new HttpRequestMessage(HttpMethod.Delete,
                $"{DBService.CloudURL}/users/{DBService.UserLogin.Value.UserId}");
                request.Headers.Add("Authorization", $"Bearer {DBService.UserLogin.Value.AccessToken}");

            using var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException("Delete user - User could not be deleted!");
            }
            else
            {
                await DBService.Logout(true);
            }
        }

        public async Task<TokenFinalResponse?> GetUserCredentials(CloudKeyData data, TokenParams tokenParams, CancellationToken cancellationToken)
        { 
            ArgumentNullException.ThrowIfNull(tokenParams.Hints?.EMail);
            var name = tokenParams.Hints.EMail.Split('@').First();
            var claims = new TokenRequestClaims(tokenParams.Hints.EMail, tokenParams.Hints.EMail, name);
            var body = new ClientCredentialsTokenRequest([DBScopes.AccessDB],
                data.ClientId, data.ClientSecret, tokenParams.Public_key, claims);
            var bodyJson = JsonSerializer.Serialize(body, ClientCredentialsTokenRequestContext.Default.ClientCredentialsTokenRequest);

            using StringContent jsonContent = new(bodyJson, Encoding.UTF8, "application/json");
            using HttpResponseMessage tokenResponse = await _httpClient.PostAsync($"{DBService.CloudURL}/token", jsonContent, cancellationToken);

            if (!tokenResponse.IsSuccessStatusCode)
            {
                Console.WriteLine("Administration - Can not get token!");
            }
            else
            {
                var jsonStringToken = await tokenResponse.Content.ReadAsStringAsync(cancellationToken);
                var accessToken = JsonSerializer.Deserialize(jsonStringToken, TokenFinalResponseContext.Default.TokenFinalResponse);
                return accessToken;
            }

            return null;
        }
    }
}
