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

using Microsoft.JSInterop;

namespace DexieNET
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

            var jsi = cloudOptions.FromObject();
            var err = dexie.DBBaseJS.Module.Invoke<string?>("ConfigureCloud", dexie.DBBaseJS.Reference, jsi);

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

        public static async ValueTask UserLogin(this DBBase dexie, LoginInformation userLogin)
        {
            dexie.TestCloudSync();
            var grantType = userLogin.GrantType switch
            {
                GrantType.DEMO => "demo",
                GrantType.OTP => "otp",
                _ => throw new InvalidOperationException("GrantType: Invalid type!")
            };

            await dexie.DBBaseJS.Module.InvokeVoidAsync("UserLogin", dexie.DBBaseJS.Reference, userLogin.EMail,
                grantType, userLogin.UserId);
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
    }
}
