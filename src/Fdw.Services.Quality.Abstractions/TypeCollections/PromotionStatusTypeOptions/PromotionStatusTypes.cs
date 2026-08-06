using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Quality.Abstractions.TypeCollections.PromotionStatusTypeOptions;

/// <summary>
/// TypeCollection for promotion status types.
/// Source generator will populate with all discovered TypeOptions.
/// </summary>
[TypeCollection(typeof(PromotionStatusTypeBase), typeof(IPromotionStatusType), typeof(PromotionStatusTypes))]
public sealed partial class PromotionStatusTypes : TypeCollectionBase<PromotionStatusTypeBase, IPromotionStatusType>
{
}
