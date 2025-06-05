using R3;
using System.Text.Json;
using DexieCloudNET;
using DexieNET;
using DexieNETCloudSample.Extensions;
using RxBlazorLightCore;

namespace DexieNETCloudSample.Logic;

public partial class DexieCloudService
{
    public async Task SubscribePush()
    {
        ArgumentNullException.ThrowIfNull(DB);
        if ((!UserLogin.Value?.IsLoggedIn).True())
        {
            throw new InvalidOperationException("User must be logged in to subscribe to push notifications!");
        }

        await DB.SubscribePush();
    }

    public async Task UnsubscribePush()
    {
        ArgumentNullException.ThrowIfNull(DB);
        await DB.UnSubscribePush();
    }

    public async Task<bool> AskForNotificationPermission()
    {
        ArgumentNullException.ThrowIfNull(DB);

        return await DB.AskForNotificationPermission();
    }
    
    public async Task<bool> OpenList(string listID)
    {
        ArgumentNullException.ThrowIfNull(DB);

        var listExist = (await DB.ToDoDBLists.Get(listID)) is not null;

        if (!listExist)
        {
            return false;
        }

        await DB.Transaction(async t =>
        {
            var oc = await DB.ListOpenCloses.Get(listID);
            await DB.ListOpenCloses.ToCollection().Modify(oc1 => oc1.IsItemsOpen, false);
            await DB.ListOpenCloses.ToCollection().Modify(oc1 => oc1.IsShareOpen, false);
           
            if (!t.Collecting)
            {
                oc = oc is null ? new ListOpenClose(false, true, listID) : oc with { IsItemsOpen = true };
            }
            await DB.ListOpenCloses.Put(oc);
        });
        
        return true;
    }
    
    public Func<IStateCommandAsync, Task> ToggleDarkMode => async _ =>
    {
        ArgumentNullException.ThrowIfNull(DB);
            
        await DB.Transaction(async t =>
        {
            var colorThemeSettings = await DB.Settings.Get(Settings.ColorThemeKey);
            if (!t.Collecting)
            {
                var theme = colorThemeSettings?.GetColorTheme();
                var light = theme is null || !theme.Light;
                colorThemeSettings = Settings.CreateColorTheme(light);
            }
            await DB.Settings.Put(colorThemeSettings);
        });
    };
    
    public Func<IStateProgressObserverAsync, IDisposable> LogoutObservable => observer =>
    {
        ArgumentNullException.ThrowIfNull(DB);
        const int timeOutS = 1;
        const int progressSliceMS = 100;
        
        var startObservable = Observable
            .FromAsync(async _ =>
            {
                if (NotificationsState.Value is NotificationState.Subscribed)
                {
                    await UnsubscribePush();
                    await Sync(new SyncOptions());
                }
                return Observable.Return(Unit.Default);
            });

        var stopObservable = Observable
            .FromAsync(async _ =>
            {
                var unsyncedChanges = await DB.NumUnsyncedChanges();
                if (unsyncedChanges == 0 && SyncState.Value.ValidPhase())
                {
                    await DB.Logout();
                }
                else
                {
                    throw new InvalidOperationException($"Can not logout: {SyncState.Value?.Phase} - {unsyncedChanges} unsynced changes");
                }

                return Observable.Return(Unit.Default);
            });

        var triggerObservable =
            Observable
                .Timer(TimeSpan.FromSeconds(timeOutS))
                .Select(_ => true).Race(DB.SyncCompleteObservable());

        return Observable
            .Interval(TimeSpan.FromMilliseconds(progressSliceMS))
            .TakeUntil(startObservable)
            .Concat(
                Observable
                    .Interval(TimeSpan.FromMilliseconds(progressSliceMS))
                    .TakeUntil(triggerObservable)
                    .Concat(
                        Observable
                            .Interval(TimeSpan.FromMilliseconds(progressSliceMS))
                            .TakeUntil(stopObservable)
                    )
            )
            .Select(_ => -1.0)
            .Subscribe(observer.AsObserver);
    };
    
    public void SetPushPayload(PushPayloadToDo? pushPayload)
    {
        PushPayload = pushPayload;
    }
    
    public void SetSharePayload(SharePayload? sharePayload)
    {
        SharePayload = sharePayload;
    }

    public async Task SendPushNotification(string message)
    {
        ArgumentNullException.ThrowIfNull(DB);
        
        var pushPayloadEnvelope = new PushPayloadEnvelope(PushPayloadType.MESSAGE, message);
        var pushPayloadEnvelopeJson = JsonSerializer.Serialize(pushPayloadEnvelope,
            PushPayloadEnvelopeConfigContext.Default.PushPayloadEnvelope);
            
        var pushPayloadBase64 = pushPayloadEnvelopeJson.ToBase64();
        var messageTrigger = new PushTrigger(message, pushPayloadBase64, PushConstants.PushIconMessage);
        var pushNotification =
            new PushNotification(_pushMessageTag, "ToDo", string.Empty, [messageTrigger], _pushMessageTag);
        await DB.PushNotifications.Put(pushNotification);
    }
}