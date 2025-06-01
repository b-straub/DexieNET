/*
DexieNETCloudPush.cs

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
    public enum NotificationState
    {
        None,
        Denied,
        Unsubscribed,
        UnsubscribedRemote,
        Subscribed,
    }
    
    public enum ServiceWorkerState 
    {
        None,
        UpdateFound,
        ReloadPage
    }
    
    public record ServiceWorkerNotifications(ServiceWorkerState State);
    
    public static partial class DBCloudExtensions
    {
        public static async ValueTask<bool> SubscribePush(this DBBase dexie)
        {
            if (!dexie.HasCloud())
            {
                throw new InvalidOperationException("Can not ConfigureCloud for non cloud database.");
            }
            
            if (!dexie.HasPushSupport)
            {
                throw new InvalidOperationException("Database has no push support can not handle notifications.");
            }

            return await dexie.Cloud.Module.InvokeAsync<bool>("SubscribePush");
        }

        public static async ValueTask<bool> UnSubscribePush(this DBBase dexie)
        {
            if (!dexie.HasCloud())
            {
                throw new InvalidOperationException("Can not ConfigureCloud for non cloud database.");
            }

            if (!dexie.HasPushSupport)
            {
                throw new InvalidOperationException("Database has no push support can not handle notifications.");
            }
            
            return await dexie.Cloud.Module.InvokeAsync<bool>("UnSubscribePush");
        }
        
        public static async ValueTask ExpireAllPushSubscriptions(this DBBase dexie)
        {
            if (!dexie.HasCloud())
            {
                throw new InvalidOperationException("Can not ConfigureCloud for non cloud database.");
            }
            
            if (!dexie.HasPushSupport)
            {
                throw new InvalidOperationException("Database has no push support can not handle notifications.");
            }

            await dexie.Cloud.Module.InvokeVoidAsync("ExpireAllPushSubscriptions");
        }

        public static async ValueTask<bool> AskForNotificationPermission(this DBBase dexie)
        {
            if (!dexie.HasCloud())
            {
                throw new InvalidOperationException("Can not ConfigureCloud for non cloud database.");
            }

            return await dexie.Cloud.Module.InvokeAsync<bool>("AskForNotificationPermission");
        }
        
        public static Observable<NotificationState> NotificationStateObservable(this DBBase dexie)
        {
            if (!dexie.HasCloud())
            {
                throw new InvalidOperationException("Can not ConfigureCloud for non cloud database.");
            }

            return JSObservable<NotificationState>.Create(dexie, "SubscribeNotificationState").AsObservable;
        }
        
        public static Observable<ServiceWorkerNotifications> ServiceWorkerNotificationObservable(this DBBase dexie)
        {
            return JSObservable<ServiceWorkerNotifications>.Create(dexie, "SubscribeServiceWorkerNotification").AsObservable;
        }

        public static async Task ExpireNotifications(this Table<PushNotification, string> table, IEnumerable<string?> notifierIDs)
        {
            if (!table.DB.HasCloud())
            {
                throw new InvalidOperationException("Can not ConfigureCloud for non cloud database.");
            }
            
            if (!table.DB.HasPushSupport)
            {
                throw new InvalidOperationException("Database has no push support can not handle notifications.");
            }
            
            if (table.TransactionCollecting)
            {
                await table.Put(null);
                return;
            }
            
            foreach (var notifierID in notifierIDs)
            {
                await table.ExpireNotification(notifierID);
            }
        }
        
        public static async Task ExpireNotification(this Table<PushNotification, string> table, string? notifierID)
        {
            if (!table.DB.HasCloud())
            {
                throw new InvalidOperationException("Can not ConfigureCloud for non cloud database.");
            }
            
            if (!table.DB.HasPushSupport)
            {
                throw new InvalidOperationException("Database has no push support can not handle notifications.");
            }
            
            if (table.TransactionCollecting)
            {
                await table.Put(null);
                return;
            }
            
            ArgumentNullException.ThrowIfNull(notifierID);
            
            var expiredNotifications = await table.Where(n => n.ID, notifierID).Modify(n => n.Expired, true);

            if (expiredNotifications == 0)
            {
                var notification = new PushNotification(notifierID, string.Empty, string.Empty, [], null, null, true);
                await table.Put(notification);
            }
        }
        
        public static async Task UpdateServiceWorker(this DBBase dexie)
        {
            if (!dexie.HasCloud())
            {
                throw new InvalidOperationException("Can not ConfigureCloud for non cloud database.");
            }

            await dexie.Cloud.Module.InvokeVoidAsync("UpdateServiceWorker");
        }
        
        public static async Task SetAppBadge(this DBBase dexie, long count)
        {
            if (!dexie.HasCloud())
            {
                throw new InvalidOperationException("Can not ConfigureCloud for non cloud database.");
            }

            if (!dexie.HasPushSupport)
            {
                throw new InvalidOperationException("Database has no push support can not handle notifications.");
            }
            
            await dexie.Cloud.Module.InvokeVoidAsync("SetAppBadge", count);
        }
    }
}
