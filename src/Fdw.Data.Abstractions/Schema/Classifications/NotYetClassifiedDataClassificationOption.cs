using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions;

/// <summary>Data nobody has classified yet.</summary>
/// <remarks>
/// The level every <c>ClassificationId</c> carries until someone picks one: each column is NOT NULL and
/// defaults to this level's Id, 470168002, and each record's <c>ClassificationId</c> starts at it. It is a
/// real level with a row in <c>data.DataClassification</c>, unlike <see cref="DataClassifications.NotFound"/>,
/// which is the collection's lookup miss and is refused wherever a classification is named. Sensitivity 0
/// places it before Public; it says nothing about how sensitive the data is, only that nobody has decided.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataClassifications), "NotYetClassified", RestrictToCurrentCompilation = true)]
public sealed class NotYetClassifiedDataClassificationOption : DataClassificationBase
{
    /// <summary>Initializes a new instance of the <see cref="NotYetClassifiedDataClassificationOption"/> class.</summary>
    public NotYetClassifiedDataClassificationOption()
        : base(
            name: "NotYetClassified",
            sensitivity: 0,
            requiresInvitation: false,
            description: "Not classified yet: the level every item carries until someone picks one.")
    {
    }
}
