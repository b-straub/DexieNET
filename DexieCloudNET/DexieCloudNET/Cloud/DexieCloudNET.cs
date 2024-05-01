﻿/*
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

using Microsoft.JSInterop;
using DexieNET;

namespace DexieCloudNET
{
    public interface IDBCloudEntity
    {
        string? Owner { get; }
        string? RealmId { get; }

        public string? EntityKey => GetEntityKey();

        private string? GetEntityKey()
        {
            if (Owner is null || RealmId is null)
            {
                return null;
            }

            return Owner + RealmId;
        }
    }

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
    }

    public static partial class DBCloudExtensions
    {
        public static void ConfigureCloud(this DBBase dexie, DexieCloudOptions cloudOptions)
        {
            if (!dexie.CloudSync)
            {
                throw new InvalidOperationException("Can not ConfigureCloud for non cloud database.");
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

            //var jsi = cloudOptions.FromObject();    

            var err = dexie.DBBaseJS.Module.Invoke<string?>("ConfigureCloud", dexie.DBBaseJS.Reference, cloudOptions, dotnetRef);

            if (err is not null)
            {
                throw new InvalidOperationException(err);
            }
        }

        #region Sharing
        public static string GetTiedRealmID(this DBBase dexie, string id)
        {
            if (!dexie.CloudSync)
            {
                throw new InvalidOperationException("Can not call GetTiedRealmID for non cloud database.");
            }

            return dexie.DBBaseJS.Module.Invoke<string>("GetTiedRealmID", id);
        }
        #endregion

        #region UIInteraction
        public static IObservable<UIInteraction> UserInteractionObservable(this DBBase dexie)
        {
            dexie.TestCloudSync();
            var jso = new JSObservable<UIInteraction>(dexie, "SubscribeUserInteraction", "ClearUserInteraction");
            return jso;
        }

        public static void SubmitUserInteraction(this DBBase dexie, UIInteraction interaction, UIParam param)
        {
            dexie.TestCloudSync();
            dexie.DBBaseJS.Module.InvokeVoid("OnSubmitUserInteraction", interaction.Key, param.AsDictionary());
        }

        public static void SubmitUserInteraction(this DBBase dexie, UIInteraction interaction, Dictionary<string, string> param)
        {
            dexie.TestCloudSync();
            dexie.DBBaseJS.Module.InvokeVoid("OnSubmitUserInteraction", interaction.Key, param);
        }

        public static void CancelUserInteraction(this DBBase dexie, UIInteraction interaction)
        {
            dexie.TestCloudSync();
            dexie.DBBaseJS.Module.InvokeVoid("OnCancelUserInteraction", interaction.Key);
        }
        #endregion

        #region Invites
        public static IObservable<IEnumerable<Invite>> InvitesObservable(this DBBase dexie)
        {
            dexie.TestCloudSync();
            var jso = new JSObservable<IEnumerable<Invite>>(dexie, "SubscribeInvites", "ClearInvites");
            return jso;
        }

        public static void AcceptInvite(this DBBase dexie, Invite invite)
        {
            dexie.TestCloudSync();
            dexie.DBBaseJS.Module.InvokeVoid("AcceptInvite", invite.Id);
        }

        public static async ValueTask AcceptInvite(this DBBase dexie, Member member)
        {
            dexie.TestCloudSync();
            await dexie.DBBaseJS.Module.InvokeVoidAsync("AcceptInviteMember", dexie.DBBaseJS.Reference, member.Id);
        }

        public static void RejectInvite(this DBBase dexie, Invite invite)
        {
            dexie.TestCloudSync();
            dexie.DBBaseJS.Module.InvokeVoid("RejectInvite", invite.Id);
        }

        public static async ValueTask RejectInvite(this DBBase dexie, Member member)
        {
            dexie.TestCloudSync();
            await dexie.DBBaseJS.Module.InvokeVoidAsync("RejectInviteMember", dexie.DBBaseJS.Reference, member.Id);
        }

        public static async ValueTask ClearInvite(this DBBase dexie, Member member)
        {
            dexie.TestCloudSync();
            await dexie.DBBaseJS.Module.InvokeVoidAsync("ClearInviteMember", dexie.DBBaseJS.Reference, member.Id);
        }
        #endregion

        #region Roles
        public static IObservable<Dictionary<string, Role>> RoleObservable(this DBBase dexie)
        {
            dexie.TestCloudSync();
            return new JSObservable<Dictionary<string, Role>>(dexie, "SubscribeRoles");
        }
        #endregion

        #region SyncState
        public static IObservable<SyncState> SyncStateObservable(this DBBase dexie)
        {
            dexie.TestCloudSync();
            return new JSObservable<SyncState>(dexie, "SubscribeSyncState");
        }

        public static IObservable<PersistedSyncState> PersistedSyncStateStateObservable(this DBBase dexie)
        {
            dexie.TestCloudSync();
            return new JSObservable<PersistedSyncState>(dexie, "SubscribePersistedSyncState");
        }

        public static async ValueTask Sync(this DBBase dexie, SyncOptions syncOptions)
        {
            dexie.TestCloudSync();
            await dexie.DBBaseJS.Module.InvokeVoidAsync("Sync", dexie.DBBaseJS.Reference, syncOptions);
        }

        public static IObservable<bool> SyncCompleteObservable(this DBBase dexie)
        {
            dexie.TestCloudSync();
            return new JSObservable<bool>(dexie, "SubscribeSyncComplete");
        }
        #endregion

        #region WebSocketStatus
        public static IObservable<string> WebSocketStatusObservable(this DBBase dexie)
        {
            dexie.TestCloudSync();
            return new JSObservable<string>(dexie, "SubscribeWebSocketStatus");
        }
        #endregion

        #region UserLogin
        public static IObservable<UserLogin> UserLoginObservable(this DBBase dexie)
        {
            dexie.TestCloudSync();
            return new JSObservable<UserLogin>(dexie, "SubscribeUserLogin");
        }

        public static string CurrentUserId(this DBBase dexie)
        {
            dexie.TestCloudSync();
            return dexie.DBBaseJS.Module.Invoke<string>("CurrentUserId", dexie.DBBaseJS.Reference);
        }

        public static string AddOnVersion(this DBBase dexie)
        {
            dexie.TestCloudSync();
            return dexie.DBBaseJS.Module.Invoke<string>("AddOnVersion", dexie.DBBaseJS.Reference);
        }

        public static DexieCloudOptions Options(this DBBase dexie)
        {
            dexie.TestCloudSync();
            return dexie.DBBaseJS.Module.Invoke<DexieCloudOptions>("Options", dexie.DBBaseJS.Reference);
        }

        public static Dictionary<string, DexieCloudSchema>? Schema(this DBBase dexie)
        {
            dexie.TestCloudSync();
            return dexie.DBBaseJS.Module.Invoke<Dictionary<string, DexieCloudSchema>?>("Schema", dexie.DBBaseJS.Reference);
        }

        public static bool? UsingServiceWorker(this DBBase dexie)
        {
            dexie.TestCloudSync();
            return dexie.DBBaseJS.Module.Invoke<bool?>("UsingServiceWorker", dexie.DBBaseJS.Reference);
        }

        public static async ValueTask<string?> UserLogin(this DBBase dexie, LoginInformation userLogin)
        {
            dexie.TestCloudSync();
            var grantType = userLogin.GrantType switch
            {
                GrantType.DEMO => "demo",
                GrantType.OTP => "otp",
                _ => throw new InvalidOperationException("GrantType: Invalid type!")
            };

            return await dexie.DBBaseJS.Module.InvokeAsync<string?>("UserLogin", dexie.DBBaseJS.Reference, userLogin.EMail,
                grantType, userLogin.UserId);
        }

        public static async Task Logout(this DBBase dexie, bool force = false)
        {
            await dexie.DBBaseJS.Module.InvokeVoidAsync("Logout", dexie.DBBaseJS.Reference, force);
        }
        #endregion

        #region Internal
        private static void TestCloudSync(this DBBase dexie)
        {
            if (!dexie.CloudSync)
            {
                throw new InvalidOperationException("Cloud Sync not enabled, call 'Create' with DexieCloudOptions.");
            }
        }
        #endregion

        #region Table
        public static IUsePermissions<T> CreateUsePermissions<T, I>(this Table<T, I> table) where T : IDBStore, IDBCloudEntity
        {
            return UsePermissions<T, I>.Create(table);
        }
        #endregion
    }
}