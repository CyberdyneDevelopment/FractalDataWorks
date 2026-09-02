using Fdw.Collections.Attributes;

namespace Fdw.Services.Authentication.Abstractions.Context;

/// <summary>Whether this principal may be issued a token at all.</summary>
[TypeOption(typeof(ContextElements), "Decision", RestrictToCurrentCompilation = true)]
public sealed class DecisionElement : ContextElementBase
{
    /// <summary>Initializes a new instance of the <see cref="DecisionElement"/> class.</summary>
    public DecisionElement()
        : base(4, "Decision")
    {
    }

    /// <inheritdoc/>
    public override bool IsPresentOn(AuthenticationContext context) => context.Decision is not null;
}
