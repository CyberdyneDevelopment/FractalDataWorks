using Fdw.Collections.Attributes;

namespace Fdw.Services.Authentication.Abstractions.Context;

/// <summary>That someone was resolved to a local principal.</summary>
[TypeOption(typeof(ContextElements), "Principal", RestrictToCurrentCompilation = true)]
public sealed class PrincipalElement : ContextElementBase
{
    /// <summary>Initializes a new instance of the <see cref="PrincipalElement"/> class.</summary>
    public PrincipalElement()
        : base(2, "Principal")
    {
    }

    /// <inheritdoc/>
    public override bool IsPresentOn(AuthenticationContext context) => context.Principal is not null;
}
