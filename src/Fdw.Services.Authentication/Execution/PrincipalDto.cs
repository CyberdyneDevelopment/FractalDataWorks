using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Authentication.Execution;

/// <summary>The wire shape of a <c>Principal</c>.</summary>
[ExcludeFromCodeCoverage]
internal sealed class PrincipalDto
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }
}
