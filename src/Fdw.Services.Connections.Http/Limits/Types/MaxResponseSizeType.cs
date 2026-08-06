using Fdw.Collections.Attributes;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Connections.Http.Limits.Types;

/// <summary>
/// TypeOption for the MaxResponseSize connection limit kind on Http connections.
/// Caps the response payload accepted from external HTTP services.
/// Subtype configuration is stored in <c>conn.HttpMaxResponseSize</c>.
/// </summary>
[TypeOption(typeof(HttpConnectionLimitTypes), "MaxResponseSize")]
public sealed class MaxResponseSizeType : ConnectionLimitTypeBase
{
    /// <summary>Initializes a new instance of <see cref="MaxResponseSizeType"/>.</summary>
    public MaxResponseSizeType()
        : base(
            2,
            "MaxResponseSize",
            "Max Response Size",
            "Caps the size of HTTP responses in megabytes.",
            [
                new ConfigurationFieldDescriptor(
                    "MaxMb",
                    "Max Size (MB)",
                    "e.g. 50",
                    ConfigurationFieldKinds.Numeric,
                    IsRequired: true),
            ])
    {
    }
}
