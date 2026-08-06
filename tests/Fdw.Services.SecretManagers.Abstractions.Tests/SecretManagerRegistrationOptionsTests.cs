using Fdw.Services.SecretManagers.Abstractions;
using Fdw.ServiceTypes;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Fdw.Services.SecretManagers.Abstractions.Tests;

public class SecretManagerRegistrationOptionsTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ConstructorInitializesWithSingletonLifetime()
    {
        var options = new SecretManagerRegistrationOptions();

        options.Lifetime.ShouldBe(ServiceLifetime.Singleton);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void InheritsFromRegistrationOptions()
    {
        var options = new SecretManagerRegistrationOptions();

        options.ShouldBeAssignableTo<RegistrationOptions>();
    }
}
