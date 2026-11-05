using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Security.Encryption;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.Abstractions;
using Moq;
using Shouldly;

namespace Fdw.Security.Encryption.Tests;

public sealed class AesGcmEncryptionCommandExecutorTests
{
    private const string ManagerName = "Main";
    private const string KeyName = "auth-enc";

    private readonly Mock<ISecretManagerProvider> _provider = new();
    private readonly Mock<ISecretManager> _manager = new();

    private AesGcmEncryptionCommandExecutor CreateExecutor(string keyBase64)
    {
        _manager.Setup(m => m.Execute(It.IsAny<ISecretManagerCommand<SecretValue>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => GenericResult<SecretValue>.Success(new SecretValue(KeyName, keyBase64)));
        _provider.Setup(p => p.Get(ManagerName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ISecretManager>.Success(_manager.Object));
        return new AesGcmEncryptionCommandExecutor(_provider.Object, TimeSpan.FromMinutes(5));
    }

    private static string NewKey() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    private static EncryptCommand Encrypt(byte[] plaintext)
        => new() { SecretManagerName = ManagerName, SecretKeyName = KeyName, Plaintext = plaintext };

    private static DecryptCommand Decrypt(byte[] ciphertext)
        => new() { SecretManagerName = ManagerName, SecretKeyName = KeyName, Ciphertext = ciphertext };

    [Fact]
    public async Task EncryptThenDecryptReturnsThePlaintext()
    {
        using var executor = CreateExecutor(NewKey());
        var plaintext = Encoding.UTF8.GetBytes("who has been proved so far");

        var blob = await executor.Execute(Encrypt(plaintext), TestContext.Current.CancellationToken);
        blob.IsSuccess.ShouldBeTrue(blob.CurrentMessage);
        blob.Value!.Length.ShouldBe(12 + plaintext.Length + 16);

        var back = await executor.Execute(Decrypt(blob.Value), TestContext.Current.CancellationToken);
        back.IsSuccess.ShouldBeTrue(back.CurrentMessage);
        back.Value.ShouldBe(plaintext);
    }

    [Fact]
    public async Task EmptyPlaintextRoundTrips()
    {
        using var executor = CreateExecutor(NewKey());

        var blob = await executor.Execute(Encrypt([]), TestContext.Current.CancellationToken);
        blob.IsSuccess.ShouldBeTrue(blob.CurrentMessage);

        var back = await executor.Execute(Decrypt(blob.Value!), TestContext.Current.CancellationToken);
        back.IsSuccess.ShouldBeTrue(back.CurrentMessage);
        back.Value.ShouldBeEmpty();
    }

    [Fact]
    public async Task TheSameInputEncryptsToADifferentBlobEachTime()
    {
        using var executor = CreateExecutor(NewKey());
        var plaintext = Encoding.UTF8.GetBytes("same");

        var first = await executor.Execute(Encrypt(plaintext), TestContext.Current.CancellationToken);
        var second = await executor.Execute(Encrypt(plaintext), TestContext.Current.CancellationToken);

        first.Value.ShouldNotBe(second.Value);
    }

    [Fact]
    public async Task AFlippedByteFailsIntegrityVerification()
    {
        using var executor = CreateExecutor(NewKey());
        var blob = (await executor.Execute(Encrypt(Encoding.UTF8.GetBytes("payload")), TestContext.Current.CancellationToken)).Value!;
        blob[14] ^= 0x01;

        var back = await executor.Execute(Decrypt(blob), TestContext.Current.CancellationToken);

        back.IsFailure.ShouldBeTrue();
        back.Messages[^1].Code.ShouldBe("ENCRYPT-7005");
    }

    [Fact]
    public async Task ABlobFromAnotherKeyFailsIntegrityVerification()
    {
        byte[] blob;
        using (var first = CreateExecutor(NewKey()))
            blob = (await first.Execute(Encrypt(Encoding.UTF8.GetBytes("payload")), TestContext.Current.CancellationToken)).Value!;

        using var second = CreateExecutor(NewKey());
        var back = await second.Execute(Decrypt(blob), TestContext.Current.CancellationToken);

        back.IsFailure.ShouldBeTrue();
        back.Messages[^1].Code.ShouldBe("ENCRYPT-7005");
    }

    [Fact]
    public async Task ABlobShorterThanNonceAndTagFails()
    {
        using var executor = CreateExecutor(NewKey());

        var back = await executor.Execute(Decrypt(new byte[27]), TestContext.Current.CancellationToken);

        back.IsFailure.ShouldBeTrue();
        back.Messages[^1].Code.ShouldBe("ENCRYPT-7006");
    }

    [Fact]
    public async Task AKeyThatIsNotBase64Fails()
    {
        using var executor = CreateExecutor("not base64 !!");

        var result = await executor.Execute(Encrypt([1]), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages[^1].Code.ShouldBe("ENCRYPT-7009");
    }

    [Fact]
    public async Task AKeyThatIsNot32BytesFails()
    {
        using var executor = CreateExecutor(Convert.ToBase64String(new byte[16]));

        var result = await executor.Execute(Encrypt([1]), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages[^1].Code.ShouldBe("ENCRYPT-7004");
    }

    [Fact]
    public async Task AnUnresolvedSecretManagerFails()
    {
        _provider.Setup(p => p.Get(ManagerName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ISecretManager>.Failure(new Fdw.Messages.GenericMessage("no manager")));
        using var executor = new AesGcmEncryptionCommandExecutor(_provider.Object, TimeSpan.FromMinutes(5));

        var result = await executor.Execute(Encrypt([1]), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages[^1].Code.ShouldBe("ENCRYPT-7002");
    }

    [Fact]
    public async Task AnUnreadableKeyFails()
    {
        _manager.Setup(m => m.Execute(It.IsAny<ISecretManagerCommand<SecretValue>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<SecretValue>.Failure(new Fdw.Messages.GenericMessage("no secret")));
        _provider.Setup(p => p.Get(ManagerName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ISecretManager>.Success(_manager.Object));
        using var executor = new AesGcmEncryptionCommandExecutor(_provider.Object, TimeSpan.FromMinutes(5));

        var result = await executor.Execute(Encrypt([1]), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages[^1].Code.ShouldBe("ENCRYPT-7003");
    }

    [Fact]
    public async Task TheKeyIsReadFromTheSecretManagerOnceWhileCached()
    {
        using var executor = CreateExecutor(NewKey());

        await executor.Execute(Encrypt([1]), TestContext.Current.CancellationToken);
        await executor.Execute(Encrypt([2]), TestContext.Current.CancellationToken);

        _manager.Verify(m => m.Execute(It.IsAny<ISecretManagerCommand<SecretValue>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ANullEncryptCommandFails()
    {
        using var executor = CreateExecutor(NewKey());

        var result = await executor.Execute((EncryptCommand)null!, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages[^1].Code.ShouldBe("ENCRYPT-7010");
    }

    [Fact]
    public async Task ANullDecryptCommandFails()
    {
        using var executor = CreateExecutor(NewKey());

        var result = await executor.Execute((DecryptCommand)null!, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages[^1].Code.ShouldBe("ENCRYPT-7010");
    }

    [Fact]
    public async Task ANullPlaintextFails()
    {
        using var executor = CreateExecutor(NewKey());

        var result = await executor.Execute(Encrypt(null!), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain(nameof(EncryptCommand.Plaintext));
    }

    [Fact]
    public async Task ANullCiphertextFails()
    {
        using var executor = CreateExecutor(NewKey());

        var result = await executor.Execute(Decrypt(null!), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain(nameof(DecryptCommand.Ciphertext));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AnEmptyKeyNameFailsWithoutCallingTheSecretManager(string keyName)
    {
        using var executor = CreateExecutor(NewKey());

        var result = await executor.Execute(
            new EncryptCommand { SecretManagerName = ManagerName, SecretKeyName = keyName, Plaintext = [1] },
            TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain(nameof(EncryptCommand.SecretKeyName));
        _provider.Verify(p => p.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AnEmptySecretManagerNameFailsWithoutCallingTheProvider(string managerName)
    {
        using var executor = CreateExecutor(NewKey());

        var result = await executor.Execute(
            new DecryptCommand { SecretManagerName = managerName, SecretKeyName = KeyName, Ciphertext = new byte[40] },
            TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain(nameof(DecryptCommand.SecretManagerName));
        _provider.Verify(p => p.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ANonPositiveCacheLifetimeIsRefused(int seconds)
        => Should.Throw<ArgumentOutOfRangeException>(
            () => new AesGcmEncryptionCommandExecutor(_provider.Object, TimeSpan.FromSeconds(seconds)));
}
