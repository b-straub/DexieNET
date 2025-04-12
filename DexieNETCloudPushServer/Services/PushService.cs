using System.Net;
using System.Text.Json;
using System.Text;
using DexieCloudNET;
using DexieNETCloudPushServer.Quartz;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Impl.Matchers;
using WebPush;

namespace DexieNETCloudPushServer.Services
{
    public sealed partial class PushService : IHostedService
    {
        public static TimeSpan PushSubscriptionsInterval => TimeSpan.FromMinutes(1);
#if DEBUG
        private static TimeSpan PushSubscriptionsStartOffset => TimeSpan.FromSeconds(10);
#else
        private static TimeSpan PushSubscriptionsStartOffset => PushSubscriptionsInterval;
#endif
        public static TimeSpan NotificationsInterval => TimeSpan.FromMinutes(1);
        private static TimeSpan NotificationExpiredMinutes => TimeSpan.FromMinutes(5);
        private static TimeSpan TokeRefreshOffsetMinutes => TimeSpan.FromMinutes(5);

        private const string AuthenticationJobGroup = "AuthenticationJobGroup";

        public ILogger Logger { get; }

        private record DatabaseInfo(string Url, string ClientId, string ClientSecret, string? Token = null);

        private readonly HttpClient _httpClient;
        private readonly ISecretsConfigurationService? _configuration;
        private readonly ISchedulerFactory _schedulerFactory;
        private readonly QuartzDBContext _dbContext;
        private IScheduler? _scheduler;

        private readonly Dictionary<string, DatabaseInfo> _databases;

        private VapidDetails? _vapidKey;

        private const string PushSubscriptionsTableName = "pushSubscriptions";
        private const string PushNotificationsTableName = "pushNotifications";
        private const string MembersTableName = "members";

        public PushService(IServiceProvider serviceProvider)
        {
            _httpClient = serviceProvider.GetRequiredService<IHttpClientFactory>()
                .CreateClient("PushClient");

            _httpClient.Timeout = TimeSpan.FromSeconds(30);

            _configuration = serviceProvider.GetRequiredService<ISecretsConfigurationService>();
            _schedulerFactory = serviceProvider.GetRequiredService<ISchedulerFactory>();
            _dbContext = serviceProvider.GetRequiredService<QuartzDBContext>();
            Logger = serviceProvider.GetRequiredService<ILogger<PushService>>();
            _databases = [];
        }

        public async Task Initialize(CancellationToken cancellationToken)
        {
            Logger.LogDebug("Initialize Push Service.");

            if (_configuration is not null && _databases.Count == 0)
            {
                var secrets = _configuration.GetSecrets();

                var databasesJson = secrets["Databases"];
                ArgumentNullException.ThrowIfNull(databasesJson);
                var databasesConfig = JsonSerializer.Deserialize(databasesJson,
                    DatabaseConfigContext.Default.DatabaseConfigArray);
                ArgumentNullException.ThrowIfNull(databasesConfig);

                foreach (var db in databasesConfig)
                {
                    var dbInfo = new DatabaseInfo(db.Url, db.ClientId, db.ClientSecret);
                    _databases[db.Url] = dbInfo;
                    await ScheduleAuthenticationJob(db.Url, null, cancellationToken);
                }

                var vapidKeyJson = secrets["VapidKeys"];
                ArgumentNullException.ThrowIfNull(vapidKeyJson);
                var vapidKeyConfig = JsonSerializer.Deserialize(vapidKeyJson,
                    VapidKeysConfigContext.Default.VapidKeysConfig);
                ArgumentNullException.ThrowIfNull(vapidKeyConfig);

                _vapidKey = new VapidDetails("DexieCloudPushServer", vapidKeyConfig.PublicKey,
                    vapidKeyConfig.PrivateKey);
            }

            await SchedulePushMessagesJob(cancellationToken);
        }

