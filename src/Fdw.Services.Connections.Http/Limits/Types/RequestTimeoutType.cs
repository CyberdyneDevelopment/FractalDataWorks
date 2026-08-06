using Fdw.Collections.Attributes;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Connections.Http.Limits.Types;

/// <summary>
/// TypeOption for the RequestTimeout connection limit kind on Http connections.
/// Cancels outbound HTTP requests that exceed the configured duration.
/// Subtype configuration is stored in <c>conn.HttpRequestTimeout</c>.
/// </summary>
[TypeOption(typeof(HttpConnectionLimitTypes), "RequestTimeout")]
public sealed class RequestTimeoutType : ConnectionLimitTypeBase
{
    /// <summary>Initializes a new instance of <see cref="RequestTimeoutType"/>.</summary>
    public RequestTimeoutType()
        : base(
            4,
            "RequestTimeout",
            "Request Timeout",
            "Cancels HTTP requests that exceed the specified duration.",
            [
                new ConfigurationFieldDescriptor(
                    "TimeoutSeconds",
                    "Timeout (seconds)",
                    "e.g. 30",
                    ConfigurationFieldKinds.Numeric,
                    IsRequired: true),
            ])
    {
    }
}
