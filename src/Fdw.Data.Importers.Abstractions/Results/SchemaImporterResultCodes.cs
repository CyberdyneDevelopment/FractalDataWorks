using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Data.SchemaImporters.Abstractions.Results;

/// <summary>
/// TypeCollection for Schema Importer result codes.
/// Codes use the categorized-number catalog scheme (Id == EventId == number, Code == "SCHEMA-{number}").
/// </summary>
[TypeCollection(typeof(SchemaImporterResultCodeBase), typeof(IResultCode), typeof(SchemaImporterResultCodes))]
public abstract partial class SchemaImporterResultCodes : TypeCollectionBase<SchemaImporterResultCodeBase, IResultCode>
{
}