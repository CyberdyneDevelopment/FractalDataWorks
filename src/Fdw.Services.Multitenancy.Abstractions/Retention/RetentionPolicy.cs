using System;

namespace Fdw.Services.Multitenancy.Abstractions;

/// <summary>
/// How long an organization keeps documents of one type, which date the period counts from and what happens at the end (FDW-1575).
/// </summary>
/// <param name="Id">The policy's identifier.</param>
/// <param name="OrganizationId">The organization the policy applies to.</param>
/// <param name="DocumentType">The document type the policy covers; the application names its types, the platform does not.</param>
/// <param name="Years">Whole years of the period.</param>
/// <param name="Months">Whole months of the period, added after the years.</param>
/// <param name="Days">Days of the period, added after the months.</param>
/// <param name="StartField">The document date the period counts from, designated by an administrator (for example a signed date).</param>
/// <param name="EndAction">The name of the action the application performs on a document when its period ends (for example Delete or Archive).</param>
public sealed record RetentionPolicy(Guid Id, Guid OrganizationId, string DocumentType, int Years, int Months, int Days, string StartField, string EndAction);
