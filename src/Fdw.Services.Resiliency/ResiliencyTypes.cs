using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Services.Resiliency.Abstractions;

namespace Fdw.Services.Resiliency;

/// <summary>
/// TypeCollection of pluggable resiliency execution strategies.
/// Each entry implements <see cref="IResiliencyType"/> and encapsulates one strategy:
/// retry logic, backup source routing, notify-on-failure, or no-op.
///
/// The source generator populates ByName(), ById(), All(), NotFound() at compile time.
///
/// Usage:
///   var strategyType = ResiliencyTypes.ByName("PollyRetry");
///   if (strategyType == ResiliencyTypes.NotFound) { ... }
///   await strategyType.Execute(runStage, config, ctx, ct);
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(
    typeof(ResiliencyTypeBase),
    typeof(IResiliencyType),
    typeof(ResiliencyTypes))]
public abstract partial class ResiliencyTypes
    : TypeCollectionBase<ResiliencyTypeBase, IResiliencyType>
{
}
