using Fdw.Collections;

namespace Fdw.Documents.Records.Abstractions.Capabilities;

/// <summary>A capability a DocumentRecord connection declares (FDW-3673), so a caller asks rather than guesses.</summary>
public interface IDocumentRecordCapability : ITypeOption<int, DocumentRecordCapabilityBase>
{
}
