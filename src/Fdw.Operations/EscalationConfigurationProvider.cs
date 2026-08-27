using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Operations.Commands;
using Fdw.Operations.Configuration;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Fdw.Operations;

/// <summary>Configuration provider for escalation policy configurations.</summary>
public class EscalationConfigurationProvider : ImplementationConfigurationProviderBase<EscalationPolicyConfiguration, EscalationPolicyConfigurationCommand>
{
    /// <summary>Initializes a new instance of the <see cref="EscalationConfigurationProvider"/> class.</summary>
    public EscalationConfigurationProvider(
        ILogger<EscalationConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName = "ConfigurationDb",
        string pathName = "workflow")
        : base(logger ?? NullLogger<EscalationConfigurationProvider>.Instance,
               gatewayProvider,
               dataStoreName, pathName)
    {
    }

    // Why: Get(string)/Get(Guid) no longer override to assemble the Policy→Levels→Recipients tree.
    // ImplementationConfigurationProviderBase.Get composes that nested 1:N hierarchy uniformly via ComposeChildren
    // (the read mirror of the save cascade), driven by the EscalationPolicy container's inbound-FK
    // metadata — EscalationLevel (FK EscalationPolicyRowId) then EscalationLevelRecipient (FK
    // EscalationLevelRowId). The old hand-rolled AssembleHierarchy is deleted.

    /// <summary>Gets all escalation level configurations.</summary>
    public async Task<IReadOnlyList<EscalationLevelConfiguration>> GetAllLevels(CancellationToken cancellationToken = default)
    {
        var command = new QueryCommandBuilder<EscalationLevelConfiguration>(
                DataStoreName, PathName, "EscalationLevel")
            .Where("IsCurrent", true)
            .Where("IsDeleted", false)
            .OrderBy("Level")
            .WithCaching()
            .Build();

        var result = await Execute<IEnumerable<EscalationLevelConfiguration>>(
            command, cancellationToken).ConfigureAwait(false);
        return result.IsSuccess ? result.Value?.ToList() ?? [] : [];
    }

    /// <summary>Gets all escalation level recipient configurations.</summary>
    public async Task<IReadOnlyList<EscalationLevelRecipientConfiguration>> GetAllRecipients(CancellationToken cancellationToken = default)
    {
        var command = new QueryCommandBuilder<EscalationLevelRecipientConfiguration>(
                DataStoreName, PathName, "EscalationLevelRecipient")
            .Where("IsCurrent", true)
            .Where("IsDeleted", false)
            .WithCaching()
            .Build();

        var result = await Execute<IEnumerable<EscalationLevelRecipientConfiguration>>(
            command, cancellationToken).ConfigureAwait(false);
        return result.IsSuccess ? result.Value?.ToList() ?? [] : [];
    }

    /// <summary>Gets an escalation level by ID.</summary>
    public async Task<EscalationLevelConfiguration?> GetLevel(Guid id, CancellationToken cancellationToken = default)
    {
        var command = new QueryCommandBuilder<EscalationLevelConfiguration>(
                DataStoreName, PathName, "EscalationLevel")
            .Where("Id", id)
            .Where("IsCurrent", true)
            .Where("IsDeleted", false)
            .Build();

        var result = await Execute<IEnumerable<EscalationLevelConfiguration>>(
            command, cancellationToken).ConfigureAwait(false);
        return result.IsSuccess ? result.Value?.FirstOrDefault() : null;
    }

    /// <summary>Gets an escalation level by name.</summary>
    public async Task<EscalationLevelConfiguration?> GetLevel(string name, CancellationToken cancellationToken = default)
    {
        var command = new QueryCommandBuilder<EscalationLevelConfiguration>(
                DataStoreName, PathName, "EscalationLevel")
            .Where("Name", name)
            .Where("IsCurrent", true)
            .Where("IsDeleted", false)
            .Build();

        var result = await Execute<IEnumerable<EscalationLevelConfiguration>>(
            command, cancellationToken).ConfigureAwait(false);
        return result.IsSuccess ? result.Value?.FirstOrDefault() : null;
    }

    /// <summary>Gets an escalation level recipient by ID.</summary>
    public async Task<EscalationLevelRecipientConfiguration?> GetRecipient(Guid id, CancellationToken cancellationToken = default)
    {
        var command = new QueryCommandBuilder<EscalationLevelRecipientConfiguration>(
                DataStoreName, PathName, "EscalationLevelRecipient")
            .Where("Id", id)
            .Where("IsCurrent", true)
            .Where("IsDeleted", false)
            .Build();

        var result = await Execute<IEnumerable<EscalationLevelRecipientConfiguration>>(
            command, cancellationToken).ConfigureAwait(false);
        return result.IsSuccess ? result.Value?.FirstOrDefault() : null;
    }

    /// <summary>Gets an escalation level recipient by name.</summary>
    public async Task<EscalationLevelRecipientConfiguration?> GetRecipient(string name, CancellationToken cancellationToken = default)
    {
        var command = new QueryCommandBuilder<EscalationLevelRecipientConfiguration>(
                DataStoreName, PathName, "EscalationLevelRecipient")
            .Where("Name", name)
            .Where("IsCurrent", true)
            .Where("IsDeleted", false)
            .Build();

        var result = await Execute<IEnumerable<EscalationLevelRecipientConfiguration>>(
            command, cancellationToken).ConfigureAwait(false);
        return result.IsSuccess ? result.Value?.FirstOrDefault() : null;
    }
}
