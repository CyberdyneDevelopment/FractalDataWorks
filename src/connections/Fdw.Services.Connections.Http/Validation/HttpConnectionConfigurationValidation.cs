using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Connections.Http;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Connections.Http.Validation;

/// <summary>States that <see cref="HttpConnectionConfiguration"/> is checked by <see cref="HttpConnectionConfigurationValidator"/> when it is written (FDW-3541).</summary>
/// <remarks>The connection endpoints run the same validator on create and update through the connection type; this runs it again on any write through the domain provider.</remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "HttpConnectionConfiguration")]
public sealed class HttpConnectionConfigurationValidation : ValidatedConfiguration<HttpConnectionConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="HttpConnectionConfigurationValidation"/> class.</summary>
    public HttpConnectionConfigurationValidation()
        : base("HttpConnectionConfiguration", new HttpConnectionConfigurationValidator())
    {
    }
}
