namespace Fdw.Services.Multitenancy.Abstractions;

/// <summary>
/// What an administrator states to set the retention policy of a document type (FDW-1575).
/// </summary>
/// <param name="DocumentType">The document type the policy covers.</param>
/// <param name="Years">Whole years of the period.</param>
/// <param name="Months">Whole months of the period, added after the years.</param>
/// <param name="Days">Days of the period, added after the months.</param>
/// <param name="StartField">The document date the period counts from.</param>
/// <param name="EndAction">The name of the action the application performs when the period ends.</param>
public sealed record NewRetentionPolicy(string DocumentType, int Years, int Months, int Days, string StartField, string EndAction);
