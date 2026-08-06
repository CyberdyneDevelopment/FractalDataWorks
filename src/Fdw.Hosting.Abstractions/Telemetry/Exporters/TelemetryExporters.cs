using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Hosting.Abstractions.Telemetry;

/// <summary>
/// Collection of telemetry exporter TypeOptions.
/// Provides type-safe access to OpenTelemetry exporter types.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(TelemetryExporterBase), typeof(ITelemetryExporter), typeof(TelemetryExporters))]
public sealed partial class TelemetryExporters : TypeCollectionBase<TelemetryExporterBase, ITelemetryExporter>
{
}
