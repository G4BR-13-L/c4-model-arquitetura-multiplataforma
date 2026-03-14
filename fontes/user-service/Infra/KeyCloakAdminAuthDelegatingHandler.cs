
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using UserService.API.Infra.Repositories;
using UserService.API.Models.KeyCloak;

namespace UserService.API.Infra
{
    public class KeyCloakAdminAuthDelegatingHandler : DelegatingHandler
    {
        private readonly IKeyCloakAuthRepository _keyCloakAuthRepository;

        public KeyCloakAdminAuthDelegatingHandler(IKeyCloakAuthRepository keyCloakAuthRepository)
        {
            _keyCloakAuthRepository = keyCloakAuthRepository;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (!request.Headers.TryGetValues("Authorization", out var values))
            {
                var token = await _keyCloakAuthRepository.GetTokenAsync();
                if (token is null)
                    return new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized);

                request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {token.AccessToken}");
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }    
}
