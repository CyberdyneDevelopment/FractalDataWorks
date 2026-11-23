using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Results;
using Fdw.Services.LanguageModels.Abstractions;
using Fdw.Services.LanguageModels.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.LanguageModels;

/// <summary>
/// The one rule that decides, before any call, whether a model can carry a request: a model id is on its row, the request states a
/// usable output limit, the model carries the reasoning effort the request states, and the model carries every capability the request needs.
/// </summary>
/// <remarks>
/// It is stated once here and asked by <see cref="LanguageModelBase"/>, so no provider re-writes it and no provider has a side door around it.
/// What a model carries is data on its configuration row; nothing here is keyed by a model id.
/// </remarks>
public static class LanguageModelRequestRule
{
    private const string ChatCapability = "Chat";

    /// <summary>Checks a request against a model's configuration.</summary>
    /// <param name="request">The request.</param>
    /// <param name="configuration">The configuration row of the model.</param>
    /// <param name="logger">The logger that records the failure.</param>
    /// <returns>A success when the model can carry the request, otherwise a failure with one coded message.</returns>
    public static IGenericResult Check(LanguageModelRequest request, ILanguageModelImplementationConfiguration configuration, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(configuration.ModelId))
            return GenericResult.Failure(LanguageModelLog.ModelIdMissing(logger, configuration.Name));

        if (request.MaxOutputTokens < 1)
            return GenericResult.Failure(LanguageModelLog.MaxOutputTokensInvalid(logger, configuration.Name));

        if (!configuration.ReasoningEffortNames.Contains(request.ReasoningEffort.Name, StringComparer.OrdinalIgnoreCase))
            return GenericResult.Failure(LanguageModelLog.EffortNotCarried(logger, configuration.Name, request.ReasoningEffort.Name));

        var missing = MissingCapability(request, configuration);
        if (missing is not null)
            return GenericResult.Failure(LanguageModelLog.CapabilityNotCarried(logger, configuration.Name, missing));

        return GenericResult.Success(LanguageModelLog.RequestChecked(logger, configuration.Name));
    }

    private static string? MissingCapability(LanguageModelRequest request, ILanguageModelImplementationConfiguration configuration)
    {
        foreach (var required in RequiredCapabilities(request))
        {
            if (!configuration.CapabilityNames.Contains(required, StringComparer.OrdinalIgnoreCase))
                return required;
        }

        return null;
    }

    private static IEnumerable<string> RequiredCapabilities(LanguageModelRequest request)
    {
        yield return ChatCapability;

        foreach (var message in request.Messages)
        {
            foreach (var part in message.Parts)
                yield return part.Kind.RequiredCapability;
        }

        foreach (var tool in request.Tools)
        {
            if (tool.HostedKind is { } hostedKind)
                yield return hostedKind.RequiredCapability;
        }
    }
}
