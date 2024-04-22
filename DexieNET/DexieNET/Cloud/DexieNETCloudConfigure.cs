/*
DexieNETCloud.cs

Copyright(c) 2023 Bernhard Straub

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

'DexieNET' used with permission of David Fahlander 
*/

namespace DexieNET
{
    public record TokenHint(string? UserdID, string? EMail);
    public record TokenParams(string Public_key, TokenHint? Hint);
    public record TokenResponseClaim(string ClaimName);
    public record TokenResponseClaims(string Sub, TokenResponseClaim[] Claims);

    public record TokenAlerts(
        string Type,
        string MessageCode,
        string Message,
        Dictionary<string, string>? MessageParams = null);

    public record TokenFinalResponse(
        string Type,
        TokenResponseClaim Claims,
        string AccessToken,
        double AccessTokenExpiration,
        string? RefreshToken = null,
        double? RefreshTokenExpiration = null,
        TokenAlerts? Alerts = null);

    public record PeriodicSyncOptions(double MinInterval);

    public record DexieCloudOptions(
        string DatabaseUrl,
        bool RequireAuth = true,
        bool? TryUseServiceWorker = null,
        PeriodicSyncOptions? PeriodicSync = null,
        bool? CustomLoginGui = null,
        string[]? UnsyncedTables = null,
        bool? NameSuffix = true,
        bool? DisableWebSocket = null,
        bool? DisableEagerSync = null,
        Func<TokenParams, ValueTask<TokenFinalResponse>>? FetchTokens = null)
    {
        public DexieCloudOptions WithRequireAuth(bool requireAuth) => this with { RequireAuth = requireAuth };
        public DexieCloudOptions WithTryUseServiceWorker(bool tryServiceWorker) => this with { TryUseServiceWorker = tryServiceWorker };
        public DexieCloudOptions WithPeriodicSync(PeriodicSyncOptions periodicSync) => this with { PeriodicSync = periodicSync };
        public DexieCloudOptions WithCustomLoginGui(bool customLoginGui) => this with { CustomLoginGui = customLoginGui };
        public DexieCloudOptions WithUnsyncedTables(string[] unsyncedTables) => this with { UnsyncedTables = unsyncedTables };
        public DexieCloudOptions WithNameSuffix(bool nameSuffix) => this with { NameSuffix = nameSuffix };
        public DexieCloudOptions WithDisableWebSocket(bool disableWebSocket) => this with { DisableWebSocket = disableWebSocket };
        public DexieCloudOptions WithFetchTokens(Func<TokenParams, ValueTask<TokenFinalResponse>> fetchTokens) => this with { FetchTokens = fetchTokens };
    }

    public record DexieCloudSchema(
        bool? GeneratedGlobalId,
        string? IdPrefix,
        bool? Deleted,
        bool? MarkedForSync,
        bool? InitiallySynced,
        string PrimaryKey
    );
}
