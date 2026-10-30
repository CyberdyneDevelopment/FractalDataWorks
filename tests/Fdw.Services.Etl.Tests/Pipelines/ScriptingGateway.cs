using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.EtlGateway;

namespace Fdw.Services.Etl.Tests.Pipelines;

/// <summary>
/// An ETL gateway fake for script tests (FDW-1443): answers a script read with the rows it was given, answers a script run with
/// the rows affected it was given, and keeps every command it was asked to run, with the data store it was asked to run on.
/// </summary>
internal sealed class ScriptingGateway
{
    /// <summary>Initializes a new instance of the <see cref="ScriptingGateway"/> class.</summary>
    /// <param name="rows">The rows a read answers.</param>
    /// <param name="affected">The rows affected a run answers.</param>
    /// <param name="fails">Whether every read and run fails.</param>
    /// <param name="failure">The messages a failing read or run carries, in order; one uncoded message when it is not given.</param>
    public ScriptingGateway(IReadOnlyList<IDictionary<string, object?>> rows, int affected = 0, bool fails = false, IReadOnlyList<IGenericMessage>? failure = null)
    {
        IReadOnlyList<IGenericMessage> refusal = failure is null ? [new GenericMessage("script refused")] : failure;
        var gateway = new Mock<IEtlGateway>();
        gateway
            .Setup(g => g.Execute<IReadOnlyList<IDictionary<string, object?>>>(
                It.IsAny<IScriptedDataCommand<IReadOnlyList<IDictionary<string, object?>>>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns((IScriptedDataCommand<IReadOnlyList<IDictionary<string, object?>>> command, string store, CancellationToken token) =>
            {
                Commands.Add((store, command));
                return Task.FromResult<IGenericResult<IReadOnlyList<IDictionary<string, object?>>>>(
                    fails
                        ? GenericResult<IReadOnlyList<IDictionary<string, object?>>>.Failure(refusal)
                        : GenericResult<IReadOnlyList<IDictionary<string, object?>>>.Success(rows));
            });
        gateway
            .Setup(g => g.Execute<int>(It.IsAny<IScriptedDataCommand<int>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns((IScriptedDataCommand<int> command, string store, CancellationToken token) =>
            {
                Commands.Add((store, command));
                return Task.FromResult<IGenericResult<int>>(
                    fails ? GenericResult<int>.Failure(refusal) : GenericResult<int>.Success(affected));
            });
        Gateway = gateway.Object;
    }

    /// <summary>Gets the ETL gateway.</summary>
    public IEtlGateway Gateway { get; }

    /// <summary>Gets every command asked of the gateway, in order, with the data store it named.</summary>
    public List<(string Store, IScriptedDataCommand Command)> Commands { get; } = [];
}
