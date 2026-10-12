using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.UI.Abstractions.Pages;

/// <summary>
/// Base class for service status.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class ServiceStatusBase : TypeOptionBase<ServiceStatusBase>, IServiceStatus
{
    /// <summary>
    /// Initializes a new instance of <see cref="ServiceStatusBase"/>.
    /// </summary>
    protected ServiceStatusBase(string name) : base(name) { }
}
