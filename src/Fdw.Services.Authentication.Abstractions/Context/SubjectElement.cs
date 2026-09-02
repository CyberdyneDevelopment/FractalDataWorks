using Fdw.Collections.Attributes;

namespace Fdw.Services.Authentication.Abstractions.Context;

/// <summary>Someone proved who they are.</summary>
[TypeOption(typeof(ContextElements), "Subject", RestrictToCurrentCompilation = true)]
public sealed class SubjectElement : ContextElementBase
{
    /// <summary>Initializes a new instance of the <see cref="SubjectElement"/> class.</summary>
    public SubjectElement()
        : base(1, "Subject")
    {
    }

    /// <inheritdoc/>
    public override bool IsPresentOn(AuthenticationContext context) => context.Subject is not null;
}
