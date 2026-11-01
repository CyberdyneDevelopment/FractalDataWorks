using System;
using System.Collections.Generic;

namespace Fdw.Services.Multitenancy.Abstractions;

/// <summary>
/// A document as the retention job sees it: its id, its type and the dates it has (FDW-1575).
/// </summary>
/// <param name="DocumentId">The identifier the application gives the document.</param>
/// <param name="DocumentType">The document's type, as named in a retention policy.</param>
/// <param name="Dates">The dates the document has, by field name (for example a signed date); only the dates the document actually has.</param>
public sealed record RetainedDocument(string DocumentId, string DocumentType, IReadOnlyDictionary<string, DateTimeOffset> Dates);
