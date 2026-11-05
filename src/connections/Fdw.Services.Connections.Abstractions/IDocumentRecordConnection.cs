using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Documents.Records.Abstractions.Capabilities;
using Fdw.Documents.Records.Abstractions.Commands;
using Fdw.Results;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// A connection that keeps document records (FDW-3673): a sibling of <see cref="IDataConnection"/> and <see cref="IDocumentConnection"/>.
/// It carries out the document record commands itself, each in its own storage's terms; the tables, if it has any, are its own.
/// </summary>
/// <remarks>
/// One connection class may be several kinds. An organization reaches one through its assignment for the purpose <c>DocumentRecords</c>
/// (<c>IDocumentRecordStores.For</c>), never by name from above the connection layer.
/// </remarks>
public interface IDocumentRecordConnection : IGenericConnection
{
    /// <summary>Gets what the connection can do; the record store needs <c>AtomicRevisionWrites</c>.</summary>
    IReadOnlyCollection<IDocumentRecordCapability> RecordCapabilities { get; }

    /// <summary>Carries out a document record command.</summary>
    /// <typeparam name="T">What the command answers with.</typeparam>
    /// <param name="command">The command; its <see cref="IDocumentRecordCommand.FirstProblem"/> has been asked and found nothing.</param>
    /// <param name="cancellationToken">Cancels the command.</param>
    /// <returns>The command's answer, or the reason it was refused.</returns>
    Task<IGenericResult<T>> Execute<T>(IDocumentRecordCommand<T> command, CancellationToken cancellationToken = default);
}
