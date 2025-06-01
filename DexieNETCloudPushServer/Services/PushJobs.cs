using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using DexieCloudNET;
using Quartz;

namespace DexieNETCloudPushServer.Services;

[DisallowConcurrentExecution]
public class ScheduleInitializationJob(IServiceProvider serviceProvider) : IJob
{
    private readonly PushService _pushService = serviceProvider.GetRequiredService<PushService>();

    public async Task Execute(IJobExecutionContext context)
    {
        var success = true;

        try
        {
            await _pushService.Initialize(context.CancellationToken);
        }
        catch (Exception ex)
        {
            _pushService.Logger.LogWarning("Rerun InitializationJob because of '{MESSAGE}'", ex.Message);
            success = false;
        }

        if (success)
        {
            await context.Scheduler.UnscheduleJob(context.Trigger.Key);
        }
    }
}

[DisallowConcurrentExecution]
public class ScheduleAuthenticationJob(IServiceProvider serviceProvider) : IJob
{
    public string? DBUrl { private get; set; }
    private readonly PushService _pushService = serviceProvider.GetRequiredService<PushService>();

    public async Task Execute(IJobExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(DBUrl);
        DateTime? nextAuthenticationTimeUtc;
        
        try
        {
            nextAuthenticationTimeUtc = await _pushService.AuthenticateAndUpdate(DBUrl, context.CancellationToken);
            if (nextAuthenticationTimeUtc <= DateTime.UtcNow)
            {
                await context.Scheduler.DeleteJob(context.JobDetail.Key);
            }
        }
        catch (Exception ex)
        {
            _pushService.Logger.LogWarning("Rerun ScheduleAuthenticationJob because of '{MESSAGE}'", ex.Message);
            nextAuthenticationTimeUtc = DateTime.UtcNow;
        }
        
        var oldTrigger = context.Trigger;

        var newTrigger = TriggerBuilder.Create()
            .WithIdentity(oldTrigger.Key.Name, oldTrigger.Key.Group)
            .StartAt(nextAuthenticationTimeUtc.Value)
            .Build();
        await context.Scheduler.RescheduleJob(oldTrigger.Key, newTrigger);
    }
}

[DisallowConcurrentExecution]
public class SchedulePushMessagesJob(IServiceProvider serviceProvider) : IJob
{
    private readonly PushService _pushService = serviceProvider.GetRequiredService<PushService>();
    
    public async Task Execute(IJobExecutionContext context)
    {
        var success = true;
        
        try
        {
            await _pushService.SchedulePushMessages(context.CancellationToken);
        }
        catch (Exception ex)
        {
            _pushService.Logger.LogWarning("Rerun SchedulePushMessages because of '{MESSAGE}'", ex.Message);
            success = false;
        }

        if (!success)
        {
            var oldTrigger = context.Trigger;

            var newTrigger = TriggerBuilder.Create()
                .WithIdentity(oldTrigger.Key.Name, oldTrigger.Key.Group)
                .StartAt(DateTimeOffset.UtcNow.Add(PushService.PushSubscriptionsInterval))
                .WithSimpleSchedule(x => x
                    .WithIntervalInMinutes((int)PushService.PushSubscriptionsInterval.TotalMinutes)
                    .RepeatForever())
                .Build();
            await context.Scheduler.RescheduleJob(oldTrigger.Key, newTrigger);
        }
    }
}

[DisallowConcurrentExecution]
[PersistJobDataAfterExecution]
public class ExecutePushMessagesJob(IServiceProvider serviceProvider) : IJob
{
    public string? DBUrl { private get; set; }
    private readonly PushService _pushService = serviceProvider.GetRequiredService<PushService>();

    public async Task Execute(IJobExecutionContext context)
    {
        var dataMap = context.MergedJobDataMap;
        var success = true;
        ArgumentNullException.ThrowIfNull(DBUrl);
        
        if (dataMap["Notification"] is PushNotification notification && dataMap["Trigger"] is PushTrigger trigger)
        {
            try
            {
                await _pushService.ExecutePushMessages(DBUrl, notification, trigger, context.CancellationToken);
            }
            catch (Exception ex)
            {
                _pushService.Logger.LogWarning("Rerun ExecutePushMessagesJob because of '{MESSAGE}'", ex.Message);
                success = false;
            }

            if (!success)
            {
                var oldTrigger = context.Trigger;

                var newTrigger = TriggerBuilder.Create()
                    .WithIdentity(oldTrigger.Key.Name, oldTrigger.Key.Group)
                    .StartAt(DateTimeOffset.UtcNow.Add(PushService.NotificationsInterval))
                    .WithSimpleSchedule(x => x
                        .WithIntervalInMinutes((int)PushService.NotificationsInterval.TotalMinutes)
                        .RepeatForever())
                    .Build();
                newTrigger.JobDataMap["Trigger"] = trigger;
                
                await context.Scheduler.RescheduleJob(oldTrigger.Key, newTrigger);
            }
        }
    }
}