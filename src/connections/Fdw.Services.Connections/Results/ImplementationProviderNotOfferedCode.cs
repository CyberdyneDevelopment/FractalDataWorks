using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Connections.Results;

/// <summary>A connection kind builds no implementation provider from a declaration (FDW-6360): a test double that reaches nothing, or a kind that needs services only the container holds.</summary>
[TypeOption(typeof(ConnectionsResultCodes), "ImplementationProviderNotOffered", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ImplementationProviderNotOfferedCode : ConnectionsResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="ImplementationProviderNotOfferedCode"/> class.</summary>
    public ImplementationProviderNotOfferedCode()
        : base(20018, "ImplementationProviderNotOffered",
            ResultSeverities.ByName("Error"),
            "Connection type '{connectionType}' builds no implementation provider from a declaration, so a connection of it cannot be opened before the container is built",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller why the request was refused: the message is written for them.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
