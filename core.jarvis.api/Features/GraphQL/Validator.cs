using FastEndpoints;
using FluentValidation;

namespace core.jarvis.api.Features.GraphQL;

public class Validator : Validator<GraphQLRequest>
{
    public Validator()
    {
        RuleFor(x => x.Query)
            .NotEmpty().WithMessage("Query is required");
    }
}
