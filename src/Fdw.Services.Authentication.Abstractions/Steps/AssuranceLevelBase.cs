using Fdw.Collections;

namespace Fdw.Services.Authentication.Abstractions.Steps;

/// <summary>Base for the assurance levels a deployment declares.</summary>
/// <remarks>
/// No option id is passed: the id derives from the option's fully qualified type name, as for every
/// type option. A level is found by the value it is known by, <see cref="AcrValue"/>, because that is
/// what a token carries and what a flow's <c>MinimumAcr</c> names.
/// </remarks>
public abstract class AssuranceLevelBase : TypeOptionBase<AssuranceLevelBase>, IAssuranceLevel
{
    /// <summary>Initializes a new instance of the <see cref="AssuranceLevelBase"/> class.</summary>
    /// <param name="name">The level's name.</param>
    /// <param name="acrValue">The value a token carries in its <c>acr</c> claim at this level.</param>
    /// <param name="factorKinds">How many distinct kinds of factor reach this level; one or more.</param>
    /// <param name="description">What the level means, as shown to the people configuring a flow.</param>
    protected AssuranceLevelBase(string name, string acrValue, int factorKinds, string description)
        : base(name, name, description, null)
    {
        AcrValue = acrValue;
        FactorKinds = factorKinds;
    }

    /// <inheritdoc />
    public string AcrValue { get; }

    /// <inheritdoc />
    public int FactorKinds { get; }
}
