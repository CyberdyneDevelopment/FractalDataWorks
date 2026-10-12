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

        RuleFor(x => x.TimeoutSeconds)
            .GreaterThan(0)
            .WithMessage("TimeoutSeconds must be greater than 0");
    }
}
