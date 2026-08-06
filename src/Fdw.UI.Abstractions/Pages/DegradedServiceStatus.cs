using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Pages;

/// <summary>Service is degraded but operational.</summary>
[TypeOption(typeof(ServiceStatuses), "Degraded")]
[ExcludeFromCodeCoverage]
public sealed class DegradedServiceStatus : ServiceStatusBase
{
    /// <summary>Initializes a new instance of <see cref="DegradedServiceStatus"/>.</summary>
    public DegradedServiceStatus() : base(3, "Degraded") { }
}
