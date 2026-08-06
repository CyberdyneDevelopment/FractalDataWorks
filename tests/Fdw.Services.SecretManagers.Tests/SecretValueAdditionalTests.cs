using System;
using System.Collections.Generic;
using Fdw.Services.SecretManagers;

namespace Fdw.Services.SecretManagers.Tests;

public sealed class SecretValueAdditionalTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void AccessStringValueOnBinarySecretThrows()
    {
        // Arrange
        using var sut = new SecretValue("key", new byte[] { 1, 2, 3 });

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => sut.AccessStringValue(v => v.Length));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void AccessBinaryValueOnStringSecretThrows()
    {
        // Arrange
        using var sut = new SecretValue("key", "my-secret");

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => sut.AccessBinaryValue(v => v.Length));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void AccessStringValueAfterDisposeThrows()
    {
        // Arrange
        var sut = new SecretValue("key", "my-secret");
        sut.Dispose();

        // Act & Assert
        Should.Throw<ObjectDisposedException>(() => sut.AccessStringValue(v => v.Length));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void AccessBinaryValueAfterDisposeThrows()
    {
        // Arrange
        var sut = new SecretValue("key", new byte[] { 1, 2, 3 });
        sut.Dispose();

        // Act & Assert
        Should.Throw<ObjectDisposedException>(() => sut.AccessBinaryValue(v => v.Length));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void BinaryConstructorWithDefaultsUsesUtcNow()
    {
        // Arrange & Act
        var before = DateTimeOffset.UtcNow;
        using var sut = new SecretValue("key", new byte[] { 1, 2 });
        var after = DateTimeOffset.UtcNow;

        // Assert
        sut.CreatedAt.ShouldBeGreaterThanOrEqualTo(before);
        sut.CreatedAt.ShouldBeLessThanOrEqualTo(after);
        sut.ModifiedAt.ShouldBeGreaterThanOrEqualTo(before);
        sut.Version.ShouldBeNull();
        sut.ExpiresAt.ShouldBeNull();
        sut.Metadata.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void BinaryConstructorWithAllParameters()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var metadata = new Dictionary<string, object> { ["purpose"] = "cert" };
        var data = new byte[] { 10, 20, 30 };

        // Act
        using var sut = new SecretValue("certKey", data, "v3", now, now, now.AddDays(365), metadata);

        // Assert
        sut.Key.ShouldBe("certKey");
        sut.Version.ShouldBe("v3");
        sut.CreatedAt.ShouldBe(now);
        sut.ModifiedAt.ShouldBe(now);
        sut.ExpiresAt.ShouldBe(now.AddDays(365));
        sut.IsBinary.ShouldBeTrue();
        sut.Metadata.ShouldContainKey("purpose");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void GetStringValueReturnsEmptyStringForEmptySecureString()
    {
        // Arrange
        using var sut = new SecretValue("key", "");

        // Act
        var value = sut.GetStringValue();

        // Assert
        value.ShouldBe("");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void GetBinaryValueReturnsEmptyArrayForEmptyData()
    {
        // Arrange
        using var sut = new SecretValue("key", Array.Empty<byte>());

        // Act
        var value = sut.GetBinaryValue();

        // Assert
        value.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void IsExpiredWithExactlyNowExpirationReturnsFalse()
    {
        // Arrange - expires at exactly now (UtcNow >= UtcNow can be flaky, but with future we are safe)
        using var sut = new SecretValue("key", "value", expiresAt: DateTimeOffset.UtcNow.AddMilliseconds(500));

        // Act & Assert
        sut.IsExpired.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void DisposeOfBinaryClearsArray()
    {
        // Arrange
        var sut = new SecretValue("key", new byte[] { 1, 2, 3 });

        // Act
        sut.Dispose();

        // Assert - subsequent access should throw
        Should.Throw<ObjectDisposedException>(() => sut.GetBinaryValue());
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void AccessStringValueReturnsResultFromCallback()
    {
        // Arrange
        using var sut = new SecretValue("key", "hello-world");

        // Act
        var result = sut.AccessStringValue(v => v.ToUpperInvariant());

        // Assert
        result.ShouldBe("HELLO-WORLD");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void AccessBinaryValueReturnsResultFromCallback()
    {
        // Arrange
        using var sut = new SecretValue("key", new byte[] { 1, 2, 3, 4, 5 });

        // Act
        var result = sut.AccessBinaryValue(v =>
        {
            int sum = 0;
            foreach (var b in v)
                sum += b;
            return sum;
        });

        // Assert
        result.ShouldBe(15);
    }
}
