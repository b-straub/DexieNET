using System.Reactive;
using System.Reactive.Linq;
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
    
    public async Task OpenList(string listID)
    {
        ArgumentNullException.ThrowIfNull(DB);

        var listExist = (await DB.ToDoDBLists.Get(listID)) is not null;

        if (!listExist)
        {
            return;
        }

        await DB.Transaction(async t =>
        {
            await DB.ListOpenCloses.ToCollection().Modify(oc => oc.IsItemsOpen, false);
            await DB.ListOpenCloses.ToCollection().Modify(oc => oc.IsShareOpen, false);
            var oc = await DB.ListOpenCloses.Get(listID);
            if (!t.Collecting)
            {
                oc = oc is null ? new ListOpenClose(true, false, listID) : oc with { IsItemsOpen = true };
            }
            await DB.ListOpenCloses.Put(oc);
        });
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
    
    public Func<IStateObserverAsync, IDisposable> LogoutObservable => observer =>
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
                }
                return Observable.Return(Unit.Default);
            });

        var stopObservable = Observable
            .FromAsync(async _ =>
            {
                if (await DB.NumUnsyncedChanges() == 0 && SyncState.Value.ValidPhase())
                {
                    await DB.Logout();
                }
                else
                {
                    throw new InvalidOperationException($"Can not logout: {SyncState.Value?.Phase}");
                }

                return Observable.Return(Unit.Default);
            });

        var triggerObservable =
            Observable
                .Timer(TimeSpan.FromSeconds(timeOutS))
                .Select(_ => true).Amb(DB.SyncCompleteObservable());

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
            .Select(_ => -1L)
            .Subscribe(observer);
    };
    
    public void SetPushPayloadEvent(PushPayload pushPayload)
    {
        PushPayloadEventState.Value = pushPayload;
    }
}