        public async Task<DateTime> AuthenticateAndUpdate(string dbUrl, CancellationToken cancellationToken)
        {
            Logger.LogDebug("Authenticate Database {URL}.", dbUrl);

            var dbInfo = _databases[dbUrl];
            ArgumentNullException.ThrowIfNull(dbInfo);

            var body = new ClientCredentialsTokenRequest(
                [DBScopes.AccessDB, DBScopes.GlobalRead, DBScopes.GlobalWrite],
                dbInfo.ClientId, dbInfo.ClientSecret);
            var bodyJson = JsonSerializer.Serialize(body,
                ClientCredentialsTokenRequestContext.Default.ClientCredentialsTokenRequest);

            using StringContent jsonContent = new(bodyJson, Encoding.UTF8, "application/json");
            using var tokenResponse =
                await _httpClient.PostAsync($"{dbInfo.Url}/token", jsonContent, cancellationToken);

            if (!tokenResponse.IsSuccessStatusCode)
            {
                if (tokenResponse.StatusCode is HttpStatusCode.Unauthorized)
                {
                    Logger.LogError("Authentication failed for Database {URL}, removing DB!", dbUrl);
                    _databases.Remove(dbUrl);
                    return DateTime.UtcNow;
                }

                throw new InvalidOperationException(
                    $"Status {tokenResponse.StatusCode} -> Administration - Can not get token!");
            }

            var jsonStringToken = await tokenResponse.Content.ReadAsStringAsync(cancellationToken);
            var accessToken = JsonSerializer.Deserialize(jsonStringToken,
                TokenFinalResponseContext.Default.TokenFinalResponse);

            ArgumentNullException.ThrowIfNull(accessToken);

            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var expiration = epoch + TimeSpan.FromMilliseconds(accessToken.AccessTokenExpiration) -
                             TokeRefreshOffsetMinutes;
            dbInfo = dbInfo with { Token = accessToken.AccessToken };
            _databases[dbInfo.Url] = dbInfo;

            Logger.LogDebug("Scheduled Re-Authenticate Database {URL} at {TIME}.", dbUrl,
                TimeStamp(expiration.ToLocalTime()));

            return expiration;
        }

        public async Task SchedulePushMessages(CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(_databases);
            ArgumentNullException.ThrowIfNull(_vapidKey);
            ArgumentNullException.ThrowIfNull(_scheduler);

            /*await DeleteAllEntries(PushSubscriptionsTableName, cancellationToken);
            await DeleteAllEntries(PushNotificationsTableName, cancellationToken);
            return;*/

            Logger.LogDebug("Schedule Push Messages.");

            foreach (var dbInfo in _databases.Select(dbInfoDict => dbInfoDict.Value))
            {
                if (dbInfo.Token is null)
                {
                    continue;
                }

                await CheckAndDeleteExpiredSubscriptions(dbInfo, cancellationToken);
                await CheckAndDeleteExpiredNotifications(dbInfo, cancellationToken);

                using var request = new HttpRequestMessage(HttpMethod.Get,
                    $"{dbInfo.Url}/all/{PushNotificationsTableName}");
                request.Headers.Add("Authorization", $"Bearer {dbInfo.Token}");

                using var tableResponse = await _httpClient.SendAsync(request, cancellationToken);
                await CheckAuthentication(dbInfo.Url, tableResponse, cancellationToken);

                if (!tableResponse.IsSuccessStatusCode)
                {
                    Logger.LogError("Could not get entries from {TABLE_NAME} from {URL}, StatusCode {Status}.",
                        PushNotificationsTableName, dbInfo.Url, tableResponse.StatusCode);
                    continue;
                }

                var jsonStringTable = await tableResponse.Content.ReadAsStringAsync(cancellationToken);

                var notifications = JsonSerializer.Deserialize(jsonStringTable,
                    PushNotificationContext.Default.PushNotificationArray);

                ArgumentNullException.ThrowIfNull(notifications);
                List<string> expiredNotifications = [];

                foreach (var notification in notifications)
                {
                    if (notification.Expired || notification.ID is null)
                    {
                        throw new InvalidOperationException("Only handling valid not expired notifications here!");
                    }

                    if (notification.Triggers.Length == 0)
                    {
                        expiredNotifications.Add(notification.ID);
                        Logger.LogWarning("Remove notification '{ID}' without triggers.", notification.ID);
                    }

                    await DeleteGroupJobs(notification.ID, cancellationToken);

                    var job = JobBuilder.Create<ExecutePushMessagesJob>()
                        .WithIdentity(notification.ID, notification.ID)
                        .UsingJobData("DBUrl", dbInfo.Url)
                        .UsingJobData("NotificationID", notification.ID)
                        .Build();

                    job.JobDataMap["Notification"] = notification;

                    List<ITrigger> triggers = [];

                    foreach (var (pushTrigger, pushTriggerIndex) in notification.Triggers.Select((pushTrigger, i) =>
                                 (pushTrigger, i)))
                    {
                        var pushTimeUtc = pushTrigger.PushTimeUtc ?? DateTime.UtcNow;
                        var secondsToNow = (pushTimeUtc - DateTime.UtcNow).TotalSeconds;

                        if (secondsToNow < -(NotificationExpiredMinutes.TotalSeconds))
                        {
                            expiredNotifications.Add(notification.ID);
                            Logger.LogWarning("Remove outdated notification trigger '{MESSAGE}' at {TIME}.",
                                pushTrigger.Message,
                                TimeStamp(pushTimeUtc.ToLocalTime()));
                            continue;
                        }
                        else if (pushTimeUtc < DateTime.UtcNow)
                        {
                            pushTimeUtc = DateTime.UtcNow;
                        }

                        var intervalMinutes = pushTrigger.IntervalMinutes ?? 0;
                        var repeatCount = pushTrigger.RepeatCount ?? 0;

                        var trigger = TriggerBuilder.Create()
                            .WithIdentity(pushTriggerIndex.ToString(), notification.ID)
                            .StartAt(pushTimeUtc)
                            .WithSimpleSchedule(x => x
                                .WithIntervalInMinutes(intervalMinutes)
                                .WithRepeatCount(repeatCount))
                            .Build();

                        trigger.JobDataMap["Trigger"] = pushTrigger;
                        triggers.Add(trigger);

                        var timeString =
                            $"{TimeStamp(pushTimeUtc.ToLocalTime())} -> Every {intervalMinutes} Minute(s) x {repeatCount}";
                        Logger.LogInformation("Schedule notification trigger '{MESSAGE}' at {TIME}.",
                            pushTrigger.Message,
                            timeString);
                    }

                    await _scheduler.ScheduleJob(job, triggers, true, cancellationToken);

                    expiredNotifications.Add(notification.ID);
                }

                await DeleteTableEntries(dbInfo, PushNotificationsTableName, expiredNotifications, cancellationToken);

                if (expiredNotifications.Count > 0)
                {
                    Logger.LogDebug("Cleaned {COUNT} expired notifications.", expiredNotifications.Count);
                }
            }
        }

