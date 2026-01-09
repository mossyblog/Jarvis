using FastEndpoints;
using FluentValidation;
using core.jarvis.api.Models;

namespace core.jarvis.api.Features.Auth.Refresh;

public class Validator : Validator<RefreshTokenRequest>
{
    public Validator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required");
    }
}
