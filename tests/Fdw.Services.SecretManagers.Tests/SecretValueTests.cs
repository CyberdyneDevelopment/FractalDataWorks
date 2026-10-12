using System;
using System.Collections.Generic;
using Fdw.Services.SecretManagers;
using Shouldly;
using Xunit;

namespace Fdw.Services.SecretManagers.Tests;

public class SecretValueTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void StringConstructorSetsPropertiesCorrectly()
    {
        var now = DateTimeOffset.UtcNow;
        var metadata = new Dictionary<string, object> { ["env"] = "test" };

        using var sut = new SecretValue("myKey", "myValue", "v1", now, now, now.AddDays(30), metadata);

        sut.Key.ShouldBe("myKey");
        sut.Version.ShouldBe("v1");
        sut.CreatedAt.ShouldBe(now);
        sut.ModifiedAt.ShouldBe(now);
        sut.ExpiresAt.ShouldBe(now.AddDays(30));
        sut.IsBinary.ShouldBeFalse();
        sut.Metadata.ShouldContainKey("env");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void BinaryConstructorSetsPropertiesCorrectly()
    {
        var data = new byte[] { 1, 2, 3, 4, 5 };
        var now = DateTimeOffset.UtcNow;

        using var sut = new SecretValue("certKey", data, "v2", now, now, now.AddDays(365));

        sut.Key.ShouldBe("certKey");
        sut.Version.ShouldBe("v2");
        sut.IsBinary.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void StringConstructorWithDefaultsUsesUtcNow()
    {
        var before = DateTimeOffset.UtcNow;
        using var sut = new SecretValue("key", "value");
        var after = DateTimeOffset.UtcNow;

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
    public void StringConstructorWithEmptyKeyThrows()
    {
        Should.Throw<ArgumentException>(() => new SecretValue("", "value"));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void StringConstructorWithWhitespaceKeyThrows()
    {
        Should.Throw<ArgumentException>(() => new SecretValue("   ", "value"));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void BinaryConstructorWithEmptyKeyThrows()
    {
        Should.Throw<ArgumentException>(() => new SecretValue("", new byte[] { 1 }));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void BinaryConstructorWithWhitespaceKeyThrows()
    {
        Should.Throw<ArgumentException>(() => new SecretValue("   ", new byte[] { 1 }));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void GetStringValueReturnsOriginalValue()
    {
        using var sut = new SecretValue("key", "secret-password-123");

        var value = sut.GetStringValue();

        value.ShouldBe("secret-password-123");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void GetStringValueOnBinarySecretThrows()
    {
        using var sut = new SecretValue("key", new byte[] { 1, 2, 3 });

        Should.Throw<InvalidOperationException>(() => sut.GetStringValue());
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void GetBinaryValueReturnsCopyOfOriginal()
    {
        var original = new byte[] { 10, 20, 30, 40 };
        using var sut = new SecretValue("key", original);

        var value = sut.GetBinaryValue();

        value.ShouldBe(new byte[] { 10, 20, 30, 40 });
        // Verify it's a copy, not the same reference
        value.ShouldNotBeSameAs(original);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void GetBinaryValueOnStringSecretThrows()
    {
        using var sut = new SecretValue("key", "text");

        Should.Throw<InvalidOperationException>(() => sut.GetBinaryValue());
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void DisposePreventsFurtherAccess()
    {
        var sut = new SecretValue("key", "value");
        sut.Dispose();

        Should.Throw<ObjectDisposedException>(() => sut.GetStringValue());
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void DisposeOnBinaryPreventsFurtherAccess()
    {
        var sut = new SecretValue("key", new byte[] { 1, 2 });
        sut.Dispose();

        Should.Throw<ObjectDisposedException>(() => sut.GetBinaryValue());
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void DoubleDisposeDoesNotThrow()
    {
        var sut = new SecretValue("key", "value");
        sut.Dispose();
        Should.NotThrow(() => sut.Dispose());
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void IsExpiredReturnsFalseWhenNoExpiration()
    {
        using var sut = new SecretValue("key", "value");

        sut.IsExpired.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void IsExpiredReturnsFalseWhenNotYetExpired()
    {
        using var sut = new SecretValue("key", "value", expiresAt: DateTimeOffset.UtcNow.AddDays(30));

        sut.IsExpired.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void IsExpiredReturnsTrueWhenExpired()
    {
        using var sut = new SecretValue("key", "value", expiresAt: DateTimeOffset.UtcNow.AddDays(-1));

        sut.IsExpired.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void AccessStringValueExecutesCallback()
    {
        using var sut = new SecretValue("key", "my-secret");

        var result = sut.AccessStringValue(val => val.Length);

        result.ShouldBe(9);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void AccessBinaryValueExecutesCallback()
    {
        using var sut = new SecretValue("key", new byte[] { 1, 2, 3 });

        var result = sut.AccessBinaryValue(val => val.Length);

        result.ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void BinaryConstructorCopiesInputArray()
    {
        var original = new byte[] { 1, 2, 3 };
        using var sut = new SecretValue("key", original);

        // Modify the original - should not affect the stored value
        original[0] = 99;

        var stored = sut.GetBinaryValue();
        stored[0].ShouldBe((byte)1);
    }
}
