using FastEndpoints;
using FluentValidation;
using AccountModel = core.jarvis.api.Models.Account;

namespace core.jarvis.api.Features.Auth.Login;

public class Validator : Validator<AccountModel>
{
    public Validator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required");
    }
}
