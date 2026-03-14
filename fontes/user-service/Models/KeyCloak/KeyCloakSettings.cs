using System.ComponentModel.DataAnnotations;

namespace UserService.API.Models.KeyCloak
{
    public sealed class KeyCloakSettings
    {
        [Required]
        public string BaseUrl { get; init; }

        [Required]
        public string Realm { get; init; }

        [Required]
        public string ClientId { get; init; }

        [Required]
        public string ClientSecret { get; init; }
    }
}
