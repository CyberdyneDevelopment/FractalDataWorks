using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Roslyn.Commands.Analysis.Severities;

/// <summary>TypeCollection of the lowest severity a diagnostics request reports, carrying the Roslyn <see cref="Microsoft.CodeAnalysis.DiagnosticSeverity"/> it stands for.</summary>
[TypeCollection(typeof(DiagnosticSeverityLevelBase), typeof(IDiagnosticSeverityLevel), typeof(DiagnosticSeverityLevels))]
[ExcludeFromCodeCoverage]
public abstract partial class DiagnosticSeverityLevels : TypeCollectionBase<DiagnosticSeverityLevelBase, IDiagnosticSeverityLevel> { }
