using System.ComponentModel.DataAnnotations;

namespace DexieNETCloudSample.Administration
{
    public class CloudKeyData(string? placeholderClientId = null, string? placeholderClientSecret = null)
    {
        [Required]
        public string ClientId { get; set; } = string.Empty;

        [Required]
        public string ClientSecret { get; set; } = string.Empty;

        public string PlaceholderClientId { get; set; } = placeholderClientId ?? string.Empty;
        public string PlaceholderClientSecret { get; set; } = placeholderClientSecret ?? string.Empty;
    }
}
