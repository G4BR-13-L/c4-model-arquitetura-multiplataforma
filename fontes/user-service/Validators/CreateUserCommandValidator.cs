using FluentValidation;
using UserService.API.Models.Commands;

namespace UserService.API.Validators
{
    public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
    {
        public CreateUserCommandValidator()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("O campo 'first_name' é obrigatório.")
                .MaximumLength(100).WithMessage("O campo 'first_name' deve ter no máximo 100 caracteres.");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("O campo 'last_name' é obrigatório.")
                .MaximumLength(100).WithMessage("O campo 'last_name' deve ter no máximo 100 caracteres.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("O campo 'email' é obrigatório.")
                .EmailAddress().WithMessage("O campo 'email' deve ser um endereço de e-mail válido.")
                .MaximumLength(200).WithMessage("O campo 'email' deve ter no máximo 200 caracteres.");

            RuleFor(x => x.UserName)
                .NotEmpty().WithMessage("O campo 'username' é obrigatório.")
                .MaximumLength(100).WithMessage("O campo 'username' deve ter no máximo 100 caracteres.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("O campo 'password' é obrigatório.")
                .MinimumLength(6).WithMessage("O campo 'password' deve ter no mínimo 6 caracteres.");

            RuleFor(x => x.DocumentNumber)
                .NotEmpty().WithMessage("O campo 'document_number' é obrigatório.")
                .MaximumLength(50).WithMessage("O campo 'document_number' deve ter no máximo 50 caracteres.");
        }
    }
}
