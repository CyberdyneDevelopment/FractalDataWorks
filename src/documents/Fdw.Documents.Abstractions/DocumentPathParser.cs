using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Documents.Abstractions.Results;
using Fdw.Results;

namespace Fdw.Documents.Abstractions;

/// <summary>Reads text as a <see cref="DocumentPath"/> (FDW-1498).</summary>
internal static class DocumentPathParser
{
    internal static IGenericResult<DocumentPath> Parse(string? text)
    {
        if (text is null || text.Length == 0)
        {
            return GenericResult<DocumentPath>.Success(DocumentPath.Root);
        }

        var segments = text.Replace('\\', '/').Split(['/'], StringSplitOptions.RemoveEmptyEntries);
        var problem = segments.Select(Problem).FirstOrDefault(found => found is not null);
        if (problem is not null)
        {
            return Refused(text, problem);
        }

        return GenericResult<DocumentPath>.Success(DocumentPath.FromNormalised(string.Join("/", segments)));
    }

    // A parent segment is how a path climbs out of its scope; a current-directory segment says nothing and hides one that does.
    private static string? Problem(string segment)
    {
        if (segment is ".." || segment is ".")
        {
            return $"the segment '{segment}' is not allowed";
        }

        return segment.Any(char.IsControl) ? "a segment holds a control character" : null;
    }

    private static IGenericResult<DocumentPath> Refused(string text, string reason) =>
        GenericResult<DocumentPath>.Failure(
            DocumentResultCodes.ByName("DocumentPathInvalid"),
            ResultDetails.Create("Path", text, "Reason", reason));
}
