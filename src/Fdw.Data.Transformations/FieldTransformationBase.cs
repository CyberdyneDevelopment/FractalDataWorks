using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions;

namespace Fdw.Data.Transformations;

/// <summary>
/// Base class for field-level transform TypeOptions in the <see cref="TransformationTypes"/> collection.
/// Extends <see cref="TransformationTypeBase"/> with parameter definitions, single-value execution,
/// and optional batch execution for transforms that don't reference other fields.
/// </summary>
public abstract class FieldTransformationBase : TransformationTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FieldTransformationBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this field transformer type.</param>
    /// <param name="name">The name of this field transformer type.</param>
    /// <param name="displayName">The display name for this field transformer type.</param>
    /// <param name="description">A description of what this field transformer does.</param>
    /// <param name="category">The category (String, DateTime, Numeric, Injection, Conditional, Boolean).</param>
    /// <param name="supportsBatching">Whether this transform can operate on columns rather than rows.</param>
    /// <param name="parameters">The parameter definitions this transform accepts.</param>
    protected FieldTransformationBase(
        int id,
        string name,
        string displayName,
        string description,
        string category,
        bool supportsBatching,
        params OperationParameterDefinition[] parameters)
        : base(id, name, displayName, description, supportsStreaming: supportsBatching, category)
    {
        ExpectedParameters = parameters;
        SupportsBatching = supportsBatching;
    }

    /// <summary>
    /// Gets the parameter definitions that this field transformer expects.
    /// Parameter values are stored in transform.FieldMappingTransformParameter and reach the
    /// transform through <see cref="TransformationContext.Parameters"/>.
    /// </summary>
    public IReadOnlyList<OperationParameterDefinition> ExpectedParameters { get; }

    /// <summary>
    /// Gets a value indicating whether this transform can operate on an entire column at once.
    /// Transforms that reference <see cref="TransformationContext.CurrentRecord"/> must return false.
    /// </summary>
    public bool SupportsBatching { get; }


    /// <summary>
    /// Transforms one field value using the configuration and runtime state carried by the context.
    /// </summary>
    /// <remarks>
    /// Why this is the only entry point: there used to be two - this one, and a parameterless
    /// <c>Transform(value, cancellationToken)</c> that existed so a caller in a project which does
    /// not reference this one could still invoke a transform through a lowest-common-denominator
    /// interface. That shim had nowhere to carry parameters or the current record, so it invented an
    /// empty parameter bag and an empty context on every call, and any transform reached through it
    /// ran with none of its configuration: a two-label BoolToString returned the empty string for
    /// every row rather than either label. Callers that built the context properly got correct
    /// results, so the same transform behaved differently depending on which path invoked it. The
    /// shim is gone and the caller that needed it now references this project, so there is one
    /// calling convention and no way to express "run this unconfigured".
    /// </remarks>
    /// <param name="input">The field value to transform (may be null).</param>
    /// <param name="context">
    /// Configured parameters plus runtime state - the current record, operating date and execution
    /// timestamp.
    /// </param>
    /// <param name="cancellationToken">Token to observe for cancellation.</param>
    /// <returns>The transformed value wrapped in a result.</returns>
    public abstract Task<IGenericResult<object?>> Transform(
        object? input,
        TransformationContext context,
        CancellationToken cancellationToken = default);
}
