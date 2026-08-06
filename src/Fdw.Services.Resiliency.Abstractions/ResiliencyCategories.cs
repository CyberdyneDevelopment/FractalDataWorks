using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Resiliency.Abstractions;

/// <summary>
/// TypeCollection for resiliency policy categories.
/// </summary>
[TypeCollection(typeof(ResiliencyCategoryBase), typeof(IResiliencyCategory), typeof(ResiliencyCategories))]
[ExcludeFromCodeCoverage]
public abstract partial class ResiliencyCategories : TypeCollectionBase<ResiliencyCategoryBase, IResiliencyCategory> { }
