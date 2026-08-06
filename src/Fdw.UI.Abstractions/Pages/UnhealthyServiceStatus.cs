using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Pages;

/// <summary>Service is unhealthy or failed.</summary>
[TypeOption(typeof(ServiceStatuses), "Unhealthy")]
[ExcludeFromCodeCoverage]
public sealed class UnhealthyServiceStatus : ServiceStatusBase
{
    /// <summary>Initializes a new instance of <see cref="UnhealthyServiceStatus"/>.</summary>
    public UnhealthyServiceStatus() : base(4, "Unhealthy") { }
}
