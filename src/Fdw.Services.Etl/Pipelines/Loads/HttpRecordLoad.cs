using Fdw.Data.Abstractions;
using Fdw.Services.Connections.Http.Abstractions;

namespace Fdw.Services.Etl.Pipelines.Loads;

/// <summary>A load whose destination is a container reached through an HTTP connection that writes records (FDW-1672).</summary>
/// <param name="Unit">The load.</param>
/// <param name="Writer">The connection that sends the rows.</param>
/// <param name="Container">The container the rows are sent for.</param>
/// <param name="Description">Names the destination in messages.</param>
internal sealed record HttpRecordLoad(LoadUnit Unit, IHttpRecordWriterConnection Writer, IDataContainer Container, string Description);
