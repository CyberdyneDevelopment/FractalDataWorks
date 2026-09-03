using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Authentication.Execution;

/// <summary>
/// The wire shape <c>AuthenticationContext</c> is serialized to before encryption.
/// </summary>
/// <remarks>
/// A dedicated DTO rather than serializing <c>AuthenticationContext</c> directly: <c>Claim.Source</c>
/// is <c>IClaimSource</c>, a TypeCollection option, not itself JSON-serializable — it is carried here
/// by <see cref="ClaimDto.Source"/>'s name and restored through <c>ClaimSources.ByName</c>.
/// </remarks>
[ExcludeFromCodeCoverage]
internal sealed class ContextDataDto
{
    public SubjectDto? Subject { get; set; }

    public PrincipalDto? Principal { get; set; }

    public ClaimDto[] Claims { get; set; } = [];

    public DecisionDto? Decision { get; set; }

    public string[] AchievedMethods { get; set; } = [];

    public string? AchievedAcr { get; set; }
}
