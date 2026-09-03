using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Authentication.Execution;

/// <summary>The wire shape of a <c>Subject</c>.</summary>
[ExcludeFromCodeCoverage]
internal sealed class SubjectDto
{
    public string Issuer { get; set; } = string.Empty;

    public string SubjectId { get; set; } = string.Empty;

    public DateTimeOffset AuthenticatedAt { get; set; }
}
