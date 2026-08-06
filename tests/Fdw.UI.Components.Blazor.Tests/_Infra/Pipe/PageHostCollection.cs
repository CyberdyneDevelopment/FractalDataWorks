namespace Fdw.UI.Components.Blazor.Tests.PipeInfra;

/// <summary>
/// Serial collection for the relocated FDW page-component tests. These swap a page's headless
/// provider for a <see cref="ProviderStub{TContext}"/> via a process-wide static seed store
/// (<c>ProviderStubState</c>); disabling parallelization keeps that static from being clobbered
/// when many page-host tests run together.
/// </summary>
[CollectionDefinition(PageHostCollection.Name, DisableParallelization = true)]
public sealed class PageHostCollection
{
    public const string Name = "FdwPageHost";
}
