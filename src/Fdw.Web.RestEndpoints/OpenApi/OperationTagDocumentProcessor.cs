using System;
using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;

namespace Fdw.Web.RestEndpoints.OpenApi;

/// <summary>
/// Gives every operation in a document exactly one tag, the group <see cref="OperationTags"/> names (FDW-1100).
/// </summary>
/// <remarks>
/// Attach it with FastEndpoints' own path tagging turned off (<c>AutoTagPathSegmentIndex = 0</c>): that tagging
/// adds a second, differently cased group to every operation, and a route made only of parameters would hand this
/// a parameter name as though the endpoint had declared it. Depends on the build alone, so it is safe in a
/// document generated once and cut per caller.
/// </remarks>
public sealed class OperationTagDocumentProcessor : IDocumentProcessor
{
    /// <inheritdoc />
    public void Process(DocumentProcessorContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        foreach (var (path, pathItem) in context.Document.Paths)
        {
            foreach (var operation in pathItem.Values)
            {
                var tag = OperationTags.For(path, [.. operation.Tags]);
                operation.Tags.Clear();
                if (tag is not null)
                    operation.Tags.Add(tag);
            }
        }
    }
}
