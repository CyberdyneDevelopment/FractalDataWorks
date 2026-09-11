using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Credentials.Abstractions;
using Fdw.Services.Credentials.Abstractions.Outcomes;
using Fdw.Services.Users.Configuration;
using Fdw.Services.Users.Services;
using Fdw.Services.Users.Tests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;
using UserStore = Fdw.Services.Users.Tests.TestSupport.ConfigurationStore<
    Fdw.Services.Users.UserConfigurationProvider,
    Fdw.Services.Users.Configuration.IUserImplementationConfiguration>;

namespace Fdw.Services.Users.Tests;

/// <summary>
/// Unit tests for <see cref="UserCredentialService"/> — the credential edge that hashes on
/// arrival, delegates Match/NoMatch to the vault, and composes the caller-facing
/// <see cref="ICredentialOutcome"/> (lockout / expiry / must-change) from non-secret policy data.
/// </summary>
/// <remarks>
/// <see cref="ICredentialServiceProvider"/> and <see cref="ICredentialService"/> are mocked (the
/// vault boundary). <see cref="UserConfigurationProvider"/> and
/// <see cref="UsersServiceConfigurationProvider"/> are NOT: both are sealed with non-virtual reads,
/// so nothing can stand in for one. They run for real over a faked configuration store, and a test
/// states what that store holds. The real
/// <c>PasswordHashAlgorithms</c> and <c>CredentialOutcomes</c> TypeCollections run for real (this
/// test project references <c>Fdw.Services.Credentials.Sql</c> so the concrete Match / NoMatch /
/// Expired / TooManyAttempts / MustChange options are registered), so outcome names are asserted
/// against the actual production registry rather than a mock.
/// </remarks>
public class UserCredentialServiceTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    // The users domain's own configuration row, read by name off the domain's own store.
    private const string ConfigurationName = "UsersService";

    private static readonly string ValidSalt = Convert.ToBase64String(new byte[16]);

    private static UserStore MakeUserStore() => ConfigurationStore.Users();

    // A by-id read is Get(id): the domain row is found and dispatched to its implementation, and a
    // miss is a SUCCESS carrying a null value rather than a failure. The store states the miss by
    // holding no row for THIS user rather than by being told what to return — and it still holds
    // another user, because "no such user" in an otherwise-empty store is not the case the
    // anti-enumeration decoy defends against.
    private static void SetupGetUser(UserStore store, Guid userId, UserImplementationConfiguration? cfg)
    {
        if (cfg is null)
        {
            // A different user entirely — a different name as well as a different id, so nothing can
            // resolve it in this user's place.
            var stranger = MakeUserConfig(Guid.NewGuid());
            stranger.Username = "stranger";
            store.Holds(stranger);
            return;
        }

        // The row the provider resolves by id IS this record, so the two ids are the same fact.
        cfg.Id = userId;
        store.Holds(cfg);
    }

    // The store itself cannot be read — every user in it is unreachable, not just this one.
    private static void SetupUserStoreUnreadable(UserStore store, IGenericMessage message)
        => store.CannotBeRead(message);

    // Save lands on the implementation row: the domain provider resolves the row from the name it is
    // given and hands the record to the implementation provider registered for it.
    private static void SetupSaveFails(UserStore store, IGenericMessage message) => store.SaveFails(message);

    private static UserImplementationConfiguration MakeUserConfig(
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
        UsersServiceImplementationConfiguration? policy = null,
        string? credentialServiceName = "Vault")
    {
        var configuration = policy ?? new UsersServiceImplementationConfiguration();
        configuration.CredentialServiceName = credentialServiceName;

        // The configuration record carries no algorithm default -- a host supplies one, so a test
        // that does not care which algorithm still has to name it.
        if (configuration.PasswordHashAlgorithm.Length == 0)
        {
            configuration.PasswordHashAlgorithm = "Pbkdf2";
        }

        // The policy is the users domain's own configuration row, read by name like any other. The
        // record carries no id of its own, and a row is identified by the id of the record it names.
        configuration.Name = ConfigurationName;
        configuration.Id = Guid.CreateVersion7();

        return new(
            credentialServiceProvider,
            ConfigurationStore.UsersService(configuration).Provider,
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
        var service = MakeService(providerMock.Object, MakeUserStore().Provider);

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
        var userStore = MakeUserStore();
        SetupGetUser(userStore, userId, userCfg);

        var credentialServiceMock = new Mock<ICredentialService>();
        credentialServiceMock
            .Setup(s => s.Validate(userId, It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ICredentialOutcome>.Success(MakeVaultOutcome(grantsAccess: true)));
        var providerMock = MakeResolvingProvider(credentialServiceMock.Object);

        var service = MakeService(providerMock.Object, userStore.Provider);

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
        var policy = new UsersServiceImplementationConfiguration { MaxFailedLoginAttempts = 5, LockoutDurationMinutes = 0 };
        var service = MakeService(providerMock.Object, MakeUserStore().Provider, policy);

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
        var service = MakeService(providerMock.Object, MakeUserStore().Provider, credentialServiceName: null);

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
        var service = MakeService(providerMock.Object, MakeUserStore().Provider);

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
        var service = MakeService(providerMock.Object, MakeUserStore().Provider);

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
        var userStore = MakeUserStore();
        SetupUserStoreUnreadable(userStore, new GenericMessage("security lookup blew up"));

        var credentialServiceMock = new Mock<ICredentialService>();
        var providerMock = MakeResolvingProvider(credentialServiceMock.Object);
        var service = MakeService(providerMock.Object, userStore.Provider);

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
        var userStore = MakeUserStore();
        SetupGetUser(userStore, userId, null);

        var credentialServiceMock = new Mock<ICredentialService>();
        var providerMock = MakeResolvingProvider(credentialServiceMock.Object);
        var service = MakeService(providerMock.Object, userStore.Provider);

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
        var userStore = MakeUserStore();
        SetupGetUser(userStore, userId, userCfg);

        var credentialServiceMock = new Mock<ICredentialService>();
        var providerMock = MakeResolvingProvider(credentialServiceMock.Object);
        var service = MakeService(providerMock.Object, userStore.Provider);

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
        var userStore = MakeUserStore();
        SetupGetUser(userStore, userId, userCfg);

        var credentialServiceMock = new Mock<ICredentialService>();
        var providerMock = MakeResolvingProvider(credentialServiceMock.Object);
        var service = MakeService(providerMock.Object, userStore.Provider);

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
        var userStore = MakeUserStore();
        SetupGetUser(userStore, userId, null);

        var credentialServiceMock = new Mock<ICredentialService>();
        var providerMock = MakeResolvingProvider(credentialServiceMock.Object);
        var policy = new UsersServiceImplementationConfiguration { PasswordHashAlgorithm = "NotARealAlgorithm" };
        var service = MakeService(providerMock.Object, userStore.Provider, policy);

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
        var userStore = MakeUserStore();
        SetupGetUser(userStore, userId, userCfg);

        var credentialServiceMock = new Mock<ICredentialService>();
        var providerMock = MakeResolvingProvider(credentialServiceMock.Object);
        var service = MakeService(providerMock.Object, userStore.Provider);

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
        var userStore = MakeUserStore();
        SetupGetUser(userStore, userId, userCfg);

        var vaultMessage = new GenericMessage("vault compare exploded");
        var credentialServiceMock = new Mock<ICredentialService>();
        credentialServiceMock
            .Setup(s => s.Validate(userId, It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ICredentialOutcome>.Failure(vaultMessage));
        var providerMock = MakeResolvingProvider(credentialServiceMock.Object);
        var service = MakeService(providerMock.Object, userStore.Provider);

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
        var userStore = MakeUserStore();
        SetupGetUser(userStore, userId, userCfg);

        var credentialServiceMock = new Mock<ICredentialService>();
        credentialServiceMock
            .Setup(s => s.Validate(userId, It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ICredentialOutcome>.Success(null!));
        var providerMock = MakeResolvingProvider(credentialServiceMock.Object);
        var service = MakeService(providerMock.Object, userStore.Provider);

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
        var userStore = MakeUserStore();
        SetupGetUser(userStore, userId, userCfg);

        var credentialServiceMock = new Mock<ICredentialService>();
        credentialServiceMock
            .Setup(s => s.Validate(userId, It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ICredentialOutcome>.Success(MakeVaultOutcome(grantsAccess: true)));
        var providerMock = MakeResolvingProvider(credentialServiceMock.Object);
        var service = MakeService(providerMock.Object, userStore.Provider);

        var result = await service.Verify(userId, "Password", "correct-password", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Name.ShouldBe("TooManyAttempts");
        result.Value.GrantsAccess.ShouldBeFalse();
        userStore.Implementations.Verify(
            p => p.Save(It.IsAny<IUserImplementationConfiguration>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task VerifyOnSuccessResetsLockoutCounterWhenPriorFailuresExist()
    {
        var userId = Guid.NewGuid();
        var userCfg = MakeUserConfig(userId, failedLoginCount: 2, lockoutEnd: null);
        var userStore = MakeUserStore();
        SetupGetUser(userStore, userId, userCfg);

        var credentialServiceMock = new Mock<ICredentialService>();
        credentialServiceMock
            .Setup(s => s.Validate(userId, It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ICredentialOutcome>.Success(MakeVaultOutcome(grantsAccess: true)));
        var providerMock = MakeResolvingProvider(credentialServiceMock.Object);
        var service = MakeService(providerMock.Object, userStore.Provider);

        var result = await service.Verify(userId, "Password", "correct-password", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Name.ShouldBe("Match");
        userCfg.FailedLoginCount.ShouldBe(0);
        userCfg.LockoutEnd.ShouldBeNull();
        userStore.Implementations.Verify(
            p => p.Save(It.IsAny<IUserImplementationConfiguration>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task VerifyOnSuccessWithNoPriorFailuresDoesNotWriteLockoutState()
    {
        var userId = Guid.NewGuid();
        var userCfg = MakeUserConfig(userId, failedLoginCount: 0, lockoutEnd: null);
        var userStore = MakeUserStore();
        SetupGetUser(userStore, userId, userCfg);

        var credentialServiceMock = new Mock<ICredentialService>();
        credentialServiceMock
            .Setup(s => s.Validate(userId, It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ICredentialOutcome>.Success(MakeVaultOutcome(grantsAccess: true)));
        var providerMock = MakeResolvingProvider(credentialServiceMock.Object);
        var service = MakeService(providerMock.Object, userStore.Provider);

        var result = await service.Verify(userId, "Password", "correct-password", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Name.ShouldBe("Match");
        userStore.Implementations.Verify(
            p => p.Save(It.IsAny<IUserImplementationConfiguration>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── Verify — expiry / must-change composition ───────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task VerifyWithExpiredPasswordReturnsExpiredOutcome()
    {
        var userId = Guid.NewGuid();
        var userCfg = MakeUserConfig(userId, lastPasswordChangedAt: DateTimeOffset.UtcNow.AddDays(-100));
        var userStore = MakeUserStore();
        SetupGetUser(userStore, userId, userCfg);

        var credentialServiceMock = new Mock<ICredentialService>();
        credentialServiceMock
            .Setup(s => s.Validate(userId, It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ICredentialOutcome>.Success(MakeVaultOutcome(grantsAccess: true)));
        var providerMock = MakeResolvingProvider(credentialServiceMock.Object);
        var policy = new UsersServiceImplementationConfiguration { PasswordMaxAgeDays = 90 };
        var service = MakeService(providerMock.Object, userStore.Provider, policy);

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
        var userStore = MakeUserStore();
        SetupGetUser(userStore, userId, userCfg);

        var credentialServiceMock = new Mock<ICredentialService>();
        credentialServiceMock
            .Setup(s => s.Validate(userId, It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ICredentialOutcome>.Success(MakeVaultOutcome(grantsAccess: true)));
        var providerMock = MakeResolvingProvider(credentialServiceMock.Object);
        var policy = new UsersServiceImplementationConfiguration { PasswordMaxAgeDays = 90 };
        var service = MakeService(providerMock.Object, userStore.Provider, policy);

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
        var userStore = MakeUserStore();
        SetupGetUser(userStore, userId, userCfg);

        var credentialServiceMock = new Mock<ICredentialService>();
        credentialServiceMock
            .Setup(s => s.Validate(userId, It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ICredentialOutcome>.Success(MakeVaultOutcome(grantsAccess: true)));
        var providerMock = MakeResolvingProvider(credentialServiceMock.Object);
        var policy = new UsersServiceImplementationConfiguration { PasswordMaxAgeDays = 90 };
        var service = MakeService(providerMock.Object, userStore.Provider, policy);

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
        var userStore = MakeUserStore();
        SetupGetUser(userStore, userId, userCfg);

        var credentialServiceMock = new Mock<ICredentialService>();
        credentialServiceMock
            .Setup(s => s.Validate(userId, It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ICredentialOutcome>.Success(MakeVaultOutcome(grantsAccess: true)));
        var providerMock = MakeResolvingProvider(credentialServiceMock.Object);
        var service = MakeService(providerMock.Object, userStore.Provider);

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
        var userStore = MakeUserStore();
        SetupGetUser(userStore, userId, userCfg);

        var credentialServiceMock = new Mock<ICredentialService>();
        credentialServiceMock
            .Setup(s => s.Validate(userId, It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ICredentialOutcome>.Success(MakeVaultOutcome(grantsAccess: false)));
        var providerMock = MakeResolvingProvider(credentialServiceMock.Object);
        var policy = new UsersServiceImplementationConfiguration { MaxFailedLoginAttempts = 5, LockoutDurationMinutes = 15 };
        var service = MakeService(providerMock.Object, userStore.Provider, policy);

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
        var userStore = MakeUserStore();
        SetupGetUser(userStore, userId, userCfg);

        var credentialServiceMock = new Mock<ICredentialService>();
        credentialServiceMock
            .Setup(s => s.Validate(userId, It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ICredentialOutcome>.Success(MakeVaultOutcome(grantsAccess: false)));
        var providerMock = MakeResolvingProvider(credentialServiceMock.Object);
        var policy = new UsersServiceImplementationConfiguration { MaxFailedLoginAttempts = 5, LockoutDurationMinutes = 15 };
        var service = MakeService(providerMock.Object, userStore.Provider, policy);

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
        var userStore = MakeUserStore();
        SetupGetUser(userStore, userId, userCfg);

        var credentialServiceMock = new Mock<ICredentialService>();
        credentialServiceMock
            .Setup(s => s.Validate(userId, It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ICredentialOutcome>.Success(MakeVaultOutcome(grantsAccess: false)));
        var providerMock = MakeResolvingProvider(credentialServiceMock.Object);
        var policy = new UsersServiceImplementationConfiguration { MaxFailedLoginAttempts = 0, LockoutDurationMinutes = 0 };
        var service = MakeService(providerMock.Object, userStore.Provider, policy);

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
        var userStore = MakeUserStore();
        SetupGetUser(userStore, userId, userCfg);
        SetupSaveFails(userStore, new GenericMessage("write conflict"));

        var credentialServiceMock = new Mock<ICredentialService>();
        credentialServiceMock
            .Setup(s => s.Validate(userId, It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ICredentialOutcome>.Success(MakeVaultOutcome(grantsAccess: false)));
        var providerMock = MakeResolvingProvider(credentialServiceMock.Object);
        var policy = new UsersServiceImplementationConfiguration { MaxFailedLoginAttempts = 5, LockoutDurationMinutes = 15 };
        var service = MakeService(providerMock.Object, userStore.Provider, policy);

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
        var userStore = MakeUserStore();
        SetupGetUser(userStore, userId, userCfg);
        SetupSaveFails(userStore, new GenericMessage("write conflict"));

        var credentialServiceMock = new Mock<ICredentialService>();
        credentialServiceMock
            .Setup(s => s.Validate(userId, It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ICredentialOutcome>.Success(MakeVaultOutcome(grantsAccess: true)));
        var providerMock = MakeResolvingProvider(credentialServiceMock.Object);
        var service = MakeService(providerMock.Object, userStore.Provider);

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
        var service = MakeService(providerMock.Object, MakeUserStore().Provider);

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
        var service = MakeService(providerMock.Object, MakeUserStore().Provider);

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
        var policy = new UsersServiceImplementationConfiguration { PasswordHashAlgorithm = "NotARealAlgorithm" };
        var service = MakeService(providerMock.Object, MakeUserStore().Provider, policy);

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
        var service = MakeService(providerMock.Object, MakeUserStore().Provider);

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

        var userStore = MakeUserStore();
        var lookupMessage = new GenericMessage("lookup blew up");
        SetupUserStoreUnreadable(userStore, lookupMessage);

        var service = MakeService(providerMock.Object, userStore.Provider);

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

        var userStore = MakeUserStore();
        SetupGetUser(userStore, userId, null);

        var service = MakeService(providerMock.Object, userStore.Provider);

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

        var userStore = MakeUserStore();
        var userCfg = MakeUserConfig(userId, algorithmName: null, salt: null, mustChangePasswordOnLogin: true);
        SetupGetUser(userStore, userId, userCfg);
        var saveMessage = new GenericMessage("save conflict");
        SetupSaveFails(userStore, saveMessage);

        var service = MakeService(providerMock.Object, userStore.Provider);

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

        var userStore = MakeUserStore();
        var userCfg = MakeUserConfig(userId, algorithmName: null, salt: null, mustChangePasswordOnLogin: true);
        SetupGetUser(userStore, userId, userCfg);

        var service = MakeService(providerMock.Object, userStore.Provider);

        var result = await service.Store(userId, "Password", "newpassword", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        userCfg.AlgorithmName.ShouldBe("Pbkdf2");
        userCfg.Salt.ShouldNotBeNullOrEmpty();
        userCfg.MustChangePasswordOnLogin.ShouldBeFalse();
        userCfg.LastPasswordChangedAt.ShouldNotBeNull();
        userStore.Implementations.Verify(
            p => p.Save(It.IsAny<IUserImplementationConfiguration>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── ForcePasswordChange ──────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ForcePasswordChangeWhenUserLookupFailsWithMessagesReturnsThatFailure()
    {
        var userId = Guid.NewGuid();
        var userStore = MakeUserStore();
        var lookupMessage = new GenericMessage("lookup blew up");
        SetupUserStoreUnreadable(userStore, lookupMessage);
        var service = MakeService(new Mock<ICredentialServiceProvider>().Object, userStore.Provider);

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
        var userStore = MakeUserStore();
        SetupGetUser(userStore, userId, null);
        var service = MakeService(new Mock<ICredentialServiceProvider>().Object, userStore.Provider);

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
        var userStore = MakeUserStore();
        var userCfg = MakeUserConfig(userId);
        SetupGetUser(userStore, userId, userCfg);
        var saveMessage = new GenericMessage("save conflict");
        SetupSaveFails(userStore, saveMessage);
        var service = MakeService(new Mock<ICredentialServiceProvider>().Object, userStore.Provider);

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
        var userStore = MakeUserStore();
        var userCfg = MakeUserConfig(userId, mustChangePasswordOnLogin: false);
        SetupGetUser(userStore, userId, userCfg);
        var service = MakeService(new Mock<ICredentialServiceProvider>().Object, userStore.Provider);

        var result = await service.ForcePasswordChange(userId, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        userCfg.MustChangePasswordOnLogin.ShouldBeTrue();
        userStore.Implementations.Verify(
            p => p.Save(It.IsAny<IUserImplementationConfiguration>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
