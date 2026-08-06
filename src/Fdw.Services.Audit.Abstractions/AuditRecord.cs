using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Audit.Abstractions;

/// <summary>
/// Represents a single audit trail record.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed record AuditRecord(
    Guid Id,
    string EntityType,
    string EntityId,
    string Action,
    string? BeforeJson,
    string? AfterJson,
    string? ChangedFields,
    string UserId,
    string? UserName,
    string? IpAddress,
    DateTimeOffset Timestamp,
    Guid? CorrelationId);
