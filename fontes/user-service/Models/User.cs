namespace UserService.API.Models
{
    public sealed class User
    {
        public User()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
        }

        public Guid Id { get; set; }
        public string UserName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string IdpId { get; private set; }
        public DateTime CreatedAt { get; set; }

        public void SetIdpId(string idpId)
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(idpId);

            if (IdpId is not null)
                throw new InvalidOperationException("IdpId já foi atribuido anteriormente!");

            IdpId = idpId;
        }
    }
}
