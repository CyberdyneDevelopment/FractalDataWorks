namespace Fdw.Documents.Records.Abstractions.Commands;

/// <summary>
/// A document record command that writes (FDW-3673). It names the organization's revision the caller read; the connection makes the write
/// and the raise of the revision one atomic step, or refuses it with <c>DocumentRevisionConflict</c> when the revision moved on.
/// </summary>
public interface IRevisedDocumentRecordCommand : IDocumentRecordCommand
{
    /// <summary>Gets the organization's revision the caller read.</summary>
    long ExpectedRevision { get; }
}
