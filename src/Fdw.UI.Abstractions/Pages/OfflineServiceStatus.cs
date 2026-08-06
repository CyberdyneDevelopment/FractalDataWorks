using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Pages;

/// <summary>Service is disabled or offline.</summary>
[TypeOption(typeof(ServiceStatuses), "Offline")]
[ExcludeFromCodeCoverage]
public sealed class OfflineServiceStatus : ServiceStatusBase
{
    /// <summary>Initializes a new instance of <see cref="OfflineServiceStatus"/>.</summary>
    public OfflineServiceStatus() : base(5, "Offline") { }
}
