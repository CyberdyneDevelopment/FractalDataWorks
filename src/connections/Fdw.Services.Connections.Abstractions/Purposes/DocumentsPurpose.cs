using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Abstractions.Purposes;

/// <summary>Document files (FDW-1581): a document connection, which a grant and an assignment may narrow to a folder.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConnectionPurposes), "Documents", RestrictToCurrentCompilation = true)]
public sealed class DocumentsPurpose : ConnectionPurposeBase
{
    /// <summary>Initializes a new instance of the <see cref="DocumentsPurpose"/> class.</summary>
    public DocumentsPurpose()
        : base("Documents", "Document files, kept on a document connection.", takesFolder: true)
    {
    }

    /// <inheritdoc />
    public override string? Unfit(IGenericConnection connection) =>
        connection is IDocumentConnection ? null : "it is not a document connection";
}
