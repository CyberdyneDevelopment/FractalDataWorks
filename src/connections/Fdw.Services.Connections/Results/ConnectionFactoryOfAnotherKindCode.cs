using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Connections.Results;

/// <summary>A connection kind was handed a factory that is not the kind it builds a provider for (FDW-6360).</summary>
[TypeOption(typeof(ConnectionsResultCodes), "ConnectionFactoryOfAnotherKind", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ConnectionFactoryOfAnotherKindCode : ConnectionsResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="ConnectionFactoryOfAnotherKindCode"/> class.</summary>
    public ConnectionFactoryOfAnotherKindCode()
        : base(20017, "ConnectionFactoryOfAnotherKind",
            ResultSeverities.ByName("Error"),
            "Connection type '{connectionType}' builds its provider from a factory of type '{expectedFactory}', not from '{givenFactory}'",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller why the request was refused: the message is written for them.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
