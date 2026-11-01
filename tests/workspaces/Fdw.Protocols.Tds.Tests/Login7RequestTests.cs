using Fdw.Protocols.Tds;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Protocols.Tds.Tests;

/// <summary>
/// Reading a client's LOGIN7: every field at its offset, the password de-obfuscated, and the flags that
/// decide whether the login is one this server accepts.
/// </summary>
public sealed class Login7RequestTests
{
    [Fact]
    public void EveryFieldIsReadFromItsOffset()
    {
        var login = Login7Request.Parse(new Login7Payload().Build(), NullLogger.Instance);

        login.IsSuccess.ShouldBeTrue();
        login.Value!.TdsVersion.ShouldBe(TdsVersions.Tds74);
        login.Value.PacketSize.ShouldBe(8000);
        login.Value.HostName.ShouldBe("WORKSTATION");
        login.Value.UserName.ShouldBe("vendor");
        login.Value.ApplicationName.ShouldBe("Microsoft SQL Server Management Studio");
        login.Value.ServerName.ShouldBe("fdwsql");
        login.Value.ClientInterfaceName.ShouldBe("Core Microsoft SqlClient Data Provider");
        login.Value.Database.ShouldBe("Warehouse");
        login.Value.UsesIntegratedSecurity.ShouldBeFalse();
        login.Value.ChangesPassword.ShouldBeFalse();
        login.Value.HasFeatureExtension.ShouldBeFalse();
    }

    [Theory]
    [InlineData("S3cret!")]
    [InlineData("pässwörd with spaces")]
    [InlineData("")]
    public void ThePasswordIsDeobfuscated(string password)
    {
        var login = Login7Request.Parse(new Login7Payload { Password = password }.Build(), NullLogger.Instance);

        login.Value!.Password.ShouldBe(password);
    }

    [Fact]
    public void ThePasswordIsNeverInTheLoginsText()
    {
        var login = Login7Request.Parse(new Login7Payload { Password = "hunter2-secret" }.Build(), NullLogger.Instance);

        login.Value!.ToString().ShouldNotContain("hunter2-secret");
    }

    [Fact]
    public void IntegratedSecurityAndAPasswordChangeAreSeen()
    {
        var login = Login7Request.Parse(new Login7Payload { IntegratedSecurity = true, ChangePassword = "new-one" }.Build(), NullLogger.Instance);

        login.Value!.UsesIntegratedSecurity.ShouldBeTrue();
        login.Value.ChangesPassword.ShouldBeTrue();
    }

    [Fact]
    public void RequestedFeaturesAreRead()
    {
        var login = Login7Request.Parse(
            new Login7Payload { Features = [Login7Request.FederatedAuthenticationFeature, 0x0A] }.Build(), NullLogger.Instance);

        login.Value!.HasFeatureExtension.ShouldBeTrue();
        login.Value.Requests(Login7Request.FederatedAuthenticationFeature).ShouldBeTrue();
        login.Value.Requests(0x0A).ShouldBeTrue();
        login.Value.Requests(0x04).ShouldBeFalse();
    }

    [Fact]
    public void APayloadShorterThanTheFixedPartIsRefused()
    {
        Login7Request.Parse(new byte[40], NullLogger.Instance).IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void AFieldThatRunsPastTheEndIsRefused()
    {
        var payload = new Login7Payload().Build();
        payload[42] = 0xFF;
        payload[43] = 0x7F;

        Login7Request.Parse(payload, NullLogger.Instance).IsFailure.ShouldBeTrue();
    }
}
