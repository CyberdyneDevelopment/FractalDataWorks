using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Components.Services;

/// <summary>
/// TypeCollection for semantic status colors.
/// </summary>
[TypeCollection(typeof(StatusColorBase), typeof(IStatusColor), typeof(StatusColors))]
[ExcludeFromCodeCoverage]
public abstract partial class StatusColors : TypeCollectionBase<StatusColorBase, IStatusColor> { }
