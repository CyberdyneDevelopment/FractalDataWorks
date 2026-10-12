using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Pages;

/// <summary>
/// TypeCollection for service status values.
/// </summary>
[TypeCollection(typeof(ServiceStatusBase), typeof(IServiceStatus), typeof(ServiceStatuses))]
[ExcludeFromCodeCoverage]
public abstract partial class ServiceStatuses : TypeCollectionBase<ServiceStatusBase, IServiceStatus> { }
