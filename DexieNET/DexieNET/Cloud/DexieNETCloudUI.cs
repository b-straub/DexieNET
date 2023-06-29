/*
DexieNETCloudUI.cs

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

using System.Text.RegularExpressions;

namespace DexieNET
{
    public record UIAlert(UIAlert.AlertType Type, UIAlert.MessageCode Code, string Message, Dictionary<string, string> Params)
    {
        public enum AlertType
        {
            INFO,
            WARNING,
            ERROR
        }

        public enum MessageCode
        {
            OTP_SENT,
            INVALID_OTP,
            INVALID_EMAIL,
            LICENSE_LIMIT_REACHED,
            GENERIC_INFO,
            GENERIC_WARNING,
            GENERIC_ERROR,
        }

        public string GetMessage()
        {
            var message = Message;
            var messageParams = Params;
            var messageFormated = UIInteractionRegex.Params().Replace(message, m =>
            {
                return messageParams[m.Value[1..^1]];
            });
            return messageFormated;
        }
    }

    public record UIField(UIField.FieldType Type, string? Label, string? Placeholder)
    {
        public enum FieldType
        {
            TEXT,
            EMAIL,
            OTP,
            PASSWORD
        }
    }

    public record UIParam(string Value, UIInteraction.InteractionType Type)
    {
        public Dictionary<string, string> AsDictionary()
        {
            var paramName = Type switch
            {
                UIInteraction.InteractionType.EMAIL => "email",
                UIInteraction.InteractionType.OTP => "otp",
                _ => throw new InvalidOperationException("UIParam: invalid type.")
            };

            return new Dictionary<string, string>
            {
                { paramName, Value }
            };
        }
    }

    public record UIInteraction(UIInteraction.InteractionType Type, string Title, UIAlert[] Alerts,
        UIField? Fields, double Key)
    {
        public enum InteractionType
        {
            EMAIL,
            OTP,
            ALERT
        }
    }

    internal static partial class UIInteractionRegex
    {
        [GeneratedRegex("{\\w+}", RegexOptions.IgnoreCase)]
        public static partial Regex Params();
    }
}
