using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Documents.Records.Abstractions.Links;

/// <summary>
/// Answers whether the entity a link points at is there (FDW-3397). The application implements it for every target kind it links to,
/// <see cref="LinkTargetKinds.Document"/> and <see cref="LinkTargetKinds.Record"/> among them, so a new kind needs no new type in FDW.
/// </summary>
public interface ILinkTargets
{
    /// <summary>Names the target kinds the application can check for an organization.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The target kinds.</returns>
    Task<IGenericResult<IReadOnlyList<string>>> Kinds(Guid organizationId, CancellationToken cancellationToken = default);

    /// <summary>Says whether a target is there for an organization.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="targetKind">The target kind, one of those <see cref="Kinds"/> names.</param>
    /// <param name="targetId">The target's public id.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>True when the target is there, false when it is not, or a failure when the application could not tell.</returns>
    Task<IGenericResult<bool>> Exists(Guid organizationId, string targetKind, Guid targetId, CancellationToken cancellationToken = default);
}
