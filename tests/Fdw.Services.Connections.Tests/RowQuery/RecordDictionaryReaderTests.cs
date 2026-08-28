using System;
using System.Collections.Generic;
using Fdw.Services.Connections.RowQuery;

namespace Fdw.Services.Connections.Tests.RowQuery;

/// <summary>
/// Unit coverage for <see cref="RecordDictionaryReader"/> — the <c>DbDataReader</c> shim over
/// in-memory row dictionaries. Typed getters COERCE (they do not hard-cast), and
/// <see cref="System.Data.Common.DbDataReader.GetOrdinal"/> throws
/// <see cref="IndexOutOfRangeException"/> for an unknown column, matching the contract the
/// generated PocoMapper's <c>GetReaderValue_*</c> helpers depend on.
/// </summary>
public sealed class RecordDictionaryReaderTests
{
    private static IReadOnlyDictionary<string, object?> Row(params (string Key, object? Value)[] fields)
    {
        var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in fields)
            dict[key] = value;
        return dict;
    }

    [Fact]
    [Trait("Category", "RowQuery")]
    public void ReadAdvancesThroughEachRowAndReturnsFalseAtTheEnd()
    {
        var reader = new RecordDictionaryReader([Row(("Id", 1L)), Row(("Id", 2L))]);

        reader.Read().ShouldBeTrue();
        reader.Read().ShouldBeTrue();
        reader.Read().ShouldBeFalse();
    }

    [Fact]
    [Trait("Category", "RowQuery")]
    [Trait("Category", "Coercion")]
    public void GetGuidCoercesAStringValue()
    {
        var guid = Guid.NewGuid();
        var reader = new RecordDictionaryReader([Row(("Id", guid.ToString()))]);
        reader.Read();

        reader.GetGuid(reader.GetOrdinal("Id")).ShouldBe(guid);
    }

    [Fact]
    [Trait("Category", "RowQuery")]
    [Trait("Category", "Coercion")]
    public void GetGuidReturnsANativeGuidValueUnchanged()
    {
        var guid = Guid.NewGuid();
        var reader = new RecordDictionaryReader([Row(("Id", guid))]);
        reader.Read();

        reader.GetGuid(reader.GetOrdinal("Id")).ShouldBe(guid);
    }

    [Fact]
    [Trait("Category", "RowQuery")]
    [Trait("Category", "Coercion")]
    public void GetBooleanCoercesALongValue()
    {
        var reader = new RecordDictionaryReader([Row(("IsCurrent", 1L))]);
        reader.Read();

        reader.GetBoolean(reader.GetOrdinal("IsCurrent")).ShouldBeTrue();
    }

    [Fact]
    [Trait("Category", "RowQuery")]
    [Trait("Category", "BoolCoercion")]
    public void GetBooleanCoercesTheStringZeroWithoutThrowing()
    {
        var reader = new RecordDictionaryReader([Row(("IsDeleted", "0"))]);
        reader.Read();

        Should.NotThrow(() => reader.GetBoolean(reader.GetOrdinal("IsDeleted")).ShouldBeFalse());
    }

    [Fact]
    [Trait("Category", "RowQuery")]
    [Trait("Category", "BoolCoercion")]
    public void GetBooleanCoercesTheStringOneWithoutThrowing()
    {
        var reader = new RecordDictionaryReader([Row(("IsCurrent", "1"))]);
        reader.Read();

        Should.NotThrow(() => reader.GetBoolean(reader.GetOrdinal("IsCurrent")).ShouldBeTrue());
    }

    [Fact]
    [Trait("Category", "RowQuery")]
    public void GetStringReturnsTheRawStringValue()
    {
        var reader = new RecordDictionaryReader([Row(("Name", "EnvSecrets"))]);
        reader.Read();

        reader.GetString(reader.GetOrdinal("Name")).ShouldBe("EnvSecrets");
    }

    [Fact]
    [Trait("Category", "RowQuery")]
    [Trait("Category", "Coercion")]
    public void GetFieldValueOfGuidCoercesAStringValue()
    {
        var guid = Guid.NewGuid();
        var reader = new RecordDictionaryReader([Row(("Id", guid.ToString()))]);
        reader.Read();

        reader.GetFieldValue<Guid>(reader.GetOrdinal("Id")).ShouldBe(guid);
    }

    [Fact]
    [Trait("Category", "RowQuery")]
    public void GetFieldValueOfBoolCoercesANativeBoolValue()
    {
        var reader = new RecordDictionaryReader([Row(("IsEnabled", true))]);
        reader.Read();

        reader.GetFieldValue<bool>(reader.GetOrdinal("IsEnabled")).ShouldBeTrue();
    }

    [Fact]
    [Trait("Category", "RowQuery")]
    public void GetOrdinalThrowsIndexOutOfRangeExceptionForAnUnknownColumn()
    {
        var reader = new RecordDictionaryReader([Row(("Name", "EnvSecrets"))]);
        reader.Read();

        Should.Throw<IndexOutOfRangeException>(() => reader.GetOrdinal("DoesNotExist"));
    }

    [Fact]
    [Trait("Category", "RowQuery")]
    public void IsDBNullReturnsTrueForAMissingColumnOnASpecificRow()
    {
        var reader = new RecordDictionaryReader([Row(("Name", "A")), Row(("Name", "B"), ("Description", "x"))]);
        reader.Read();

        reader.IsDBNull(reader.GetOrdinal("Description")).ShouldBeTrue();
    }

    [Fact]
    [Trait("Category", "RowQuery")]
    public void IsDBNullReturnsTrueForAnExplicitNullValue()
    {
        var reader = new RecordDictionaryReader([Row(("Description", null))]);
        reader.Read();

        reader.IsDBNull(reader.GetOrdinal("Description")).ShouldBeTrue();
    }
}
