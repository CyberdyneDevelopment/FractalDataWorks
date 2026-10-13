using Fdw.Collections;
using Fdw.Data.Transformations.Configuration;
using Fdw.Results;
using Fdw.Services.Data.Clients.Models;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// What one kind of transformation does for the <c>/transformations</c> endpoints: build the configuration a create or a change
/// states, and describe a stored one as the payload a read returns.
/// </summary>
public abstract class TransformationKindBase : TypeOptionBase<TransformationKindBase>, ITransformationKind
{
    /// <summary>Initializes a new instance of the <see cref="TransformationKindBase"/> class.</summary>
    /// <param name="name">The implementation name a request states, such as Field or Built.</param>
    protected TransformationKindBase(string name) : base(name)
    {
    }

    /// <summary>Checks a create request and builds the configuration to store, refusing members that belong to another kind.</summary>
    /// <param name="request">The create request.</param>
    /// <param name="context">The settings and logger.</param>
    /// <returns>The configuration ready to save, or the refusal.</returns>
    public abstract IGenericResult<ITransformationImplementationConfiguration> Prepare(
        CreateTransformationRequest request, TransformationKindContext context);

    /// <summary>Checks a change and builds the configuration that replaces the stored one, keeping its Id.</summary>
    /// <param name="request">The change; a member it leaves out keeps the stored value.</param>
    /// <param name="existing">The stored transformation, which is of this kind.</param>
    /// <param name="context">The settings and logger.</param>
    /// <returns>The configuration ready to save, or the refusal.</returns>
    public abstract IGenericResult<ITransformationImplementationConfiguration> PrepareUpdate(
        UpdateTransformationRequest request, ITransformationImplementationConfiguration existing, TransformationKindContext context);

    /// <summary>Adds the members only this kind has to a payload, as a list shows them.</summary>
    /// <param name="transformation">The stored transformation.</param>
    /// <param name="payload">The payload that already carries the members every kind has.</param>
    public abstract void Describe(ITransformationImplementationConfiguration transformation, TransformationPayload payload);

    /// <summary>Adds everything this kind has to a payload, as the read of one transformation shows it.</summary>
    /// <param name="transformation">The stored transformation.</param>
    /// <param name="payload">The payload that already carries the members every kind has.</param>
    /// <param name="context">The settings and logger.</param>
    /// <returns>Success, or the refusal when a stored part cannot be read.</returns>
    public virtual IGenericResult Detail(
        ITransformationImplementationConfiguration transformation, TransformationPayload payload, TransformationKindContext context)
    {
        Describe(transformation, payload);
        return GenericResult.Success();
    }
}
