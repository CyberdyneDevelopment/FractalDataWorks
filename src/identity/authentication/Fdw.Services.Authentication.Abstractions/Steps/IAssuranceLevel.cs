using Fdw.Collections;

namespace Fdw.Services.Authentication.Abstractions.Steps;

/// <summary>
/// One rung of a deployment's assurance ladder: one level of the <see cref="AssuranceLevels"/> collection.
/// </summary>
/// <remarks>
/// FDW defines how a level is reached (how many distinct kinds of factor were proved) and nothing
/// about what the levels are called. The value a token carries in its <c>acr</c> claim, and a flow names
/// in its <c>MinimumAcr</c>, is the deployment's own, declared by the package that declares the level.
/// </remarks>
public interface IAssuranceLevel : ITypeOption<int, AssuranceLevelBase>
{
    /// <summary>Gets the value this level is known by: what a token carries in its <c>acr</c> claim.</summary>
    string AcrValue { get; }

    /// <summary>Gets how many distinct kinds of factor (knowledge, possession, inherence) reach this level.</summary>
    int FactorKinds { get; }

    /// <summary>Gets what the level means, as shown to the people configuring a flow.</summary>
    string Description { get; }
}
