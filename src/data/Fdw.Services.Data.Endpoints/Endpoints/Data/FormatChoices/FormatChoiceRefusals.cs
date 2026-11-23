using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Data.Endpoints;

/// <summary>The refusals of a format's choices that a stream answers with its own coded failure, found by the message number.</summary>
/// <remarks>A number this collection does not hold answers the NotFound sentinel: the failure is not a refusal of choices, and the caller leaves it as it is.</remarks>
[TypeCollection(typeof(FormatChoiceRefusalBase), typeof(IFormatChoiceRefusal), typeof(FormatChoiceRefusals))]
[ExcludeFromCodeCoverage]
public abstract partial class FormatChoiceRefusals : TypeCollectionBase<FormatChoiceRefusalBase, IFormatChoiceRefusal>
{
}
