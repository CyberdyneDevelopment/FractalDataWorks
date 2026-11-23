using System;
using System.Collections.Generic;
using Fdw.Documents.Abstractions;
using Fdw.Documents.Abstractions.Commands;
using Fdw.Results;

namespace Fdw.Services.Connections.NarrowedDocuments;

/// <summary>Places the paths of a command under a folder, and makes the paths of a result relative to it again.</summary>
internal static class NarrowedPaths
{
    internal static IGenericResult<IDocumentCommand> Below(DocumentScope folder, DocumentPath path, Func<DocumentPath, IDocumentCommand> build)
    {
        var resolved = folder.Resolve(path);
        return resolved.IsFailure ? resolved.ToNewResult<IDocumentCommand>() : GenericResult<IDocumentCommand>.Success(build(resolved.Value));
    }

    internal static IGenericResult<IDocumentCommand> BelowBoth(
        DocumentScope folder, DocumentPath first, DocumentPath second, Func<DocumentPath, DocumentPath, IDocumentCommand> build)
    {
        var one = folder.Resolve(first);
        var other = folder.Resolve(second);
        if (one.IsFailure)
        {
            return one.ToNewResult<IDocumentCommand>();
        }

        return other.IsFailure ? other.ToNewResult<IDocumentCommand>() : GenericResult<IDocumentCommand>.Success(build(one.Value, other.Value));
    }

    internal static IGenericResult<DocumentInfo> RelativeInfo(DocumentScope folder, DocumentInfo info)
    {
        var relative = folder.Relative(info.Path);
        return relative.IsFailure
            ? relative.ToNewResult<DocumentInfo>()
            : GenericResult<DocumentInfo>.Success(new DocumentInfo(
                relative.Value, info.Length, info.ContentType, info.LastModified, info.ContentHash, info.Version, info.Metadata));
    }

    internal static IGenericResult<DocumentPage> RelativePage(DocumentScope folder, DocumentPage page)
    {
        var items = new List<DocumentInfo>(page.Items.Count);
        foreach (var item in page.Items)
        {
            var relative = RelativeInfo(folder, item);
            if (relative.IsFailure || relative.Value is null)
            {
                return relative.ToNewResult<DocumentPage>();
            }

            items.Add(relative.Value);
        }

        return GenericResult<DocumentPage>.Success(new DocumentPage(items, page.Continuation));
    }

    internal static IGenericResult<DocumentContent> RelativeContent(DocumentScope folder, DocumentContent content)
    {
        var relative = RelativeInfo(folder, content.Info);
        if (relative.IsFailure || relative.Value is null)
        {
            content.Dispose();
            return relative.ToNewResult<DocumentContent>();
        }

        return GenericResult<DocumentContent>.Success(new DocumentContent(relative.Value, content.Content));
    }
}
