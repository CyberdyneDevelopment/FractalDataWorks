using System;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Users.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Users.Tests;

/// <summary>
/// Tests for <see cref="UnavailableDefaultWorkspaceFounding"/> (FDW-4178): a host without the workspaces service starts and a founding fails
/// with a message, and the workspace group's real registration replaces the refusing one whichever of the two registers first.
/// </summary>
public sealed class UnavailableDefaultWorkspaceFoundingTests
{
    private static readonly NewDefaultWorkspace Workspace = new(
        new Guid("66666666-0000-0000-0000-000000000001"), new Guid("11111111-0000-0000-0000-000000000001"), "beta",
        new Guid("44444444-0000-0000-0000-000000000001"), "Main", "main", true, new Guid("22222222-0000-0000-0000-000000000001"));

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Users")]
    public async Task FoundingFailsWithAMessageInsteadOfThrowing()
    {
        var result = await new UnavailableDefaultWorkspaceFounding().Found(Workspace);

        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Users")]
    public async Task RemovingFailsWithAMessageInsteadOfThrowing()
    {
        var result = await new UnavailableDefaultWorkspaceFounding().Remove(Workspace);

        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Users")]
    public void TheRefusingFoundingResolvesWhenNothingElseRegistersOne()
    {
        var services = new ServiceCollection();
        services.TryAddScoped<IDefaultWorkspaceFounding, UnavailableDefaultWorkspaceFounding>();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IDefaultWorkspaceFounding>().ShouldBeOfType<UnavailableDefaultWorkspaceFounding>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Users")]
    public void TheRealFoundingWinsWhenTheUsersTypeRegistersFirst()
    {
        var real = new Mock<IDefaultWorkspaceFounding>().Object;
        var services = new ServiceCollection();
        services.TryAddScoped<IDefaultWorkspaceFounding, UnavailableDefaultWorkspaceFounding>();
        services.AddScoped<IDefaultWorkspaceFounding>(_ => real);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IDefaultWorkspaceFounding>().ShouldBeSameAs(real);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Users")]
    public void TheRealFoundingWinsWhenTheWorkspaceGroupRegistersFirst()
    {
        var real = new Mock<IDefaultWorkspaceFounding>().Object;
        var services = new ServiceCollection();
        services.AddScoped<IDefaultWorkspaceFounding>(_ => real);
        services.TryAddScoped<IDefaultWorkspaceFounding, UnavailableDefaultWorkspaceFounding>();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IDefaultWorkspaceFounding>().ShouldBeSameAs(real);
    }
}
