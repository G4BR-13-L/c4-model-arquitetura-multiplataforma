
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using UserService.API.Infra.Repositories;
using UserService.API.Models.KeyCloak;

namespace UserService.API.Infra.Auth
{
    public class KeyCloakAdminAuthDelegatingHandler : DelegatingHandler
    {
        private readonly IKeyCloakAuthRepository _keyCloakAuthRepository;
        private readonly ILogger<KeyCloakAdminAuthDelegatingHandler> _logger;

        public KeyCloakAdminAuthDelegatingHandler(IKeyCloakAuthRepository keyCloakAuthRepository, ILogger<KeyCloakAdminAuthDelegatingHandler> logger)
        {
            _keyCloakAuthRepository = keyCloakAuthRepository;
            _logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (!request.Headers.TryGetValues("Authorization", out var values))
            {
                _logger.LogInformation("Header Authorization não encontrado. Obtendo token de acesso");
                var token = await _keyCloakAuthRepository.GetTokenAsync();
                if (token is null)
                {
                    _logger.LogWarning("Falha ao obter token de acesso. Retornando Unauthorized");
                    return new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized);
                }

                request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {token.AccessToken}");
                _logger.LogInformation("Token de acesso adicionado ao header Authorization");
            }

            _logger.LogInformation("Encaminhando requisição para {Uri}", request.RequestUri);
            return await base.SendAsync(request, cancellationToken);
        }
    }    
}
