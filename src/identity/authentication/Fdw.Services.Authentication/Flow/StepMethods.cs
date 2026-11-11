using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Steps;

namespace Fdw.Services.Authentication.Flow;

/// <summary>
/// The methods a step can prove when sign-in admission or flow administration asks, before any login has run (FDW-4697).
/// </summary>
public static class StepMethods
{
    /// <summary>Returns the methods the step declares plus, for a step that reads its authority, the ones that authority permits.</summary>
    /// <param name="step">The step.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The distinct methods the step can prove, or the step's failure when its authority cannot be read.</returns>
    public static async Task<IGenericResult<IReadOnlyList<string>>> Of(IAuthenticationStep step, CancellationToken cancellationToken = default)
    {
        if (step is not IAuthorityAssertingAuthenticationStep asserting)
            return GenericResult<IReadOnlyList<string>>.Success(step.AuthenticationMethods);

        var provable = await asserting.ProvableMethods(cancellationToken).ConfigureAwait(false);
        if (provable.IsFailure || provable.Value is null)
            return provable;

        return GenericResult<IReadOnlyList<string>>.Success([.. step.AuthenticationMethods.Union(provable.Value, StringComparer.Ordinal)]);
    }
}