        public async Task ExecutePushMessages(string dbUrl, PushNotification notification, PushTrigger pushTrigger,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(_databases);
            ArgumentNullException.ThrowIfNull(_vapidKey);

            if (!_databases.TryGetValue(dbUrl, out var dbInfo))
            {
                await DeleteGroupJobs(notification.ID, cancellationToken);
                return;
            }

            // check if notification update exist
            using var request = new HttpRequestMessage(HttpMethod.Get,
                $"{dbInfo.Url}/all/{PushNotificationsTableName}/{notification.ID}");
            request.Headers.Add("Authorization", $"Bearer {dbInfo.Token}");

            using var tableResponse = await _httpClient.SendAsync(request, cancellationToken);
            await CheckAuthentication(dbInfo.Url, tableResponse, cancellationToken);

            if (tableResponse.IsSuccessStatusCode)
            {
                return; // handled elsewhere
            }

            List<string> owners = [notification.Owner];

            var requestUri1 =
                QueryHelpers.AddQueryString($"{dbInfo.Url}/all/{MembersTableName}", "realmId",
                    notification.NotifierRealm);
            using var request1 = new HttpRequestMessage(HttpMethod.Get, requestUri1);
            request1.Headers.Add("Authorization", $"Bearer {dbInfo.Token}");

            using var tableResponse1 = await _httpClient.SendAsync(request1, cancellationToken);
            await CheckAuthentication(dbInfo.Url, tableResponse1, cancellationToken);

            if (tableResponse1.IsSuccessStatusCode)
            {
                var jsonStringTable1 = await tableResponse1.Content.ReadAsStringAsync(cancellationToken);
                var members = JsonSerializer.Deserialize(jsonStringTable1,
                    PushMemberContext.Default.PushMemberArray);

                ArgumentNullException.ThrowIfNull(members);
                owners.AddRange(members.Where(m => m.UserId is not null).Select(m => m.UserId!));
            }
            else
            {
                Logger.LogError("Could not get entries from {TableName} from {URL}, StatusCode {Status}.",
                    MembersTableName, dbInfo.Url, tableResponse1.StatusCode);
            }

            foreach (var owner in owners.Distinct())
            {
                var requestUri2 =
                    QueryHelpers.AddQueryString($"{dbInfo.Url}/all/{PushSubscriptionsTableName}", "owner", owner);
                using var request2 = new HttpRequestMessage(HttpMethod.Get, requestUri2);
                request2.Headers.Add("Authorization", $"Bearer {dbInfo.Token}");

                using var tableResponse2 = await _httpClient.SendAsync(request2, cancellationToken);
                await CheckAuthentication(dbInfo.Url, tableResponse2, cancellationToken);

                if (!tableResponse2.IsSuccessStatusCode)
                {
                    Logger.LogError("Could not get entries from {TableName} from {URL}, StatusCode {Status}.",
                        PushSubscriptionsTableName, dbInfo.Url, tableResponse2.StatusCode);
                    return;
                }

                var jsonStringTable2 = await tableResponse2.Content.ReadAsStringAsync(cancellationToken);
                var dexieSubscriptions = JsonSerializer.Deserialize(jsonStringTable2,
                    WebPushSubscriptionEntryContext.Default.WebPushSubscriptionArray);

                ArgumentNullException.ThrowIfNull(dexieSubscriptions);

                if (dexieSubscriptions.Length == 0)
                {
                    continue;
                }

                using var webPushClient = new WebPushClient();

                foreach (var dexieSubscription in dexieSubscriptions)
                {
                    if (dexieSubscription.Expired)
                    {
                        return; // cleaned-up elsewhere
                    }

                    try
                    {
                        var pushSubscription = new PushSubscription(dexieSubscription.Subscription.Endpoint,
                            dexieSubscription.Subscription.Keys.P256dh,
                            dexieSubscription.Subscription.Keys.Auth);
                        var vapidDetails = new VapidDetails(@$"mailto:user@localhost.de",
                            _vapidKey.PublicKey, _vapidKey.PrivateKey);

                        var pushEventJsonBase64 =
                            Convert.ToBase64String(Encoding.UTF8.GetBytes(pushTrigger.PushPayloadJson));
                        var pushURLBase64 = dexieSubscription.PushURL +
                                            $"?{PushConstants.PushPayloadJsonBase64}={pushEventJsonBase64}";

                        // currently declarative push is working only for iOS/iPadOS
                        // the notification format is a bit picky about optional fields, leave out icon and requireInteraction
                        var webPushNotification =
                            new WebPushNotification(notification.Title, pushTrigger.Message, pushURLBase64,
                                notification.Tag, notification.AppBadge, 
                                dexieSubscription.PushSupport.Declarative ? null : pushTrigger.Icon, 
                                dexieSubscription.PushSupport.Declarative ? null : pushTrigger.RequireInteraction);
                        var magicNumber = dexieSubscription.PushSupport is { Declarative: true, IsMobile: true }
                            ? DeclarativeWebPushNotification.DeclarativeWebPushMagicNumber
                            : (int?)null;
                        var declarativePushNotification =
                            new DeclarativeWebPushNotification(magicNumber, webPushNotification);

                        var declarativePushNotificationJson =
                            JsonSerializer.Serialize(declarativePushNotification,
                                DeclarativeWebPushNotificationContext.Default.DeclarativeWebPushNotification);

                        await webPushClient.SendNotificationAsync(pushSubscription, declarativePushNotificationJson,
                            vapidDetails,
                            cancellationToken);

                        Logger.LogDebug("Submit notification '{MESSAGE}' to {URL}.", pushTrigger.Message,
                            GetURLPart(dexieSubscription.Subscription.Endpoint));
                    }
                    catch (Exception exception)
                    {
                        var cleanSubscription = exception switch
                        {
                            WebPushException webPushException => webPushException.StatusCode is HttpStatusCode.Gone,
                            _ => true, // likely some outdated subscription
                        };

                        if (cleanSubscription)
                        {
                            await DeleteTableEntries(dbInfo, PushSubscriptionsTableName, [dexieSubscription.ID],
                                cancellationToken);
                            Logger.LogWarning("Remove gone or invalid Subscription with endpoint to {URL}.",
                                GetURLPart(dexieSubscription.Subscription.Endpoint));
                        }
                        else
                        {
                            Logger.LogError("Submit notification failed '{MESSAGE}' to {URL} with {STATUS}.",
                                pushTrigger.Message,
                                GetURLPart(dexieSubscription.Subscription.Endpoint), exception.Message);
                        }
                    }
                }
            }
        }

