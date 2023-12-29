
using DexieNET;
using DexieNETCloudSample.Dialogs;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace DexieNETCloudSample.Logic
{
    public static class Authenticate
    {
        public static async Task<bool> HandleUIInteraction(DBBase db, UIInteraction ui, IDialogService dialogService)
        {
            switch (ui.Type)
            {
                case UIInteraction.InteractionType.EMAIL:
                    {
                        EmailData data = new(ui.Fields?.Placeholder);

                        var parameters = new DialogParameters { ["Item"] = data };
                        var dialog = dialogService.Show<AuthenticateEMail>(ui.Title, parameters);

                        var result = await dialog.Result;
                        if (!result.Canceled)
                        {
                            data = (EmailData)result.Data;
                            UIParam emailParam = new(data.Email, ui.Type);
                            db.SubmitUserInteraction(ui, emailParam);
                        }
                        else
                        {
                            db.CancelUserInteraction(ui);
                        }
                    }
                    break;

                case UIInteraction.InteractionType.OTP:
                    {
                        OTPData data = new(ui.Alerts);

                        var parameters = new DialogParameters { ["Item"] = data };
                        var dialog = dialogService.Show<AuthenticateOTP>(ui.Title, parameters);

                        var result = await dialog.Result;
                        if (!result.Canceled)
                        {
                            data = (OTPData)result.Data;
                            UIParam otpParam = new(data.OTP.ToUpperInvariant(), ui.Type);
                            db.SubmitUserInteraction(ui, otpParam);
                        }
                        else
                        {
                            db.CancelUserInteraction(ui);
                        }
                    }
                    break;

                case UIInteraction.InteractionType.MESSAGE_ALERT:
                    {
                        await dialogService.ShowMessageBox(ui.Title, FormatAlertMessage(ui.Alerts), yesText: "OK");
                    }
                    break;

                case UIInteraction.InteractionType.LOGOUT_CONFIRMATION:
                    {
                        ArgumentNullException.ThrowIfNull(ui.Alerts.FirstOrDefault());

                        var currentUserId = ui.Alerts.First().Params["currentUserId"];
                        var parameters = new DialogParameters { ["Message"] = FormatAlertMessage(ui.Alerts).Value, ["ConfirmButton"] = "Logout", ["SuccessOnConfirm"] = false };
                        var dialog = dialogService.Show<ConfirmDialog>($"{ui.Title} for '{currentUserId}'", parameters);

                        var res = await dialog.Result;

                        if (res.Canceled)
                        {
                            return false;
                        }
                    }
                    break;
                default: throw new InvalidOperationException($"HandleUIInteraction: Type {ui.Type} not handled.");
            }

            return true;
        }

        private static MarkupString FormatAlertMessage(UIAlert[] alerts)
        {
            var markUpString = string.Empty;
            bool firstLine = true;

            foreach (var alert in alerts)
            {
                var message = alert.GetMessage();
                markUpString = firstLine ? message : "<br />" + markUpString + message;
                firstLine = false;
            }

            return (MarkupString)markUpString;
        }
    }
}
