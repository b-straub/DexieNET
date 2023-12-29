using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using DexieNETCloudSample.Dialogs;
using DexieNETCloudSample.Extensions;
using DexieNETCloudSample.Logic;
using DexieNET;

namespace DexieNETCloudSample.Pages
{
    public partial class Administration
    {
        [Inject]
        private IDialogService? DialogService { get; set; }

        [Inject]
        private HttpClient? HttpClient { get; set; }

        [Inject]
        private IConfiguration? Configuration { get; set; }

        private static readonly string[] _scopesArray = ["GLOBAL_READ"];
        private readonly List<UserResponse> _users = [];
        private bool _requestRunning = false;
        private readonly string[] _headers = ["UserId", "LastLogin", "LicenseType", "EvalDaysLeft"];

        private async Task GetUsers()
        {
            ArgumentNullException.ThrowIfNull(DialogService);
            ArgumentNullException.ThrowIfNull(HttpClient);

            CloudKeyData data = new("clientId", "clientSecret");

            var parameters = new DialogParameters { ["Item"] = data };
            var dialog = DialogService.Show<GetClientKeys>("Cloud Client Keys", parameters);

            var result = await dialog.Result;
            if (!result.Canceled)
            {
                _requestRunning = true;
                await InvokeAsync(StateHasChanged);

                data = (CloudKeyData)result.Data;
                var cloudURL = Configuration.GetDBUrl();

                var body = new AccesssTokenRequest([DBScopes.AccessDB, DBScopes.GlobalRead, DBScopes.GlobalWrite], data.ClientId, data.ClientSecret);
                var bodyJson = JsonSerializer.Serialize(body, AccesssTokenRequestContext.Default.AccesssTokenRequest);

                using StringContent jsonContent = new(bodyJson, Encoding.UTF8, "application/json");
                using HttpResponseMessage tokenResponse = await HttpClient.PostAsync($"{cloudURL}/token", jsonContent);

                if (!tokenResponse.IsSuccessStatusCode)
                {
                    await DialogService.ShowMessageBox("Administration", "Can not get token!");
                }
                else
                {
                    var jsonStringToken = await tokenResponse.Content.ReadAsStringAsync();
                    var accessToken = JsonSerializer.Deserialize(jsonStringToken, AccesssTokenResponseContext.Default.AccesssTokenResponse);

                    var token = accessToken?.AccessToken;

                    using var request = new HttpRequestMessage(HttpMethod.Get,
                    $"{cloudURL}/users");
                    request.Headers.Add("Authorization", $"Bearer {token}");

                    using var userResponse = await HttpClient.SendAsync(request);

                    if (!userResponse.IsSuccessStatusCode)
                    {
                        await DialogService.ShowMessageBox("Administration", "Can not get users!");
                    }
                    else
                    {
                        var jsonStringUser = await userResponse.Content.ReadAsStringAsync();
                        var usersResponse = JsonSerializer.Deserialize(jsonStringUser, UsersResponseContext.Default.UsersResponse);

                        if (usersResponse is not null)
                        {
                            _users.Clear();
                            _users.AddRange(usersResponse.Data);
                        }

                        /*var user = users?.Data.FirstOrDefault();
                        if (user is not null && user.EvalDaysLeft != 5)
                        {
                            var updatedUser = new UserRequest(user.UserId, EvalDaysLeft: 5);

                            var userBodyJson = $"[{JsonSerializer.Serialize(updatedUser, UserRequestContext.Default.UserRequest)}]";

                            using StringContent jsonContentUser = new(userBodyJson, Encoding.UTF8, "application/json");

                            using var updateRequest = new HttpRequestMessage(HttpMethod.Post,
                                $"{cloudURL}/users")
                            {
                                Content = jsonContentUser
                            };
                            updateRequest.Headers.Add("Authorization", $"Bearer {token}");

                            using HttpResponseMessage userUpdateResponse = await HttpClient.SendAsync(updateRequest);
                        }*/
                    }
                }
                _requestRunning = false;
                await InvokeAsync(StateHasChanged);
            }
        }
    }
}
