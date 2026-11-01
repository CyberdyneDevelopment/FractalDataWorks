using System.Collections.Generic;

namespace Fdw.Operations.Endpoints;

/// <summary>What the field-consumer scan found (FDW-2157): who reads each field, and the graph pipelines whose use of the data set could not be followed.</summary>
/// <param name="Consumers">The consumers of each field, keyed by field name (case-insensitive).</param>
/// <param name="PipelinesNotFollowed">The graph pipelines that read the data set and whose fields reached no destination with an origin in it.</param>
internal sealed record FieldConsumerScan(Dictionary<string, List<FieldConsumerResponse>> Consumers, IReadOnlyList<string> PipelinesNotFollowed);
