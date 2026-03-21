namespace UserService.API.Models.DTOs
{
    public sealed record UserCreatedDto(Guid Id, string UserName, string FirstName, string LastName, string Email)
    {
        public static UserCreatedDto Create(User model)
            => new UserCreatedDto(model.Id, model.UserName, model.FirstName, model.LastName, model.Email);
    }
}
