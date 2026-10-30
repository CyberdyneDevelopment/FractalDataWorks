using System;

namespace Fdw.Services.Authentication.Abstractions.Steps;

/// <summary>
/// Where a sign-in lands (FDW-2938): the tenant it acts in, and the department of it the person marked as their default, if they marked one.
/// </summary>
/// <param name="TenantId">The tenant the sign-in acts in.</param>
/// <param name="OrganizationId">The department the token already acts in; none when the person marked none, so the token acts in the tenant with no department.</param>
public sealed record Landing(Guid TenantId, Guid? OrganizationId);
