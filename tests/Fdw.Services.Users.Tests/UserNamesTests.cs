using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Users.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Users.Tests;

/// <summary>
/// What a person is called is read from their own account (FDW-2199): the name of the account, never an id in its place, and a failure when there is none.
/// </summary>
public sealed class UserNamesTests
{
    private static readonly Guid Person = Guid.Parse("0c000000-0000-0000-0000-00000000000c");

    private static UserNames Names(IGenericResult<IUserImplementationConfiguration> answer)
    {
        var users = new Mock<IUserConfigurationProvider>();
        users.Setup(u => u.Get(Person, It.IsAny<CancellationToken>())).ReturnsAsync(answer);
        return new UserNames(users.Object, NullLogger<UserNames>.Instance);
    }

    private static IGenericResult<IUserImplementationConfiguration> Account(string? name)
    {
        var account = new Mock<IUserImplementationConfiguration>();
        account.SetupGet(a => a.Name).Returns(name!);
        return GenericResult<IUserImplementationConfiguration>.Success(account.Object);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TheNameIsTheNameOfTheAccount()
    {
        var result = await Names(Account("admin")).Get(Person, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("admin");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task APersonWithNoAccountIsAFailureAndNotTheirId()
    {
        var result = await Names(GenericResult<IUserImplementationConfiguration>.Success(default!)).Get(Person, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == "USERS-51100");
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AnAccountWithNoNameIsAFailureAndNotTheirId(string? blank)
    {
        var result = await Names(Account(blank)).Get(Person, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == "USERS-51101");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AnAccountThatCannotBeReadIsReportedAsItsOwnFailure()
    {
        var failed = GenericResult<IUserImplementationConfiguration>.Failure(new GenericMessage("the store is down"));

        var result = await Names(failed).Get(Person, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Message.Contains("the store is down", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ALookupWithoutAProviderIsRefused()
    {
        Should.Throw<ArgumentNullException>(() => new UserNames(null!, NullLogger<UserNames>.Instance));
    }
}
