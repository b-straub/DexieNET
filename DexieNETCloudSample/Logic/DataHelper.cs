using DexieNET;
using System.ComponentModel.DataAnnotations;

namespace DexieNETCloudSample.Logic
{
    public class ToDoListData(string title)
    {
        [Required(AllowEmptyStrings = false)]
        public string Title { get; set; } = title;

        [Required]
        public DateTime DueDate { get; set; }
    }

    public class ToDoItemData(string text, DateTime date)
    {
        [Required(AllowEmptyStrings = false)]
        public string Text { get; set; } = text;

        [Required]
        public DateTime DueDate { get; set; } = date;
    }

    public class EmailData(string? placeholder)
    {
        [Required(AllowEmptyStrings = false)]
        [RegularExpression(@"^[\w\-+.]+@([\w-]+\.)+[\w-]{2,10}(\sas\s[\w\-+.]+@([\w-]+\.)+[\w-]{2,10})?$",
            ErrorMessage = "Enter a valid eMail address or eMail1 as eMail2!")]
        // https://github.com/dexie/Dexie.js/blob/81ce60736455470742e6b4c12a9e4f10e73f7c60/addons/dexie-cloud/src/authentication/interactWithUser.ts#L100
        public string Email { get; set; } = string.Empty;

        public string Placeholder { get; set; } = placeholder ?? string.Empty;
    }

    public class OTPData(IEnumerable<UIAlert> alerts)
    {
        [StringLength(8, MinimumLength = 8)]
        [RegularExpression(@"([a-zA-Z0-9]+)", ErrorMessage = "Only letters and numbers allowed")]
        public string OTP { get; set; } = string.Empty;

        public IEnumerable<UIAlert> Alerts { get; set; } = alerts;
    }
}
