namespace UserService.API.Models
{
    public sealed class User
    {
        public User()
        {
            CreatedAt = DateTimeOffset.Now;
        }

        public string UserName { get; init; }
        public string FirstName { get; init; }
        public string LastName { get; init; }
        public string Email { get; init; }
        public string IdpId { get; private set; }
        public DateTimeOffset CreatedAt { get; }

        public void SetIdpId(string idpId)
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(idpId);

            if (IdpId is not null)
                throw new InvalidOperationException("IdpId já foi atribuido anteriormente!");

            IdpId = idpId;
        }
    }
}
