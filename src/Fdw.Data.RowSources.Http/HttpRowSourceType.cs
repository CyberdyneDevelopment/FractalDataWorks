using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Fdw.Collections.Attributes;
using Fdw.Data.RowSources.Abstractions;

// ReSharper disable once RedundantUsingDirective
using Fdw.Data.RowSources;

namespace Fdw.Data.RowSources.Http;

/// <summary>
/// TypeOption for HTTP streaming row sources.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "TypeOption - no logic to test")]
[TypeOption(typeof(RecordSourceTypes), "Http")]
public sealed class HttpRowSourceType : RecordSourceTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HttpRowSourceType"/> class.
    /// </summary>
    public HttpRowSourceType() : base(5, "Http")
    {
    }

    /// <inheritdoc />
    public override bool SupportsSync => false;

    /// <inheritdoc />
    public override bool SupportsAsync => true;

    /// <inheritdoc />
    public override bool SupportsReset => false;

    /// <inheritdoc />
    public override int TypicalAllocationsPerRow => 1;

    /// <inheritdoc />
    public override string Format => "Json";

    /// <inheritdoc />
    // Why: HTTP row sources wrap a paginated response ENUMERATOR (multi-request), not a single
    // content stream. The per-page payload is parsed by its own content format's row source
    // (Json/Xml), so this type does not itself create a reader from a bare stream. Fail loud
    // rather than guess (NO FALLBACKS).
    public override IRowSourceReader CreateReader(Stream content, RowSourceOptions? options)
        => throw new NotSupportedException(
            "HTTP row sources wrap a paginated response enumerator and cannot be created from a single content stream.");

    /// <inheritdoc />
    // Why: same reason as CreateReader — HTTP wraps a paginated, multi-request response enumerator, not
    // a single content stream; the per-page payload is parsed by its own content format's record source
    // (Json/Xml). Fail loud (NO FALLBACKS).
    public override IRecordSource<DataRecord> Create(RecordSourceContext context)
        => throw new NotSupportedException(
            "HTTP row sources wrap a paginated response enumerator and cannot be created from a single content stream.");
}
