using FluentValidation;
using Fdw.Validation;

namespace Fdw.Services.Connections.Http.Validation;

/// <summary>
/// Validator for <see cref="HttpConnectionConfiguration"/>.
/// </summary>
public sealed class HttpConnectionConfigurationValidator : FdwConfigurationValidator<HttpConnectionConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HttpConnectionConfigurationValidator"/> class.
    /// </summary>
    public HttpConnectionConfigurationValidator()
    {

        RuleFor(x => x.BaseUrl)
            .NotEmpty()
            .WithMessage("BaseUrl is required");

        RuleFor(x => x.Protocol)
            .NotEmpty()
            .WithMessage("Protocol is required (e.g., Rest, Soap11, Soap12, GraphQL, OData)");

        // No rule for Lifetime (FDW-5139): it has no column and nothing reads it, so the stored row always comes back without one, and
        // requiring it refused every update of every HTTP connection.

        RuleFor(x => x.AuthenticationType)
            .NotEmpty()
            .WithMessage("AuthenticationType is required (None, Basic, Bearer, ApiKey, UsernameToken or WsSecurity)");

        RuleFor(x => x.TimeoutSeconds)
            .GreaterThan(0)
            .When(x => x.TimeoutSeconds is not null)
            .WithMessage("TimeoutSeconds must be greater than 0 when it is stated; leave it out to take the platform's timeout");
    }
}
