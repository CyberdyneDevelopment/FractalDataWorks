using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Multitenancy.Abstractions;

/// <summary>
/// The Documents feature (FDW-1580): upload, store and share documents on the connections assigned to the organization.
/// </summary>
[TypeOption(typeof(Features), "Documents")]
[ExcludeFromCodeCoverage]
public sealed class DocumentsFeature : FeatureBase
{
    /// <summary>Initializes a new instance of the <see cref="DocumentsFeature"/> class.</summary>
    public DocumentsFeature()
        : base("Documents", "Documents", "Upload, store and share documents on the connections assigned to the organization.")
    {
    }
}
