using System.Collections.Generic;
using FluentValidation.Results;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Results;

namespace Fdw.Services.Connections.Http;

/// <summary>
/// Generic HTTP connection configuration that works with any protocol.
/// Protocol-specific behavior is determined by the translator and protocol type.
/// Persisted to <c>conn.HttpConnection</c> as a child of <c>conn.Connection</c> via <see cref="HttpConnectionConfigurationBase.ConnectionId"/>.
/// </summary>
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Connection", ServiceType = "Http")]
public sealed partial class HttpConnectionConfiguration : HttpConnectionConfigurationBase
{
    /// <inheritdoc/>
    public override string ConnectionType => "Http";

    /// <summary>
    /// Validates this configuration.
    /// </summary>
    public IGenericResult<ValidationResult> Validate()
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(BaseUrl))
        {
            result.Errors.Add(new FluentValidation.Results.ValidationFailure(nameof(BaseUrl), "BaseUrl is required"));
        }

        if (TimeoutSeconds <= 0)
        {
            result.Errors.Add(new FluentValidation.Results.ValidationFailure(nameof(TimeoutSeconds), "TimeoutSeconds must be greater than 0"));
        }

        if (string.IsNullOrWhiteSpace(Protocol))
        {
            result.Errors.Add(new FluentValidation.Results.ValidationFailure(nameof(Protocol), "Protocol must be specified (Rest, Soap11, Soap12, GraphQL, OData, etc.)"));
        }

        return GenericResult<ValidationResult>.Success(result);
    }
}
