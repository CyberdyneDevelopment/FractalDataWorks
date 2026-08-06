using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Pages;

/// <summary>Status is unknown or not yet checked.</summary>
[TypeOption(typeof(ServiceStatuses), "Unknown")]
[ExcludeFromCodeCoverage]
public sealed class UnknownServiceStatus : ServiceStatusBase
{
    /// <summary>Initializes a new instance of <see cref="UnknownServiceStatus"/>.</summary>
    public UnknownServiceStatus() : base(1, "Unknown") { }
}
