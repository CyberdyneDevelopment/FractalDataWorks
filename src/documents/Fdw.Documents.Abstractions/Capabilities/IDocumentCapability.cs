using Fdw.Collections;

namespace Fdw.Documents.Abstractions.Capabilities;

/// <summary>Something a document connection can do that not every one can (FDW-1499): a caller asks rather than guesses.</summary>
public interface IDocumentCapability : ITypeOption<int, DocumentCapabilityBase>
{
}
