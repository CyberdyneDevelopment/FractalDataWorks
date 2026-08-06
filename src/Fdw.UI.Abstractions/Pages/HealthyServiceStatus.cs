using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Pages;

/// <summary>Service is healthy and operational.</summary>
[TypeOption(typeof(ServiceStatuses), "Healthy")]
[ExcludeFromCodeCoverage]
public sealed class HealthyServiceStatus : ServiceStatusBase
{
    /// <summary>Initializes a new instance of <see cref="HealthyServiceStatus"/>.</summary>
    public HealthyServiceStatus() : base(2, "Healthy") { }
}
