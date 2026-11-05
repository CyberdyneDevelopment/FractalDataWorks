using System;
using Fdw.Data.Abstractions;
using Moq;

namespace Fdw.Data.MsSql.Tests.TestSupport;

/// <summary>
/// The field type an <see cref="IField"/> double states when its test does not care which: text. <see cref="IField.FieldType"/> is
/// non-nullable and the translators read it for every column (a dictionary row's null binds as the column's type), so a double
/// that leaves it unset is not a field the translators can be given.
/// </summary>
internal static class TestFieldTypes
{
    /// <summary>Gets a text field type: <see cref="string"/>, named String.</summary>
    public static IFieldType Text { get; } = Create(typeof(string), "String");

    /// <summary>Creates a field type double.</summary>
    /// <param name="clrType">The CLR type.</param>
    /// <param name="typeName">The type's name.</param>
    /// <returns>The double.</returns>
    public static IFieldType Create(Type clrType, string typeName)
    {
        var type = new Mock<IFieldType>();
        type.Setup(t => t.ClrType).Returns(clrType);
        type.Setup(t => t.TypeName).Returns(typeName);
        return type.Object;
    }
}
