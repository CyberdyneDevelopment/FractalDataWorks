using Fdw.Collections.Attributes;

namespace Fdw.Services.Authentication.Abstractions.Context;

/// <summary>Facts about the principal, each carrying where it came from.</summary>
[TypeOption(typeof(ContextElements), "Claims", RestrictToCurrentCompilation = true)]
public sealed class ClaimsElement : ContextElementBase
{
    /// <summary>Initializes a new instance of the <see cref="ClaimsElement"/> class.</summary>
    public ClaimsElement()
        : base(3, "Claims")
    {
    }

    /// <inheritdoc/>
    public override bool IsPresentOn(AuthenticationContext context) => context.Claims.Claims.Length > 0;
}
