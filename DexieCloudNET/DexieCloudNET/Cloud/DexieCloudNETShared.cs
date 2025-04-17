/*
DexieNETCloudShared.cs

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
    
    public record PushTrigger(
        string Message,
        string PushPayloadBase64,
        string Icon,
        DateTime? PushTimeUtc = null,
        bool RequireInteraction = false,
        int? IntervalMinutes = null,
        int? RepeatCount = null
    );

    [Schema(CloudSync = true)]
    public record PushNotification(
        [property: Index(IsPrimary = true)] string ID,
        string Title,
        string NotifierRealm,
        PushTrigger[] Triggers,
        string? Tag = null,
        long? AppBadge = null,
        bool Expired = false,
        string? RealmId = null,
        string? Owner = null
    ) : IDBStore, IDBCloudEntity;
}
