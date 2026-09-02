using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Credentials.Abstractions;
using Fdw.Services.Credentials.Abstractions.Outcomes;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Users.Abstractions;
using Fdw.Services.Users.Configuration;
using Fdw.Services.Users.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;
using Xunit;
using Fdw.Services.Data;

namespace Fdw.Services.Users.Tests;

/// <summary>
/// Unit tests for <see cref="UserCredentialService"/> — the credential edge that hashes on
/// arrival, delegates Match/NoMatch to the vault, and composes the caller-facing
/// <see cref="ICredentialOutcome"/> (lockout / expiry / must-change) from non-secret policy data.
/// </summary>
/// <remarks>
/// <see cref="ICredentialServiceProvider"/> and <see cref="ICredentialService"/> are mocked (the
/// vault boundary). <see cref="UserConfigurationProvider"/> is mocked via its virtual
/// <c>GetUser</c>/<c>Save</c> members (the comment on those members states this is the intended
/// test-isolation seam — no real <see cref="IConfigurationGateway"/> needed). The real
/// <c>PasswordHashAlgorithms</c> and <c>CredentialOutcomes</c> TypeCollections run for real (this
/// test project references <c>Fdw.Services.Credentials.Sql</c> so the concrete Match / NoMatch /
/// Expired / TooManyAttempts / MustChange options are registered), so outcome names are asserted
/// against the actual production registry rather than a mock.
/// </remarks>
public class UserCredentialServiceTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    private static readonly string ValidSalt = Convert.ToBase64String(new byte[16]);

    private static Mock<UserConfigurationProvider> MakeUserProviderMock()
        => new(
            NullLogger<UserConfigurationProvider>.Instance,
            GatewayProviderOn("PlatformConfiguration"),
            "PlatformConfiguration",
            "usr");

    private static IConfigurationGatewayProvider GatewayProviderOn(string connectionName)
    {
        var gateways = new ConfigurationGatewayProvider();
        gateways.Register(Mock.Of<IConfigurationGateway>(g => g.ConnectionName == connectionName));
        return gateways;
    }

    private static void SetupGetUser(Mock<UserConfigurationProvider> providerMock, Guid userId, UserConfiguration? cfg)
        => providerMock
            .Setup(p => p.GetUser(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<UserConfiguration?>.Success(cfg));

    private static void SetupGetUserFails(Mock<UserConfigurationProvider> providerMock, Guid userId, IGenericMessage message)
        => providerMock
            .Setup(p => p.GetUser(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<UserConfiguration?>.Failure(message));

    private static void SetupSaveSucceeds(Mock<UserConfigurationProvider> providerMock)
        => providerMock
            .Setup(p => p.Save(It.IsAny<UserConfiguration>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserConfiguration cfg, CancellationToken _) => GenericResult<UserConfiguration>.Success(cfg));

    private static void SetupSaveFails(Mock<UserConfigurationProvider> providerMock, IGenericMessage message)
        => providerMock
            .Setup(p => p.Save(It.IsAny<UserConfiguration>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<UserConfiguration>.Failure(message));

    private static UserConfiguration MakeUserConfig(
        Guid id,
        string? algorithmName = "Pbkdf2",
        string? salt = null,
        int failedLoginCount = 0,
        DateTimeOffset? lockoutEnd = null,
        DateTimeOffset? lastPasswordChangedAt = null,
        bool mustChangePasswordOnLogin = false)
        => new()
        {
            Id = id,
            Username = "alice",
            AlgorithmName = algorithmName,
            Salt = salt ?? ValidSalt,
            FailedLoginCount = failedLoginCount,
            LockoutEnd = lockoutEnd,
            LastPasswordChangedAt = lastPasswordChangedAt,
            MustChangePasswordOnLogin = mustChangePasswordOnLogin,
            IsActive = true,
            IsCurrent = true,
            IsDeleted = false,
        };

    private static ICredentialOutcome MakeVaultOutcome(bool grantsAccess)
    {
        var mock = new Mock<ICredentialOutcome>();
        mock.SetupGet(o => o.GrantsAccess).Returns(grantsAccess);
        return mock.Object;
    }

    private static Mock<ICredentialServiceProvider> MakeResolvingProvider(ICredentialService service)
    {
        var mock = new Mock<ICredentialServiceProvider>();
        mock.Setup(p => p.Get(It.IsAny<CredentialServiceRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ICredentialService>.Success(service));
        return mock;
    }

    private static UserCredentialService MakeService(
        ICredentialServiceProvider credentialServiceProvider,
        UserConfigurationProvider userProvider,
        UsersServiceConfiguration? policy = null,
        string? credentialServiceName = "Vault")
    {
        var configuration = policy ?? new UsersServiceConfiguration();
        configuration.CredentialServiceName = credentialServiceName;

        // The configuration record carries no algorithm default -- a host supplies one, so a test
        // that does not care which algorithm still has to name it.
        if (configuration.PasswordHashAlgorithm.Length == 0)
        {
            configuration.PasswordHashAlgorithm = "Pbkdf2";
        }

        var provider = new Mock<UsersServiceConfigurationProvider>(
            MockBehavior.Loose,
            null!,
            Mock.Of<IConfigurationGatewayProvider>(),
            "PlatformConfiguration",
            "usr");
        provider.Setup(x => x.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<UsersServiceConfiguration>.Success(configuration));

        return new(
            credentialServiceProvider,
            provider.Object,
            userProvider,
            NullLogger<UserCredentialService>.Instance);
    }

    // ── Verify — guard branches (secret type / policy validity) ────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task VerifyWithWrongSecretTypeReturnsFailureWithSecretTypeNotSupported()
    {
        var userId = Guid.NewGuid();
        var providerMock = new Mock<ICredentialServiceProvider>();
        var service = MakeService(providerMock.Object, MakeUserProviderMock().Object);

        var result = await service.Verify(userId, "ApiKey", "irrelevant", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldContain(m => m.Code == "USERS-61002");
        providerMock.Verify(p => p.Get(It.IsAny<CredentialServiceRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task VerifyWithLowercasePasswordSecretTypeIsAccepted()
    {
        var userId = Guid.NewGuid();
        var userCfg = MakeUserConfig(userId);
        var userProviderMock = MakeUserProviderMock();
        SetupGetUser(userProviderMock, userId, userCfg);

        var credentialServiceMock = new Mock<ICredentialService>();
        credentialServiceMock
            .Setup(s => s.Validate(userId, It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ICredentialOutcome>.Success(MakeVaultOutcome(grantsAccess: true)));
        var providerMock = MakeResolvingProvider(credentialServiceMock.Object);

        var service = MakeService(providerMock.Object, userProviderMock.Object);

        var result = await service.Verify(userId, "password", "secret", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Name.ShouldBe("Match");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task VerifyWithInvalidLockoutPolicyReturnsFailureWithPasswordPolicyInvalid()
    {
        var userId = Guid.NewGuid();
        var providerMock = new Mock<ICredentialServiceProvider>();
        var policy = new UsersServiceConfiguration { MaxFailedLoginAttempts = 5, LockoutDurationMinutes = 0 };
        var service = MakeService(providerMock.Object, MakeUserProviderMock().Object, policy);

        var result = await service.Verify(userId, "Password", "secret", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldContain(m => m.Code == "USERS-61003");
        providerMock.Verify(p => p.Get(It.IsAny<CredentialServiceRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Verify — credential service resolution ──────────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task VerifyWithMissingCredentialServiceNameReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var providerMock = new Mock<ICredentialServiceProvider>();
        var service = MakeService(providerMock.Object, MakeUserProviderMock().Object, credentialServiceName: null);

        var result = await service.Verify(userId, "Password", "secret", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldContain(m => m.Code == "USERS-61000");
        providerMock.Verify(p => p.Get(It.IsAny<CredentialServiceRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task VerifyWhenCredentialServiceProviderFailsReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var providerMock = new Mock<ICredentialServiceProvider>();
        providerMock
            .Setup(p => p.Get(It.IsAny<CredentialServiceRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ICredentialService>.Failure(new GenericMessage("vault unreachable")));
        var service = MakeService(providerMock.Object, MakeUserProviderMock().Object);

        var result = await service.Verify(userId, "Password", "secret", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldContain(m => m.Code == "USERS-61001");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task VerifyWhenCredentialServiceProviderReturnsNullValueReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var providerMock = new Mock<ICredentialServiceProvider>();
        providerMock
            .Setup(p => p.Get(It.IsAny<CredentialServiceRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ICredentialService>.Success(null!));
        var service = MakeService(providerMock.Object, MakeUserProviderMock().Object);

        var result = await service.Verify(userId, "Password", "secret", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldContain(m => m.Code == "USERS-61001");
    }

    // ── Verify — security-lookup provider FAILURE (fail loud, never decoy) ──────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task VerifyWhenUserSecurityLookupProviderFailsReturnsFailureWithoutRunningDecoy()
    {
        var userId = Guid.NewGuid();
        var userProviderMock = MakeUserProviderMock();
        SetupGetUserFails(userProviderMock, userId, new GenericMessage("security lookup blew up"));

        var credentialServiceMock = new Mock<ICredentialService>();
        var providerMock = MakeResolvingProvider(credentialServiceMock.Object);
        var service = MakeService(providerMock.Object, userProviderMock.Object);

        var result = await service.Verify(userId, "Password", "whatever", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldContain(m => m.Code == "USERS-71035");
        credentialServiceMock.Verify(
            s => s.Validate(It.IsAny<Guid>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Verify — anti-enumeration decoy path (§6) ───────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task VerifyWithUnknownUserRunsDecoyKdfAndReturnsNoMatch()
    {
        var userId = Guid.NewGuid();
        var userProviderMock = MakeUserProviderMock();
        SetupGetUser(userProviderMock, userId, null);

        var credentialServiceMock = new Mock<ICredentialService>();
        var providerMock = MakeResolvingProvider(credentialServiceMock.Object);
        var service = MakeService(providerMock.Object, userProviderMock.Object);

        var result = await service.Verify(userId, "Password", "whatever", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Name.ShouldBe("NoMatch");
        result.Value.GrantsAccess.ShouldBeFalse();
        credentialServiceMock.Verify(
            s => s.Validate(It.IsAny<Guid>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task VerifyWithMissingSaltRunsDecoyKdfAndReturnsNoMatch()
    {
        var userId = Guid.NewGuid();
        var userCfg = MakeUserConfig(userId, salt: string.Empty);
        var userProviderMock = MakeUserProviderMock();
        SetupGetUser(userProviderMock, userId, userCfg);

        var credentialServiceMock = new Mock<ICredentialService>();
        var providerMock = MakeResolvingProvider(credentialServiceMock.Object);
        var service = MakeService(providerMock.Object, userProviderMock.Object);

        var result = await service.Verify(userId, "Password", "whatever", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Name.ShouldBe("NoMatch");
        credentialServiceMock.Verify(
            s => s.Validate(It.IsAny<Guid>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task VerifyWithMissingAlgorithmNameRunsDecoyKdfAndReturnsNoMatch()
    {
        var userId = Guid.NewGuid();
        var userCfg = MakeUserConfig(userId, algorithmName: "   ");
        var userProviderMock = MakeUserProviderMock();
        SetupGetUser(userProviderMock, userId, userCfg);

        var credentialServiceMock = new Mock<ICredentialService>();
        var providerMock = MakeResolvingProvider(credentialServiceMock.Object);
        var service = MakeService(providerMock.Object, userProviderMock.Object);

        var result = await service.Verify(userId, "Password", "whatever", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Name.ShouldBe("NoMatch");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task VerifyDecoyWithUnregisteredPolicyAlgorithmStillReturnsNoMatchWithoutThrowing()
    {
        var userId = Guid.NewGuid();
        var userProviderMock = MakeUserProviderMock();
        SetupGetUser(userProviderMock, userId, null);

        var credentialServiceMock = new Mock<ICredentialService>();
        var providerMock = MakeResolvingProvider(credentialServiceMock.Object);
        var policy = new UsersServiceConfiguration { PasswordHashAlgorithm = "NotARealAlgorithm" };
        var service = MakeService(providerMock.Object, userProviderMock.Object, policy);

        var result = await service.Verify(userId, "Password", "whatever", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Name.ShouldBe("NoMatch");
    }

    // ── Verify — stored algorithm resolution ────────────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task VerifyWithUnknownStoredAlgorithmReturnsFailureWithVaultAlgorithmNotFound()
    {
        var userId = Guid.NewGuid();
        var userCfg = MakeUserConfig(userId, algorithmName: "RotNotAlgorithm");
        var userProviderMock = MakeUserProviderMock();
        SetupGetUser(userProviderMock, userId, userCfg);

        var credentialServiceMock = new Mock<ICredentialService>();
        var providerMock = MakeResolvingProvider(credentialServiceMock.Object);
        var service = MakeService(providerMock.Object, userProviderMock.Object);

        var result = await service.Verify(userId, "Password", "whatever", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldContain(m => m.Code == "USERS-31000");
        credentialServiceMock.Verify(
            s => s.Validate(It.IsAny<Guid>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Verify — vault compare passthrough ──────────────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task VerifyWhenVaultValidateFailsReturnsThatFailureUnchanged()
    {
        var userId = Guid.NewGuid();
        var userCfg = MakeUserConfig(userId);
        var userProviderMock = MakeUserProviderMock();
        SetupGetUser(userProviderMock, userId, userCfg);

        var vaultMessage = new GenericMessage("vault compare exploded");
        var credentialServiceMock = new Mock<ICredentialService>();
        credentialServiceMock
            .Setup(s => s.Validate(userId, It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ICredentialOutcome>.Failure(vaultMessage));
        var providerMock = MakeResolvingProvider(credentialServiceMock.Object);
        var service = MakeService(providerMock.Object, userProviderMock.Object);

        var result = await service.Verify(userId, "Password", "whatever", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldContain(m => ReferenceEquals(m, vaultMessage));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task VerifyWhenVaultReturnsSuccessWithNullOutcomeReturnsSameNullValueResult()
    {
        var userId = Guid.NewGuid();
        var userCfg = MakeUserConfig(userId);
        var userProviderMock = MakeUserProviderMock();
        SetupGetUser(userProviderMock, userId, userCfg);

        var credentialServiceMock = new Mock<ICredentialService>();
        credentialServiceMock
            .Setup(s => s.Validate(userId, It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ICredentialOutcome>.Success(null!));
        var providerMock = MakeResolvingProvider(credentialServiceMock.Object);
        var service = MakeService(providerMock.Object, userProviderMock.Object);

        var result = await service.Verify(userId, "Password", "whatever", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeNull();
    }

    // ── Verify — lockout window (§7) ─────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task VerifyWhenStillLockedDeniesWithoutResettingCounter()
    {
        var userId = Guid.NewGuid();
        var userCfg = MakeUserConfig(userId, failedLoginCount: 3, lockoutEnd: DateTimeOffset.UtcNow.AddMinutes(10));
        var userProviderMock = MakeUserProviderMock();
        SetupGetUser(userProviderMock, userId, userCfg);

        var credentialServiceMock = new Mock<ICredentialService>();
        credentialServiceMock
            .Setup(s => s.Validate(userId, It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ICredentialOutcome>.Success(MakeVaultOutcome(grantsAccess: true)));
        var providerMock = MakeResolvingProvider(credentialServiceMock.Object);
        var service = MakeService(providerMock.Object, userProviderMock.Object);

        var result = await service.Verify(userId, "Password", "correct-password", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Name.ShouldBe("TooManyAttempts");
        result.Value.GrantsAccess.ShouldBeFalse();
        userProviderMock.Verify(p => p.Save(It.IsAny<UserConfiguration>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task VerifyOnSuccessResetsLockoutCounterWhenPriorFailuresExist()
    {
        var userId = Guid.NewGuid();
        var userCfg = MakeUserConfig(userId, failedLoginCount: 2, lockoutEnd: null);
        var userProviderMock = MakeUserProviderMock();
        SetupGetUser(userProviderMock, userId, userCfg);
        SetupSaveSucceeds(userProviderMock);

        var credentialServiceMock = new Mock<ICredentialService>();
        credentialServiceMock
            .Setup(s => s.Validate(userId, It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ICredentialOutcome>.Success(MakeVaultOutcome(grantsAccess: true)));
        var providerMock = MakeResolvingProvider(credentialServiceMock.Object);
        var service = MakeService(providerMock.Object, userProviderMock.Object);

        var result = await service.Verify(userId, "Password", "correct-password", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Name.ShouldBe("Match");
        userCfg.FailedLoginCount.ShouldBe(0);
        userCfg.LockoutEnd.ShouldBeNull();
        userProviderMock.Verify(p => p.Save(It.IsAny<UserConfiguration>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task VerifyOnSuccessWithNoPriorFailuresDoesNotWriteLockoutState()
    {
        var userId = Guid.NewGuid();
        var userCfg = MakeUserConfig(userId, failedLoginCount: 0, lockoutEnd: null);
        var userProviderMock = MakeUserProviderMock();
        SetupGetUser(userProviderMock, userId, userCfg);

        var credentialServiceMock = new Mock<ICredentialService>();
        credentialServiceMock
            .Setup(s => s.Validate(userId, It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ICredentialOutcome>.Success(MakeVaultOutcome(grantsAccess: true)));
        var providerMock = MakeResolvingProvider(credentialServiceMock.Object);
        var service = MakeService(providerMock.Object, userProviderMock.Object);

        var result = await service.Verify(userId, "Password", "correct-password", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Name.ShouldBe("Match");
        userProviderMock.Verify(p => p.Save(It.IsAny<UserConfiguration>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Verify — expiry / must-change composition ───────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task VerifyWithExpiredPasswordReturnsExpiredOutcome()
    {
        var userId = Guid.NewGuid();
        var userCfg = MakeUserConfig(userId, lastPasswordChangedAt: DateTimeOffset.UtcNow.AddDays(-100));
        var userProviderMock = MakeUserProviderMock();
        SetupGetUser(userProviderMock, userId, userCfg);

        var credentialServiceMock = new Mock<ICredentialService>();
        credentialServiceMock
            .Setup(s => s.Validate(userId, It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ICredentialOutcome>.Success(MakeVaultOutcome(grantsAccess: true)));
        var providerMock = MakeResolvingProvider(credentialServiceMock.Object);
        var policy = new UsersServiceConfiguration { PasswordMaxAgeDays = 90 };
        var service = MakeService(providerMock.Object, userProviderMock.Object, policy);

        var result = await service.Verify(userId, "Password", "correct-password", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Name.ShouldBe("Expired");
        result.Value.GrantsAccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task VerifyWithUnexpiredPasswordReturnsMatchOutcome()
    {
        var userId = Guid.NewGuid();
        var userCfg = MakeUserConfig(userId, lastPasswordChangedAt: DateTimeOffset.UtcNow.AddDays(-10));
        var userProviderMock = MakeUserProviderMock();
        SetupGetUser(userProviderMock, userId, userCfg);

        var credentialServiceMock = new Mock<ICredentialService>();
        credentialServiceMock
            .Setup(s => s.Validate(userId, It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ICredentialOutcome>.Success(MakeVaultOutcome(grantsAccess: true)));
        var providerMock = MakeResolvingProvider(credentialServiceMock.Object);
        var policy = new UsersServiceConfiguration { PasswordMaxAgeDays = 90 };
        var service = MakeService(providerMock.Object, userProviderMock.Object, policy);

        var result = await service.Verify(userId, "Password", "correct-password", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Name.ShouldBe("Match");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task VerifyExpiryTakesPrecedenceOverMustChange()
    {
        var userId = Guid.NewGuid();
        var userCfg = MakeUserConfig(
            userId,
            lastPasswordChangedAt: DateTimeOffset.UtcNow.AddDays(-100),
            mustChangePasswordOnLogin: true);
        var userProviderMock = MakeUserProviderMock();
        SetupGetUser(userProviderMock, userId, userCfg);

        var credentialServiceMock = new Mock<ICredentialService>();
        credentialServiceMock
            .Setup(s => s.Validate(userId, It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ICredentialOutcome>.Success(MakeVaultOutcome(grantsAccess: true)));
        var providerMock = MakeResolvingProvider(credentialServiceMock.Object);
        var policy = new UsersServiceConfiguration { PasswordMaxAgeDays = 90 };
        var service = MakeService(providerMock.Object, userProviderMock.Object, policy);

        var result = await service.Verify(userId, "Password", "correct-password", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Name.ShouldBe("Expired");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task VerifyWithMustChangeFlagReturnsMustChangeOutcome()
    {
        var userId = Guid.NewGuid();
        var userCfg = MakeUserConfig(userId, mustChangePasswordOnLogin: true);
        var userProviderMock = MakeUserProviderMock();
        SetupGetUser(userProviderMock, userId, userCfg);

        var credentialServiceMock = new Mock<ICredentialService>();
        credentialServiceMock
            .Setup(s => s.Validate(userId, It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ICredentialOutcome>.Success(MakeVaultOutcome(grantsAccess: true)));
        var providerMock = MakeResolvingProvider(credentialServiceMock.Object);
        var service = MakeService(providerMock.Object, userProviderMock.Object);

        var result = await service.Verify(userId, "Password", "correct-password", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Name.ShouldBe("MustChange");
        result.Value.GrantsAccess.ShouldBeFalse();
    }

    // ── Verify — wrong password / lockout escalation ────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task VerifyOnWrongPasswordIncrementsFailedCountAndReturnsNoMatch()
    {
        var userId = Guid.NewGuid();
        var userCfg = MakeUserConfig(userId, failedLoginCount: 0, lockoutEnd: null);
        var userProviderMock = MakeUserProviderMock();
        SetupGetUser(userProviderMock, userId, userCfg);
        SetupSaveSucceeds(userProviderMock);

        var credentialServiceMock = new Mock<ICredentialService>();
        credentialServiceMock
            .Setup(s => s.Validate(userId, It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ICredentialOutcome>.Success(MakeVaultOutcome(grantsAccess: false)));
        var providerMock = MakeResolvingProvider(credentialServiceMock.Object);
        var policy = new UsersServiceConfiguration { MaxFailedLoginAttempts = 5, LockoutDurationMinutes = 15 };
        var service = MakeService(providerMock.Object, userProviderMock.Object, policy);

        var result = await service.Verify(userId, "Password", "wrong-password", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Name.ShouldBe("NoMatch");
        result.Value.GrantsAccess.ShouldBeFalse();
        userCfg.FailedLoginCount.ShouldBe(1);
        userCfg.LockoutEnd.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task VerifyOnWrongPasswordAtThresholdLocksAccountAndReturnsTooManyAttempts()
    {
        var userId = Guid.NewGuid();
        var userCfg = MakeUserConfig(userId, failedLoginCount: 4, lockoutEnd: null);
        var userProviderMock = MakeUserProviderMock();
        SetupGetUser(userProviderMock, userId, userCfg);
        SetupSaveSucceeds(userProviderMock);

        var credentialServiceMock = new Mock<ICredentialService>();
        credentialServiceMock
            .Setup(s => s.Validate(userId, It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ICredentialOutcome>.Success(MakeVaultOutcome(grantsAccess: false)));
        var providerMock = MakeResolvingProvider(credentialServiceMock.Object);
        var policy = new UsersServiceConfiguration { MaxFailedLoginAttempts = 5, LockoutDurationMinutes = 15 };
        var service = MakeService(providerMock.Object, userProviderMock.Object, policy);

        var before = DateTimeOffset.UtcNow;
        var result = await service.Verify(userId, "Password", "wrong-password", TestContext.Current.CancellationToken);
        var after = DateTimeOffset.UtcNow;

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Name.ShouldBe("TooManyAttempts");
        result.Value.GrantsAccess.ShouldBeFalse();
        userCfg.FailedLoginCount.ShouldBe(5);
        userCfg.LockoutEnd.ShouldNotBeNull();
        userCfg.LockoutEnd!.Value.ShouldBeInRange(before.AddMinutes(15), after.AddMinutes(15));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task VerifyOnWrongPasswordWithLockoutDisabledNeverLocks()
    {
        var userId = Guid.NewGuid();
        var userCfg = MakeUserConfig(userId, failedLoginCount: 999, lockoutEnd: null);
        var userProviderMock = MakeUserProviderMock();
        SetupGetUser(userProviderMock, userId, userCfg);
        SetupSaveSucceeds(userProviderMock);

        var credentialServiceMock = new Mock<ICredentialService>();
        credentialServiceMock
            .Setup(s => s.Validate(userId, It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ICredentialOutcome>.Success(MakeVaultOutcome(grantsAccess: false)));
        var providerMock = MakeResolvingProvider(credentialServiceMock.Object);
        var policy = new UsersServiceConfiguration { MaxFailedLoginAttempts = 0, LockoutDurationMinutes = 0 };
        var service = MakeService(providerMock.Object, userProviderMock.Object, policy);

        var result = await service.Verify(userId, "Password", "wrong-password", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Name.ShouldBe("NoMatch");
        userCfg.FailedLoginCount.ShouldBe(1000);
        userCfg.LockoutEnd.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task VerifyWhenLockoutCounterWriteFailsStillReturnsNoMatch()
    {
        var userId = Guid.NewGuid();
        var userCfg = MakeUserConfig(userId, failedLoginCount: 0, lockoutEnd: null);
        var userProviderMock = MakeUserProviderMock();
        SetupGetUser(userProviderMock, userId, userCfg);
        SetupSaveFails(userProviderMock, new GenericMessage("write conflict"));

        var credentialServiceMock = new Mock<ICredentialService>();
        credentialServiceMock
            .Setup(s => s.Validate(userId, It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ICredentialOutcome>.Success(MakeVaultOutcome(grantsAccess: false)));
        var providerMock = MakeResolvingProvider(credentialServiceMock.Object);
        var policy = new UsersServiceConfiguration { MaxFailedLoginAttempts = 5, LockoutDurationMinutes = 15 };
        var service = MakeService(providerMock.Object, userProviderMock.Object, policy);

        var result = await service.Verify(userId, "Password", "wrong-password", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Name.ShouldBe("NoMatch");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task VerifyWhenLockoutResetWriteFailsStillComposesMatchOutcome()
    {
        var userId = Guid.NewGuid();
        var userCfg = MakeUserConfig(userId, failedLoginCount: 3, lockoutEnd: null);
        var userProviderMock = MakeUserProviderMock();
        SetupGetUser(userProviderMock, userId, userCfg);
        SetupSaveFails(userProviderMock, new GenericMessage("write conflict"));

        var credentialServiceMock = new Mock<ICredentialService>();
        credentialServiceMock
            .Setup(s => s.Validate(userId, It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ICredentialOutcome>.Success(MakeVaultOutcome(grantsAccess: true)));
        var providerMock = MakeResolvingProvider(credentialServiceMock.Object);
        var service = MakeService(providerMock.Object, userProviderMock.Object);

        var result = await service.Verify(userId, "Password", "correct-password", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Name.ShouldBe("Match");
    }

    // ── Store ────────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task StoreWithWrongSecretTypeReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var providerMock = new Mock<ICredentialServiceProvider>();
        var service = MakeService(providerMock.Object, MakeUserProviderMock().Object);

        var result = await service.Store(userId, "ApiKey", "newpassword", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldContain(m => m.Code == "USERS-61002");
        providerMock.Verify(p => p.Get(It.IsAny<CredentialServiceRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task StoreWhenCredentialServiceResolutionFailsReturnsThatFailure()
    {
        var userId = Guid.NewGuid();
        var providerMock = new Mock<ICredentialServiceProvider>();
        providerMock
            .Setup(p => p.Get(It.IsAny<CredentialServiceRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ICredentialService>.Failure(new GenericMessage("vault unreachable")));
        var service = MakeService(providerMock.Object, MakeUserProviderMock().Object);

        var result = await service.Store(userId, "Password", "newpassword", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldContain(m => m.Code == "USERS-61001");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task StoreWithUnregisteredPolicyAlgorithmReturnsFailureWithVaultAlgorithmNotFound()
    {
        var userId = Guid.NewGuid();
        var credentialServiceMock = new Mock<ICredentialService>();
        var providerMock = MakeResolvingProvider(credentialServiceMock.Object);
        var policy = new UsersServiceConfiguration { PasswordHashAlgorithm = "NotARealAlgorithm" };
        var service = MakeService(providerMock.Object, MakeUserProviderMock().Object, policy);

        var result = await service.Store(userId, "Password", "newpassword", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldContain(m => m.Code == "USERS-31000");
        credentialServiceMock.Verify(
            s => s.Create(It.IsAny<Guid>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task StoreWhenVaultCreateFailsReturnsThatFailureUnchanged()
    {
        var userId = Guid.NewGuid();
        var vaultMessage = new GenericMessage("vault insert exploded");
        var credentialServiceMock = new Mock<ICredentialService>();
        credentialServiceMock
            .Setup(s => s.Create(userId, It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Failure(vaultMessage));
        var providerMock = MakeResolvingProvider(credentialServiceMock.Object);
        var service = MakeService(providerMock.Object, MakeUserProviderMock().Object);

        var result = await service.Store(userId, "Password", "newpassword", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldContain(m => ReferenceEquals(m, vaultMessage));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task StoreWhenPostCreateUserLookupFailsWithMessagesReturnsThatFailure()
    {
        var userId = Guid.NewGuid();
        var credentialServiceMock = new Mock<ICredentialService>();
        credentialServiceMock
            .Setup(s => s.Create(userId, It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());
        var providerMock = MakeResolvingProvider(credentialServiceMock.Object);

        var userProviderMock = MakeUserProviderMock();
        var lookupMessage = new GenericMessage("lookup blew up");
        SetupGetUserFails(userProviderMock, userId, lookupMessage);

        var service = MakeService(providerMock.Object, userProviderMock.Object);

        var result = await service.Store(userId, "Password", "newpassword", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldContain(m => ReferenceEquals(m, lookupMessage));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task StoreWhenPostCreateUserLookupReturnsNullWithoutMessagesReturnsFailureWithVaultStoreFailed()
    {
        var userId = Guid.NewGuid();
        var credentialServiceMock = new Mock<ICredentialService>();
        credentialServiceMock
            .Setup(s => s.Create(userId, It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());
        var providerMock = MakeResolvingProvider(credentialServiceMock.Object);

        var userProviderMock = MakeUserProviderMock();
        SetupGetUser(userProviderMock, userId, null);

        var service = MakeService(providerMock.Object, userProviderMock.Object);

        var result = await service.Store(userId, "Password", "newpassword", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldContain(m => m.Code == "USERS-71033");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task StoreWhenFinalSaveFailsReturnsThatFailureUnchanged()
    {
        var userId = Guid.NewGuid();
        var credentialServiceMock = new Mock<ICredentialService>();
        credentialServiceMock
            .Setup(s => s.Create(userId, It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());
        var providerMock = MakeResolvingProvider(credentialServiceMock.Object);

        var userProviderMock = MakeUserProviderMock();
        var userCfg = MakeUserConfig(userId, algorithmName: null, salt: null, mustChangePasswordOnLogin: true);
        SetupGetUser(userProviderMock, userId, userCfg);
        var saveMessage = new GenericMessage("save conflict");
        SetupSaveFails(userProviderMock, saveMessage);

        var service = MakeService(providerMock.Object, userProviderMock.Object);

        var result = await service.Store(userId, "Password", "newpassword", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldContain(m => ReferenceEquals(m, saveMessage));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task StoreOnSuccessPersistsSaltAlgorithmAndClearsMustChange()
    {
        var userId = Guid.NewGuid();
        var credentialServiceMock = new Mock<ICredentialService>();
        credentialServiceMock
            .Setup(s => s.Create(userId, It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());
        var providerMock = MakeResolvingProvider(credentialServiceMock.Object);

        var userProviderMock = MakeUserProviderMock();
        var userCfg = MakeUserConfig(userId, algorithmName: null, salt: null, mustChangePasswordOnLogin: true);
        SetupGetUser(userProviderMock, userId, userCfg);
        SetupSaveSucceeds(userProviderMock);

        var service = MakeService(providerMock.Object, userProviderMock.Object);

        var result = await service.Store(userId, "Password", "newpassword", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        userCfg.AlgorithmName.ShouldBe("Pbkdf2");
        userCfg.Salt.ShouldNotBeNullOrEmpty();
        userCfg.MustChangePasswordOnLogin.ShouldBeFalse();
        userCfg.LastPasswordChangedAt.ShouldNotBeNull();
        userProviderMock.Verify(p => p.Save(It.IsAny<UserConfiguration>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── ForcePasswordChange ──────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ForcePasswordChangeWhenUserLookupFailsWithMessagesReturnsThatFailure()
    {
        var userId = Guid.NewGuid();
        var userProviderMock = MakeUserProviderMock();
        var lookupMessage = new GenericMessage("lookup blew up");
        SetupGetUserFails(userProviderMock, userId, lookupMessage);
        var service = MakeService(new Mock<ICredentialServiceProvider>().Object, userProviderMock.Object);

        var result = await service.ForcePasswordChange(userId, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldContain(m => ReferenceEquals(m, lookupMessage));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ForcePasswordChangeWhenUserLookupReturnsNullWithoutMessagesReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var userProviderMock = MakeUserProviderMock();
        SetupGetUser(userProviderMock, userId, null);
        var service = MakeService(new Mock<ICredentialServiceProvider>().Object, userProviderMock.Object);

        var result = await service.ForcePasswordChange(userId, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldContain(m => m.Code == "USERS-71011");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ForcePasswordChangeWhenSaveFailsReturnsThatFailureUnchanged()
    {
        var userId = Guid.NewGuid();
        var userProviderMock = MakeUserProviderMock();
        var userCfg = MakeUserConfig(userId);
        SetupGetUser(userProviderMock, userId, userCfg);
        var saveMessage = new GenericMessage("save conflict");
        SetupSaveFails(userProviderMock, saveMessage);
        var service = MakeService(new Mock<ICredentialServiceProvider>().Object, userProviderMock.Object);

        var result = await service.ForcePasswordChange(userId, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldContain(m => ReferenceEquals(m, saveMessage));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ForcePasswordChangeOnSuccessSetsMustChangeFlag()
    {
        var userId = Guid.NewGuid();
        var userProviderMock = MakeUserProviderMock();
        var userCfg = MakeUserConfig(userId, mustChangePasswordOnLogin: false);
        SetupGetUser(userProviderMock, userId, userCfg);
        SetupSaveSucceeds(userProviderMock);
        var service = MakeService(new Mock<ICredentialServiceProvider>().Object, userProviderMock.Object);

        var result = await service.ForcePasswordChange(userId, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        userCfg.MustChangePasswordOnLogin.ShouldBeTrue();
        userProviderMock.Verify(p => p.Save(It.IsAny<UserConfiguration>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
