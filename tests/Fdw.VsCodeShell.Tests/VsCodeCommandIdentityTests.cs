using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.ServiceTypes;
using Fdw.VsCodeShell;
using Fdw.VsCodeShell.Abstractions;

namespace Fdw.VsCodeShell.Tests;

/// <summary>
/// Guards option identity for <see cref="VsCodeCommandTypes"/>.
/// </summary>
/// <remarks>
/// This is the regression suite for a failure that is otherwise SILENT: the generated
/// <c>RegisterMember</c> discards an option whose Id is already present — no throw, no log, the command
/// simply never exists. The compile-time guard (ST001) only sees options in the collection's own
/// compilation, and every real command is declared downstream, so nothing catches it at build time either.
/// <para>
/// What collides changed. <c>ServiceTypeBase.Id</c> was <c>MD5($"{TService.FullName}:{TFactory.FullName}")</c>
/// — computed from the GENERIC ARGUMENTS — so two commands closing the same handler type were one id and the
/// second was dropped. It is now <c>DeriveId(name)</c>, hashing the option's own name, so that collision is
/// gone and the surviving hazard is two options sharing a NAME. These tests assert both halves.
/// </para>
/// </remarks>
public class VsCodeCommandIdentityTests
{
    private sealed class AlphaHandler : IVsCodeCommandHandler
    {
        public Task<IGenericResult<object?>> Invoke(EditorContext context, CancellationToken cancellationToken = default)
            => Task.FromResult<IGenericResult<object?>>(GenericResult<object?>.Success(null));
    }

    private sealed class BetaHandler : IVsCodeCommandHandler
    {
        public Task<IGenericResult<object?>> Invoke(EditorContext context, CancellationToken cancellationToken = default)
            => Task.FromResult<IGenericResult<object?>>(GenericResult<object?>.Success(null));
    }

    private sealed class AlphaCommand : VsCodeCommandTypeBase<AlphaHandler>
    {
        public AlphaCommand() : base("Alpha", "test.alpha", "Alpha") { }
    }

    private sealed class BetaCommand : VsCodeCommandTypeBase<BetaHandler>
    {
        public BetaCommand() : base("Beta", "test.beta", "Beta") { }
    }

    /// <summary>A second option reusing another's handler type — no longer a collision.</summary>
    private sealed class DuplicateOfAlphaCommand : VsCodeCommandTypeBase<AlphaHandler>
    {
        public DuplicateOfAlphaCommand() : base("DuplicateOfAlpha", "test.duplicate", "Duplicate") { }
    }

    /// <summary>A second option reusing another's NAME — the shape that gets dropped now.</summary>
    private sealed class SameNameAsAlphaCommand : VsCodeCommandTypeBase<BetaHandler>
    {
        public SameNameAsAlphaCommand() : base("Alpha", "test.samename", "Same name as Alpha") { }
    }

    private static System.Guid IdOf(IServiceType option) => option.Id;

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void CommandsClosingDistinctHandlersHaveDistinctIds()
    {
        IdOf(new AlphaCommand()).ShouldNotBe(IdOf(new BetaCommand()));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void CommandsReusingAHandlerTypeHaveDistinctIds()
    {
        IdOf(new DuplicateOfAlphaCommand()).ShouldNotBe(IdOf(new AlphaCommand()));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void CommandsSharingANameStillGetDistinctIds()
    {
        IdOf(new SameNameAsAlphaCommand()).ShouldNotBe(IdOf(new AlphaCommand()));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void CommandIdIsSeparateFromOptionName()
    {
        var command = new AlphaCommand();

        command.Name.ShouldBe("Alpha");
        command.CommandId.ShouldBe("test.alpha");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void HandlerTypeExposesTheClosedHandler()
    {
        new AlphaCommand().HandlerType.ShouldBe(typeof(AlphaHandler));
    }
}
