using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Components.Services;

/// <summary>
/// TypeCollection for semantic status variants.
/// </summary>
[TypeCollection(typeof(StatusVariantBase), typeof(IStatusVariant), typeof(StatusVariants))]
[ExcludeFromCodeCoverage]
public abstract partial class StatusVariants : TypeCollectionBase<StatusVariantBase, IStatusVariant> { }
