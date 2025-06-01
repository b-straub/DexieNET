/*
DexieNETCloud.cs

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

using Microsoft.JSInterop;
using DexieNET;
using R3;

// ReSharper disable once CheckNamespace
namespace DexieCloudNET
{
    public record DBCloudEntity(string Owner, string RealmId) : IDBCloudEntity
    {
        public string EntityKey => Owner + RealmId;
    }

    public sealed class DBCloudFetchTokens
    {
        public DotNetObjectReference<DBCloudFetchTokens> DotnetRef { get; }

        private readonly Func<TokenParams, Task<TokenFinalResponse?>> _fetchTokens;

        [JSInvokable]
        public async Task<TokenFinalResponse?> FetchTokens(TokenParams tokenParams)
        {
            return await _fetchTokens(tokenParams);
        }

        public DBCloudFetchTokens(Func<TokenParams, Task<TokenFinalResponse?>> fetchTokens)
        {
            DotnetRef = DotNetObjectReference.Create(this);
            _fetchTokens = fetchTokens;
        }

        ~DBCloudFetchTokens()
        {
            DotnetRef.Dispose();
        }
    }

    public static partial class DBCloudExtensions
    {
        public static async Task ConfigureCloud(this DBBase dexie, DexieCloudOptions cloudOptions, string? pushURL = null,
            string? applicationServerKey = null)
        {
            if (!dexie.HasCloud())
            {
                throw new InvalidOperationException("Can not ConfigureCloud for non cloud database.");
            }

            if (dexie.HasPushSupport && applicationServerKey is null)
            {
                throw new InvalidOperationException(
                    "Database has push support but not 'ApplicationServerKey' has been provided.");
            }

            if (!dexie.HasPushSupport && applicationServerKey is not null)
            {
                throw new InvalidOperationException(
                    "Database has no push support but 'ApplicationServerKey' has been provided.");
            }

            if (cloudOptions.UnsyncedTables is null)
            {
                cloudOptions = cloudOptions
                    .WithUnsyncedTables(dexie.UnsyncedTables);
            }

            DotNetObjectReference<DBCloudFetchTokens>? dotnetRef = null;

            if (cloudOptions.FetchTokens is not null)
            {
                var fetchTokensWrapper = new DBCloudFetchTokens(cloudOptions.FetchTokens);
                dotnetRef = fetchTokensWrapper.DotnetRef;
                cloudOptions = cloudOptions.WithFetchTokens(null);
            }

            var err = await dexie.Cloud.Module.InvokeAsync<string?>("ConfigureCloud", dexie.Cloud.Reference, cloudOptions,
                pushURL, applicationServerKey, dotnetRef);

            if (err is not null)
            {
                throw new InvalidOperationException(err);
            }
        }
        
        #region Sharing

        public static string GetTiedRealmID(this DBBase dexie, string id)
        {
            if (!dexie.HasCloud())
            {
                throw new InvalidOperationException("Can not call GetTiedRealmID for non cloud database.");
            }

            return dexie.Cloud.Module.Invoke<string>("GetTiedRealmID", id);
        }

        #endregion

        #region UIInteraction

        public static Observable<UIInteraction> UserInteractionObservable(this DBBase dexie)
        {
            return JSObservable<UIInteraction>.Create(dexie, "SubscribeUserInteraction", "ClearUserInteraction").AsObservable;
        }

        public static void SubmitUserInteraction(this DBBase dexie, UIInteraction interaction, UIParam param)
        {
            if (!dexie.HasCloud())
            {
                throw new InvalidOperationException("Can not ConfigureCloud for non cloud database.");
            }

            dexie.Cloud.Module.InvokeVoid("OnSubmitUserInteraction", interaction.Key, param.AsDictionary());
        }

        public static void SubmitUserInteraction(this DBBase dexie, UIInteraction interaction,
            Dictionary<string, string> param)
        {
            if (!dexie.HasCloud())
            {
                throw new InvalidOperationException("Can not ConfigureCloud for non cloud database.");
            }

            dexie.Cloud.Module.InvokeVoid("OnSubmitUserInteraction", interaction.Key, param);
        }

        public static void CancelUserInteraction(this DBBase dexie, UIInteraction interaction)
        {
            if (!dexie.HasCloud())
            {
                throw new InvalidOperationException("Can not ConfigureCloud for non cloud database.");
            }

            dexie.Cloud.Module.InvokeVoid("OnCancelUserInteraction", interaction.Key);
        }

        #endregion

        #region Invites

        public static Observable<IEnumerable<Invite>> InvitesObservable(this DBBase dexie)
        {
            return JSObservable<IEnumerable<Invite>>.Create(dexie, "SubscribeInvites", "ClearInvites").AsObservable;
        }

        public static void AcceptInvite(this DBBase dexie, Invite invite)
        {
            if (!dexie.HasCloud())
            {
                throw new InvalidOperationException("Can not ConfigureCloud for non cloud database.");
            }

            dexie.Cloud.Module.InvokeVoid("AcceptInvite", invite.Id);
        }

        public static async ValueTask AcceptInvite(this DBBase dexie, Member member)
        {
            if (!dexie.HasCloud())
            {
                throw new InvalidOperationException("Can not ConfigureCloud for non cloud database.");
            }

            await dexie.Cloud.Module.InvokeVoidAsync("AcceptInviteMember", dexie.Cloud.Reference, member.Id);
        }

        public static void RejectInvite(this DBBase dexie, Invite invite)
        {
            if (!dexie.HasCloud())
            {
                throw new InvalidOperationException("Can not ConfigureCloud for non cloud database.");
            }

            dexie.Cloud.Module.InvokeVoid("RejectInvite", invite.Id);
        }

        public static async ValueTask RejectInvite(this DBBase dexie, Member member)
        {
            if (!dexie.HasCloud())
            {
                throw new InvalidOperationException("Can not ConfigureCloud for non cloud database.");
            }

            await dexie.Cloud.Module.InvokeVoidAsync("RejectInviteMember", dexie.Cloud.Reference, member.Id);
        }

        public static async ValueTask ClearInvite(this DBBase dexie, Member member)
        {
            if (!dexie.HasCloud())
            {
                throw new InvalidOperationException("Can not ConfigureCloud for non cloud database.");
            }

            await dexie.Cloud.Module.InvokeVoidAsync("ClearInviteMember", dexie.Cloud.Reference, member.Id);
        }

        #endregion

        #region Roles

        public static Observable<Dictionary<string, Role>> RoleObservable(this DBBase dexie)
        {
            if (!dexie.HasCloud())
            {
                throw new InvalidOperationException("Can not ConfigureCloud for non cloud database.");
            }

            return JSObservable<Dictionary<string, Role>>.Create(dexie, "SubscribeRoles").AsObservable;
        }

        #endregion

        #region SyncState

        public static Observable<SyncState> SyncStateObservable(this DBBase dexie)
        {
            if (!dexie.HasCloud())
            {
                throw new InvalidOperationException("Can not ConfigureCloud for non cloud database.");
            }

            return JSObservable<SyncState>.Create(dexie, "SubscribeSyncState").AsObservable;
        }

        public static Observable<PersistedSyncState> PersistedSyncStateStateObservable(this DBBase dexie)
        {
            if (!dexie.HasCloud())
            {
                throw new InvalidOperationException("Can not ConfigureCloud for non cloud database.");
            }

            return JSObservable<PersistedSyncState>.Create(dexie, "SubscribePersistedSyncState").AsObservable;
        }

        public static async ValueTask Sync(this DBBase dexie, SyncOptions? syncOptions = null)
        {
            if (!dexie.HasCloud())
            {
                throw new InvalidOperationException("Can not ConfigureCloud for non cloud database.");
            }

            await dexie.Cloud.Module.InvokeVoidAsync("Sync", dexie.Cloud.Reference, syncOptions);
        }

        public static Observable<bool> SyncCompleteObservable(this DBBase dexie,
            TimeOut<bool>? timeout = null)
        {
            return JSObservable<bool>.Create(dexie, "SubscribeSyncComplete", timeout).AsObservable;
        }

        #endregion

        #region WebSocketStatus

        public static Observable<string> WebSocketStatusObservable(this DBBase dexie)
        {
            return JSObservable<string>.Create(dexie, "SubscribeWebSocketStatus").AsObservable;
        }

        #endregion

        #region UserLogin

        public static Observable<UserLogin> UserLoginObservable(this DBBase dexie)
        {
            return JSObservable<UserLogin>.Create(dexie, "SubscribeUserLogin").AsObservable;
        }

        public static string CurrentUserId(this DBBase dexie)
        {
            if (!dexie.HasCloud())
            {
                throw new InvalidOperationException("Can not ConfigureCloud for non cloud database.");
            }

            return dexie.Cloud.Module.Invoke<string>("CurrentUserId", dexie.Cloud.Reference);
        }

        public static string AddOnVersion(this DBBase dexie)
        {
            if (!dexie.HasCloud())
            {
                throw new InvalidOperationException("Can not ConfigureCloud for non cloud database.");
            }

            return dexie.Cloud.Module.Invoke<string>("AddOnVersion", dexie.Cloud.Reference);
        }

        public static DexieCloudOptions Options(this DBBase dexie)
        {
            if (!dexie.HasCloud())
            {
                throw new InvalidOperationException("Can not ConfigureCloud for non cloud database.");
            }

            return dexie.Cloud.Module.Invoke<DexieCloudOptions>("Options", dexie.Cloud.Reference);
        }

        public static Dictionary<string, DexieCloudSchema>? Schema(this DBBase dexie)
        {
            if (!dexie.HasCloud())
            {
                throw new InvalidOperationException("Can not ConfigureCloud for non cloud database.");
            }

            return dexie.Cloud.Module.Invoke<Dictionary<string, DexieCloudSchema>?>("Schema", dexie.Cloud.Reference);
        }

        public static bool? UsingServiceWorker(this DBBase dexie)
        {
            if (!dexie.HasCloud())
            {
                throw new InvalidOperationException("Can not ConfigureCloud for non cloud database.");
            }

            return dexie.Cloud.Module.Invoke<bool?>("UsingServiceWorker", dexie.Cloud.Reference);
        }

        // returns http error if any
        public static async ValueTask<string?> UserLogin(this DBBase dexie, LoginInformation userLogin)
        {
            if (!dexie.HasCloud())
            {
                throw new InvalidOperationException("Can not ConfigureCloud for non cloud database.");
            }

            var grantType = userLogin.GrantType switch
            {
                GrantType.DEMO => "demo",
                GrantType.OTP => "otp",
                _ => throw new InvalidOperationException("GrantType: Invalid type!")
            };

            return await dexie.Cloud.Module.InvokeAsync<string?>("UserLogin", dexie.Cloud.Reference, userLogin.EMail,
                grantType, userLogin.UserId);
        }

        public static async Task Logout(this DBBase dexie, bool force = false)
        {
            if (!dexie.HasCloud())
            {
                throw new InvalidOperationException("Can not ConfigureCloud for non cloud database.");
            }

            await dexie.Cloud.Module.InvokeVoidAsync("Logout", dexie.Cloud.Reference, force);
        }

        public static async Task<long> NumUnsyncedChanges(this DBBase dexie)
        {
            if (!dexie.HasCloud())
            {
                throw new InvalidOperationException("Can not ConfigureCloud for non cloud database.");
            }

            return await dexie.Cloud.Module.InvokeAsync<long>("NumUnsyncedChanges", dexie.Cloud.Reference);
        }

        #endregion
        
        #region Table

        public static IUsePermissions<T> CreateUsePermissions<T, I>(this Table<T, I> table)
            where T : IDBStore, IDBCloudEntity
        {
            return UsePermissions<T, I>.Create(table);
        }

        #endregion
    }
}