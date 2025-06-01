using Microsoft.AspNetCore.Components;
using System.Text.Json;
using DexieNETCloudSample.Logic;
using MudBlazor;

namespace DexieNETCloudSample.Components;

public partial class SharePushContainer
{
    [Inject]
    public required ISnackbar Snackbar { get; init; }

    [Parameter]
    [SupplyParameterFromQuery(Name = $"{PushConstants.PushPayloadBase64}")]
    public string? PushPayloadBase64 { get; set; }

    [Parameter]
    [SupplyParameterFromQuery(Name = $"{PushConstants.SharePayloadBase64}")]
    public string? SharePayloadBase64 { get; set; }

    [Inject]
    public required NavigationManager NavigationManager { get; init; }

    protected override void OnAfterRender(bool firstRender)
    {
        base.OnAfterRender(firstRender);

        if (!firstRender)
        {
            return;
        }

        if (PushPayloadBase64 is not null)
        {
            try
            {
                var pushPayloadJson = PushPayloadBase64.FromBase64();
                var pushPayloadEnvelope = JsonSerializer.Deserialize(pushPayloadJson,
                    PushPayloadEnvelopeConfigContext.Default.PushPayloadEnvelope);
                ArgumentNullException.ThrowIfNull(pushPayloadEnvelope);

                switch (pushPayloadEnvelope)
                {
                    case { Type: PushPayloadType.TODO, Payload: not null }:
                    {
                        var pushPayload = JsonSerializer.Deserialize(pushPayloadEnvelope.Payload,
                            PushPayloadToDoConfigContext.Default.PushPayloadToDo);

                        ArgumentNullException.ThrowIfNull(pushPayload);
                        Service.SetPushPayload(pushPayload);
                        break;
                    }
                    case { Type: PushPayloadType.MESSAGE, Payload: not null }:
                        Snackbar.Add(
                            $"Push Notification, received urgent message: '{pushPayloadEnvelope.Payload}'!",
                            Severity.Warning,
                            config => { config.RequireInteraction = false; });
                        break;
                    default:
                        Snackbar.Add($"Push Notification, Received invalid push payload: '{pushPayloadJson}'!",
                            Severity.Error,
                            config => { config.RequireInteraction = false; });
                        break;
                }
            }
            catch (Exception e)
            {
                Snackbar.Add($"Push Notification, Received invalid push payload: '{e.Message}'!",
                    Severity.Error,
                    config => { config.RequireInteraction = false; });
            }
            finally
            {
                PushPayloadBase64 = null;
                NavigationManager.NavigateTo(PushConstants.HomeRoute); // home - some PWA implementations may store last URL
            }
        }

        if (SharePayloadBase64 is not null)
        {
            try
            {
                var sharePayloadBase64 = SharePayloadBase64.FromBase64();
                var sharePayload = JsonSerializer.Deserialize(sharePayloadBase64,
                    SharePayloadConfigContext.Default.SharePayload);

                ArgumentNullException.ThrowIfNull(sharePayload);
                Service.SetSharePayload(sharePayload);
            }
            catch (Exception e)
            {
                Snackbar.Add($"Share, received invalid share payload: '{e.Message}'!",
                    Severity.Error,
                    config => { config.RequireInteraction = false; });
            }
            finally
            {
                SharePayloadBase64 = null;
                NavigationManager.NavigateTo(PushConstants.HomeRoute); // home - some PWA implementations may store last URL
            }
        }
    }
}