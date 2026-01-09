using FastEndpoints;
using FluentValidation;
using core.jarvis.api.Systems;

namespace core.jarvis.api.Features.Auth.Register;

public class Validator : Validator<RegistrationRequest>
{
    public Validator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters");

        RuleFor(x => x.FullName)
            .MaximumLength(255).WithMessage("Name is too long (max 255 characters)")
            .When(x => !string.IsNullOrEmpty(x.FullName));
    }
}
