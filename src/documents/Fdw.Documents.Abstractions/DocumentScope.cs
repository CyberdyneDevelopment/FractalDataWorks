using System;
using Fdw.Documents.Abstractions.Results;
using Fdw.Results;

namespace Fdw.Documents.Abstractions;

/// <summary>
/// Where a document connection may reach (FDW-1498): the account, a container, or a folder within one, as the connection's root
/// path. Every path a command names is relative to it, and every resolved path is checked to lie inside it, in one place.
/// </summary>
public sealed class DocumentScope
{
    /// <summary>Initializes a new instance of the <see cref="DocumentScope"/> class.</summary>
    /// <param name="root">The root path of the scope, within the account or container the connection reaches; the root is the whole of it.</param>
    public DocumentScope(DocumentPath root)
    {
        Root = root;
    }

    /// <summary>Gets the root path of the scope.</summary>
    public DocumentPath Root { get; }

    /// <summary>Resolves a path relative to the scope into the path the storage is asked for.</summary>
    /// <param name="relative">A path relative to the scope.</param>
    /// <returns>The path under the scope's root, or the failure when it lies outside the scope.</returns>
    public IGenericResult<DocumentPath> Resolve(DocumentPath relative) => Check(Root.Combine(relative), relative);

    /// <summary>Resolves text relative to the scope.</summary>
    /// <param name="relative">Text that is read as a path relative to the scope.</param>
    /// <returns>The resolved path, or the failure that names what is wrong with the text or that it lies outside the scope.</returns>
    public IGenericResult<DocumentPath> Resolve(string? relative)
    {
        var parsed = DocumentPath.Parse(relative);
        return parsed.IsFailure ? parsed : Resolve(parsed.Value);
    }

    /// <summary>Says whether a path the storage reports lies inside the scope.</summary>
    /// <param name="resolved">A path under the account or container, as the storage names it.</param>
    /// <returns>True when it is the scope's root or below it.</returns>
    public bool Contains(DocumentPath resolved) => resolved.IsWithin(Root);

    /// <summary>Turns a path the storage reports back into one relative to the scope.</summary>
    /// <param name="resolved">A path under the scope's root.</param>
    /// <returns>The relative path, or the failure when the path is outside the scope.</returns>
    public IGenericResult<DocumentPath> Relative(DocumentPath resolved)
    {
        if (!Contains(resolved))
        {
            return Outside(resolved);
        }

        var below = Root.IsRoot ? resolved.Value : resolved.Value.Substring(Math.Min(resolved.Value.Length, Root.Value.Length + 1));
        return DocumentPath.Parse(below);
    }

    private IGenericResult<DocumentPath> Check(DocumentPath resolved, DocumentPath relative) =>
        Contains(resolved) ? GenericResult<DocumentPath>.Success(resolved) : Outside(relative);

    private IGenericResult<DocumentPath> Outside(DocumentPath path) =>
        GenericResult<DocumentPath>.Failure(
            DocumentResultCodes.ByName("DocumentOutsideScope"),
            ResultDetails.Create("Path", path.Value, "Scope", Root.Value));
}
