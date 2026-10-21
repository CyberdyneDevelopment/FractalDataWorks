using System;
using System.Collections.Generic;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Transforms;

namespace Fdw.Services.Etl.Graphs;

/// <summary>What the stages a graph holds name, resolved before any row moves (FDW-1656).</summary>
/// <param name="Built">The Built transformations and sets its Compute tools run.</param>
/// <param name="Chains">The chains of named transformations its field mappings run, by mapping id.</param>
internal sealed record ResolvedStages(BuiltCompilation Built, IReadOnlyDictionary<Guid, IReadOnlyList<IFieldMappingTransformation>> Chains);
