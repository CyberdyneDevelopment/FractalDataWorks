using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Roslyn.Commands.Abstractions;
using Microsoft.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Tests.TestDoubles;

/// <summary>
/// A concrete, DI-constructible <see cref="IRoslynCommandTranslator"/> used to exercise
/// <see cref="ServiceCollectionExtensions.AddTranslator{TTranslator}"/>.
/// </summary>
public sealed class FakeRoslynCommandTranslator : IRoslynCommandTranslator
{
    public int Id => 1;

    object ITypeOption.Id => Id;

    public string Name => "FakeTranslator";

    public string Category => "Fake";

    public Type CommandType => typeof(FakeRoslynCommand);

    public Task<IGenericResult<IRoslynCommandResult>> Execute(
        IRoslynCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(GenericResult<IRoslynCommandResult>.Success(new FakeCommandResult()));
    }
}