        private async Task CheckAndDeleteExpiredNotifications(DatabaseInfo dbInfo, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(_scheduler);

            var requestUri =
                QueryHelpers.AddQueryString($"{dbInfo.Url}/all/{PushNotificationsTableName}", "expired", "true");

            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            request.Headers.Add("Authorization", $"Bearer {dbInfo.Token}");

            using var tableResponse = await _httpClient.SendAsync(request, cancellationToken);
            await CheckAuthentication(dbInfo.Url, tableResponse, cancellationToken);

            if (!tableResponse.IsSuccessStatusCode)
            {
                Logger.LogError("Could not get entries from {TABLE_NAME} from {URL}, StatusCode {STATUS}.",
                    PushNotificationsTableName, dbInfo.Url, tableResponse.StatusCode);
                return;
            }

            var jsonStringTable = await tableResponse.Content.ReadAsStringAsync(cancellationToken);

            var notifications = JsonSerializer.Deserialize(jsonStringTable,
                PushNotificationContext.Default.PushNotificationArray);

            ArgumentNullException.ThrowIfNull(notifications);

            foreach (var notification in notifications)
            {
                if (!notification.Expired || notification.ID is null)
                {
                    throw new InvalidOperationException("Only handling valid expired notifications here!");
                }

                await DeleteGroupJobs(notification.ID, cancellationToken);
                await DeleteTableEntries(dbInfo, PushNotificationsTableName, [notification.ID], cancellationToken);
            }

            if (notifications.Length > 0)
            {
                Logger.LogDebug("Cleaned {LENGTH} expired notifications.", notifications.Length);
            }
        }

