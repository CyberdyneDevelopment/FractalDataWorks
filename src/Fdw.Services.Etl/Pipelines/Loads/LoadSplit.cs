using System.Collections.Generic;

namespace Fdw.Services.Etl.Pipelines.Loads;

/// <summary>The loads of a run, told apart by where they go (FDW-1672).</summary>
/// <param name="Database">The loads that run as commands in a unit of work.</param>
/// <param name="Http">The loads that send their rows to an HTTP endpoint.</param>
internal sealed record LoadSplit(List<LoadUnit> Database, List<HttpRecordLoad> Http);
