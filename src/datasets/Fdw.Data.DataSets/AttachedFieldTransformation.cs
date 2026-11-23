using System;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>One stored link of a field mapping's transformation chain (FDW-877), as a conversion to a Graph data set carries it (FDW-6973).</summary>
/// <param name="TransformationId">The transformation the link runs.</param>
/// <param name="Name">The transformation's name.</param>
/// <param name="Ordinal">The link's place in the chain.</param>
public sealed record AttachedFieldTransformation(Guid TransformationId, string Name, int Ordinal);
