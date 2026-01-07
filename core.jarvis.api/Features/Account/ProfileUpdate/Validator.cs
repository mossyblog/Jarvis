using FastEndpoints;
using FluentValidation;
using core.jarvis.api.Models;

namespace core.jarvis.api.Features.Account.ProfileUpdate;

public class Validator : Validator<SecurityProfile>
{
    public Validator()
    {
        RuleFor(x => x.Name)
            .MaximumLength(255).WithMessage("Name is too long (max 255 characters)")
            .When(x => !string.IsNullOrEmpty(x.Name));
    }
}
