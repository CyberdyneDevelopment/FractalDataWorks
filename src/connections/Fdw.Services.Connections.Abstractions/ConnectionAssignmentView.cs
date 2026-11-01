using System;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// The connection an organization uses for a purpose, as callers see it (FDW-1581).
/// </summary>
/// <param name="OrganizationId">The organization.</param>
/// <param name="Purpose">What the connection is used for.</param>
/// <param name="GrantId">The grant the assignment uses.</param>
/// <param name="ConnectionName">The granted connection; <see langword="null"/> when the grant the assignment names no longer exists.</param>
/// <param name="FolderPath">The folder of the grant; <see langword="null"/> when the grant is the whole connection.</param>
/// <param name="BaseFolder">The folder inside the grant's the organization is narrowed to; <see langword="null"/> when it uses the grant as it is.</param>
public sealed record ConnectionAssignmentView(Guid OrganizationId, string Purpose, Guid GrantId, string? ConnectionName, string? FolderPath, string? BaseFolder);
