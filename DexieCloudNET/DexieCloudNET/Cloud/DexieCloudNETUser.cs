/*
DexieNETCloudUser.cs

Copyright(c) 2024 Bernhard Straub

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

namespace DexieCloudNET
{
    public enum GrantType
    {
        DEMO,
        OTP
    }

    public record LoginInformation
    (
        string EMail,
        string? UserId,
        GrantType GrantType
    );

    public enum LicenseType
    {
        DEMO,
        EVAL,
        PROD,
        CLIENT
    }

    public enum LicenseStatus
    {
        OK,
        EXPIRED,
        DEACTIVATED
    }

    public record License
    (
        LicenseType Type,
        LicenseStatus Status,
        DateTime? ValidUntil,
        int? EvalDaysLeft
    );

    public record UserLogin(string UserId, string? Name, string? EMail, Dictionary<string, string> Claims,
        License? License, DateTime LastLogin,
        string? AccessToken, DateTime? AccessTokenExpiration, string? RefreshToken, DateTime? RefreshTokenExpiration,
        string? NonExportablePrivateKey, string? PublicKey, bool IsLoggedIn);
}

