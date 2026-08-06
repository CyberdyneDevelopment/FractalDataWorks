using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Components;

/// <summary>TypeCollection for validation message severity levels.</summary>
[TypeCollection(typeof(ValidationSeverityBase), typeof(IValidationSeverity), typeof(ValidationSeverities))]
[ExcludeFromCodeCoverage]
public abstract partial class ValidationSeverities : TypeCollectionBase<ValidationSeverityBase, IValidationSeverity> { }
