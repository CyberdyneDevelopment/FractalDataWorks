using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Scheduling.Abstractions;
using Fdw.Services.Scheduling.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Scheduling;

/// <summary>
/// Abstract base class for scheduling command translators. Mirrors
/// <c>DataCommandTranslatorBase&lt;TCommand&gt;</c> for connections: one command per operation, shared
/// by every implementation, and only the translator varies.
/// </summary>
/// <typeparam name="TNative">The native type this translator produces.</typeparam>
public abstract class SchedulingCommandTranslatorBase<TNative> : ISchedulingCommandTranslator<TNative>
{
    /// <summary>Initializes a new instance of the <see cref="SchedulingCommandTranslatorBase{TNative}"/> class.</summary>
    /// <param name="name">Name of the translator (must match the command kind, e.g. "Create", "Pause").</param>
    /// <param name="domainName">The implementation this translator targets (e.g. "Quartz", "Hangfire").</param>
    protected SchedulingCommandTranslatorBase(string name, string domainName)
    {
        if (string.IsNullOrEmpty(name))
        {
            SchedulingLog.TranslatorNameMissing(NullLogger<SchedulingCommandTranslatorBase<TNative>>.Instance);
            throw new ArgumentNullException(nameof(name));
        }

        Id = OptionId.DeriveInt(GetType().FullName!);
        Name = name;
        DomainName = domainName;

        SchedulingLog.TranslatorInitializing(
            NullLogger<SchedulingCommandTranslatorBase<TNative>>.Instance, name, domainName);
    }

    /// <inheritdoc />
    public int Id { get; }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public string Category => DomainName;

    /// <inheritdoc />
    public string DomainName { get; }

    /// <inheritdoc />
    public abstract Task<IGenericResult<TNative>> Translate(
        ISchedulingCommand command,
        CancellationToken cancellationToken = default);
}
