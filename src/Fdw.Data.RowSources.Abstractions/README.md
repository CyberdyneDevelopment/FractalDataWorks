# Fdw.Data.RowSources.Abstractions

The row-source contracts: `IRecordSource`, `IRecordWriter` and the projection surface between a record and a typed row.

Implementations live in the sibling packages; this one holds the interfaces, base types and models they agree on, so a consumer can depend on the contract without taking the implementation.

## Interfaces (17)

| Type | Kind | Purpose |
|---|---|---|
| `IAsyncRowSourceReader` | interface | Asynchronous row source reader for streaming scenarios. Use for network streams, large files, and… |
| `IHttpRowSourceResultCode` | interface | Interface for HTTP RowSource result codes. |
| `IRecordCursor` | interface | The positioned-reader primitive over a single source: a cursor that exposes the field values of the… |
| `IRecordSource<T>` | interface | The parent abstraction over any source that yields RECORDS (items): a JSON/XML document yields items, a… |
| `IRecordSourceType` | interface | TypeOption interface for record source types (DataReader, Xml, Json, Delimited, FixedWidth, Http). The… |
| `IRecordWriter<T>` | interface | The parent abstraction over any target that accepts RECORDS (items) and serializes them to a format: the… |
| `IRecordWriterType` | interface | TypeOption interface for record writer types (Json, Xml, Delimited, FixedWidth). The write-side mirror… |
| `IRestPaginationStyle` | interface | Interface for REST pagination styles. |
| `IRowEnumerator` | interface | Streaming enumerator that yields rows with per-row Result pattern support. Enables processing millions… |
| `IRowMapper` | interface | Source-agnostic row mapper that converts data to dictionaries. Works with any record cursor (DataReader,… |
| `IRowMapperFactory` | interface | Factory interface for creating row mappers. |
| `IRowMapperFactory<in TConfiguration>` | interface | Factory interface for creating row mappers with configuration. |
| `IRowMapperProvider` | interface | Provider for row mapper factories, enabling lookup by type name. |
| `IRowMapperType` | interface | TypeOption interface for row mapper types (Pooled, Dynamic). |
| `IRowSource` | interface | The specialization of that additionally exposes the positional cursor () driving its record enumeration… |
| `IRowSourceReader` | interface | Synchronous reader that extends the primitive with forward navigation. Use for data sources that support… |

## Base types (9)

| Type | Kind | Purpose |
|---|---|---|
| `HttpRowEnumeratorBase` | class | Base class for HTTP-based row enumerators that stream paginated responses. |
| `HttpRowSourceResultCodeBase` | class | Base class for HTTP RowSource result codes. |
| `HttpRowSourceResultCodes` | class | TypeCollection for HTTP RowSource result codes. Codes use categorized numbers (Id == EventId == number,… |
| `RecordSourceTypeBase` | class | Base class for record source types using the CRTP pattern. The factory that builds a reader from a… |
| `RecordWriterTypeBase` | class | Base class for record writer types using the CRTP pattern. The write-side mirror of . (Renamed from… |
| `RestPaginationStyleBase` | class | Base class for REST pagination styles. |
| `RestPaginationStyles` | class | TypeCollection for REST pagination styles. |
| `RowMapperTypeBase` | class | Base class for row mapper types using CRTP pattern. |
| `RowMappingContextBase` | class | Base class for pre-computed mapping context that caches field ordinals and converters. Derived classes… |

## Models and supporting types (31)

| Type | Kind | Purpose |
|---|---|---|
| `CursorRecordSource` | class | The shared record-source adapter that turns a low-level cursor into an of : it advances the cursor and… |
| `DataReaderRowSource` | class | Adapts IDataReader to the IRowSourceReader interface. Provides zero-allocation row access for ADO.NET… |
| `DataRecord` | struct | The default record type for the no-DTO case: a record whose "type" IS the configured field set. It pairs… |
| `DelimitedRowSourceOptions` | class | Options for delimited (variable-length) row reading. Every knob maps 1:1 to a… |
| `DelimitedRowWriterOptions` | class | Options for delimited (variable-length) row writing. The write-side mirror of . The column ordering… |
| `GraphQlStreamingOptions` | class | Options for GraphQL cursor-based pagination. |
| `HttpRequestFailedCode` | class | HTTP request failed with non-success status code. |
| `HttpRowEnumeratorOptions` | class | Options for HTTP row enumeration with pagination support. |
| `JsonRowSourceOptions` | class | Options for JSON row source processing. |
| `JsonRowWriterOptions` | class | Options for JSON row writing. The write-side mirror of ; every knob maps 1:1 to a / setting. |
| `JsonStreamRowSource` | class | Streaming JSON row source that reads array elements without loading entire document. |
| `JsonStreamRowWriter` | class | Writes flat name→value rows as a JSON array of objects using . The write-side mirror of . |

## Installation

```bash
dotnet add package Fdw.Data.RowSources.Abstractions --prerelease
```

## Dependencies

`Fdw.Abstractions` · `Fdw.Collections` · `Fdw.Data.Abstractions` · `Fdw.MessageLogging.Abstractions` · `Fdw.Results` · `Fdw.Results.Abstractions`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
