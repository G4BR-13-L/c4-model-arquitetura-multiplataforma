namespace UserService.API.Models
{
    public sealed class User
    {
        public User()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.Now;
        }

        public Guid Id { get; init; }
        public string UserName { get; init; }
        public string FirstName { get; init; }
        public string LastName { get; init; }
        public string Email { get; init; }
        public string DocumentNumber { get; init; }
        public string KeyCloakId { get; private set; }
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; private set; }

        public void SetIdpId(string idpId)
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(idpId);

            if (KeyCloakId is not null)
                throw new InvalidOperationException("KeyCloakId já foi atribuido anteriormente!");

            KeyCloakId = idpId;
            UpdatedAt = DateTime.Now;
        }
    }
}
