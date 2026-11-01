using System;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// A connection granted to a tenant for a purpose, as callers see it (FDW-1581).
/// </summary>
/// <param name="Id">The grant's durable id, which an assignment names.</param>
/// <param name="TenantId">The tenant it was granted to.</param>
/// <param name="ConnectionName">The granted connection.</param>
/// <param name="Purpose">What it was granted for (for example Documents).</param>
/// <param name="FolderPath">The folder the grant is narrowed to; <see langword="null"/> when it grants the connection whole.</param>
/// <param name="MayRegisterOwn">Whether the tenant may register its own connections for the purpose.</param>
public sealed record ConnectionGrantView(Guid Id, Guid TenantId, string ConnectionName, string Purpose, string? FolderPath, bool MayRegisterOwn);