        private async Task CheckAndDeleteExpiredSubscriptions(DatabaseInfo dbInfo, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(_scheduler);

            var requestUri =
                QueryHelpers.AddQueryString($"{dbInfo.Url}/all/{PushSubscriptionsTableName}", "expired", "true");

            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);

            //using var request = new HttpRequestMessage(HttpMethod.Get, $"{dbInfo.Url}/all/{PushSubscriptionsTableName}");

            request.Headers.Add("Authorization", $"Bearer {dbInfo.Token}");

            using var tableResponse = await _httpClient.SendAsync(request, cancellationToken);
            await CheckAuthentication(dbInfo.Url, tableResponse, cancellationToken);

            if (!tableResponse.IsSuccessStatusCode)
            {
                Logger.LogError("Could not get entries from {TABLE_NAME} from {URL}, StatusCode {STATUS}.",
                    PushSubscriptionsTableName, dbInfo.Url, tableResponse.StatusCode);
                return;
            }

            var jsonStringTable = await tableResponse.Content.ReadAsStringAsync(cancellationToken);

            var subscriptions = JsonSerializer.Deserialize(jsonStringTable,
                WebPushSubscriptionEntryContext.Default.WebPushSubscriptionArray);

            ArgumentNullException.ThrowIfNull(subscriptions);

            foreach (var subscription in subscriptions)
            {
                if (!subscription.Expired)
                {
                    throw new InvalidOperationException("Only handling expired subscriptions here!");
                }

                await DeleteTableEntries(dbInfo, PushSubscriptionsTableName, [subscription.ID], cancellationToken);
            }

            if (subscriptions.Length > 0)
            {
                Logger.LogInformation("Cleaned {LENGTH} expired subscriptions.", subscriptions.Length);
            }
        }

        private async Task DeleteGroupJobs(string groupID, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(_scheduler);

            var groupMatcher = GroupMatcher<JobKey>.GroupContains(groupID);
            var jobKeys = await _scheduler.GetJobKeys(groupMatcher, cancellationToken);
            foreach (var jobKey in jobKeys)
            {
                await _scheduler.DeleteJob(jobKey, cancellationToken);
            }
        }

        private async Task DeleteTableEntries(DatabaseInfo dbInfo, string tableName, IEnumerable<string?> ids,
            CancellationToken cancellationToken)
        {
            foreach (var id in ids)
            {
                if (id is null)
                {
                    continue;
                }

                var idEncoded = Uri.EscapeDataString(id);

                using var request = new HttpRequestMessage(HttpMethod.Delete,
                    $"{dbInfo.Url}/all/{tableName}/{idEncoded}");
                request.Headers.Add("Authorization", $"Bearer {dbInfo.Token}");

                using var tableResponse = await _httpClient.SendAsync(request, cancellationToken);
                await CheckAuthentication(dbInfo.Url, tableResponse, cancellationToken);

                if (!tableResponse.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException(
                        $"Delete subscription failed with status: {tableResponse.StatusCode}!");
                }
            }
        }

        private async Task DeleteAllEntries(string tableName, CancellationToken cancellationToken)
        {
            foreach (var dbInfo in _databases.Select(dbInfoDict => dbInfoDict.Value))
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, $"{dbInfo.Url}/all/{tableName}");
                request.Headers.Add("Authorization", $"Bearer {dbInfo.Token}");

