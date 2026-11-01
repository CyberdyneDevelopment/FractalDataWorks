using Fdw.Services.Authorization.Abstractions;

namespace Fdw.Services.Multitenancy;

/// <summary>
/// The rights the retention services ask of <see cref="IOrganizationScope"/> for a department's retention (FDW-2215).
/// </summary>
/// <remarks>
/// Whether the caller holds a right for a department, and which tenant it is in, is <see cref="IOrganizationScope"/>'s one answer, so a
/// department of another company and one the store does not hold are refused in the same way (FDW-2887).
/// </remarks>
internal static class RetentionScope
{
    internal const string PolicyReadRight = "retention:read";
    internal const string PolicyWriteRight = "retention:write";
    internal const string HoldReadRight = "legal-holds:read";
    internal const string HoldWriteRight = "legal-holds:write";
}
