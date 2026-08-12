using System;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using Shouldly;
using Xunit;

namespace Fdw.Web.RestEndpoints.Tests.EndpointTypeOptions;

/// <summary>
/// Tests the registry FastEndpoints filters on.
/// </summary>
/// <remarks>
/// Worth covering because the failure it guards is silent: with DisableAutoDiscovery set, an
/// endpoint missing from here is not routed, and the host starts cleanly and answers 404. Nothing
/// at build time says so — 96 endpoints across sixteen packages were in exactly that state until
/// they were declared.
/// </remarks>
[Trait("Priority", "P0")]
[Trait("Category", "CoreFramework")]
public sealed class DeclaredEndpointsTests
{
    private sealed class FirstEndpoint;

    private sealed class SecondEndpoint;

    private sealed class UndeclaredEndpoint;

    /// <summary>A declared type is reported as declared.</summary>
    [Fact]
    public void DeclareMakesTypeDeclared()
    {
        DeclaredEndpoints.Declare(typeof(FirstEndpoint));

        DeclaredEndpoints.IsDeclared(typeof(FirstEndpoint)).ShouldBeTrue();
    }

    /// <summary>A type nobody declared is not.</summary>
    /// <remarks>
    /// The half that matters: IsDeclared is the FastEndpoints Filter, so anything it answers true
    /// for gets routed. A registry that said yes to everything would make SkipRegistration
    /// decorative.
    /// </remarks>
    [Fact]
    public void UndeclaredTypeIsNotDeclared()
    {
        DeclaredEndpoints.IsDeclared(typeof(UndeclaredEndpoint)).ShouldBeFalse();
    }

    /// <summary>Declaring twice does not double-count.</summary>
    /// <remarks>
    /// Options can be cycled more than once — a collection's Register may run per host — so
    /// declaring has to be idempotent or Count stops meaning anything.
    /// </remarks>
    [Fact]
    public void DeclaringTwiceCountsOnce()
    {
        DeclaredEndpoints.Declare(typeof(SecondEndpoint));
        var afterFirst = DeclaredEndpoints.Count;

        DeclaredEndpoints.Declare(typeof(SecondEndpoint));

        DeclaredEndpoints.Count.ShouldBe(afterFirst);
    }

    /// <summary>A declared type appears in the set.</summary>
    [Fact]
    public void TypesContainsDeclared()
    {
        DeclaredEndpoints.Declare(typeof(FirstEndpoint));

        DeclaredEndpoints.Types.ShouldContain(typeof(FirstEndpoint));
    }

    /// <summary>Declaring null throws rather than recording nothing.</summary>
    [Fact]
    public void DeclareNullThrows()
    {
        Should.Throw<ArgumentNullException>(() => DeclaredEndpoints.Declare(null!));
    }
}