                using var tableResponse = await _httpClient.SendAsync(request, cancellationToken);
                await CheckAuthentication(dbInfo.Url, tableResponse, cancellationToken);

                if (!tableResponse.IsSuccessStatusCode)
                {
                    Logger.LogError("Could not get entries from {TABLE_NAME} for {URL}, StatusCode {STATUS}.",
                        tableName, dbInfo.Url, tableResponse.StatusCode);
                    return;
                }

                var jsonStringTable = await tableResponse.Content.ReadAsStringAsync(cancellationToken);

                var pushEntities = JsonSerializer.Deserialize(jsonStringTable,
                    PushEntityContext.Default.PushEntityArray);

                ArgumentNullException.ThrowIfNull(pushEntities);

                var idsToDelete = pushEntities.Select(e => e.ID).ToList();
                await DeleteTableEntries(dbInfo, tableName, idsToDelete, cancellationToken);
                Logger.LogDebug("Purged {LENGTH} entries from {TABLE_NAME} for {URL}.", idsToDelete.Count,
                    tableName, dbInfo.Url);
            }
        }

        private async Task CheckAuthentication(string dbUrl, HttpResponseMessage httpResponseMessage,
            CancellationToken cancellationToken)
        {
            if (httpResponseMessage.StatusCode is HttpStatusCode.Unauthorized)
            {
                await ScheduleAuthenticationJob(dbUrl, null, cancellationToken);
                throw new HttpRequestException("Retry because of failed Authentication!", null,
                    httpResponseMessage.StatusCode);
            }
        }

        private async Task SchedulePushMessagesJob(CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(_scheduler);

            var job = JobBuilder.Create<SchedulePushMessagesJob>()
                .WithIdentity("schedulePushMessagesJob", "Administration")
                .Build();

            var trigger = TriggerBuilder.Create()
                .WithIdentity("schedulePushMessagesTrigger", "Administration")
                .StartAt(DateTime.UtcNow + PushSubscriptionsStartOffset)
                .WithSimpleSchedule(x => x
                    .WithIntervalInMinutes((int)PushSubscriptionsInterval.TotalMinutes)
                    .RepeatForever())
                .Build();

            await _scheduler.ScheduleJob(job, [trigger], true, cancellationToken);
        }

        private async Task ScheduleAuthenticationJob(string url, DateTime? startTimeUtc,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(_scheduler);

            var job = JobBuilder.Create<ScheduleAuthenticationJob>()
                .WithIdentity(url, AuthenticationJobGroup)
                .UsingJobData("DBUrl", url)
                .Build();

            var timeToStartUtc = startTimeUtc ?? DateTime.UtcNow;

            var trigger = TriggerBuilder.Create()
                .WithIdentity(url, AuthenticationJobGroup)
                .StartAt(timeToStartUtc)
                .Build();

            await _scheduler.ScheduleJob(job, [trigger], true, cancellationToken);
        }

        private async Task ScheduleInitializationJob(CancellationToken cancellationToken)
        {
            _scheduler ??= await _schedulerFactory.GetScheduler(cancellationToken);
            ArgumentNullException.ThrowIfNull(_scheduler);
            await DeleteGroupJobs(AuthenticationJobGroup, cancellationToken);

            var job = JobBuilder.Create<ScheduleInitializationJob>()
                .WithIdentity("scheduleInitializationJob", "Administration")
                .Build();

            var trigger = TriggerBuilder.Create()
                .WithIdentity("scheduleInitializationTrigger", "Administration")
                .StartNow()
                .WithSimpleSchedule(x => x
                    .WithIntervalInMinutes((int)NotificationsInterval.TotalMinutes)
                    .RepeatForever())
                .Build();

            await _scheduler.ScheduleJob(job, [trigger], true, cancellationToken);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _dbContext.Database.EnsureCreatedAsync(cancellationToken);
            await _dbContext.Database.MigrateAsync(cancellationToken);

            await ScheduleInitializationJob(cancellationToken);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_scheduler is not null)
            {
                await _scheduler.Shutdown(cancellationToken);
            }

            //await _dbContext.Database.EnsureDeletedAsync(cancellationToken);
            await _dbContext.DisposeAsync();
        }

        private static string TimeStamp(DateTime? dateTime = null)
        {
            return dateTime.HasValue
                ? dateTime.Value.ToString("G")
                : DateTime.Now.ToString("G");
        }

        private static string GetURLPart(string text, int number = 50)
        {
            return text.Length <= number ? text : text[..number];
        }
    }
}