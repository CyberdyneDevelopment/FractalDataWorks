using System;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;

namespace Fdw.ApiSurface;

/// <summary>Writes the record of one field or constant. A constant's value is part of the record: it is compiled into the caller.</summary>
internal static class FieldRecords
{
    public static void Write(SurfaceContext context, OwnerInfo owner, FieldDefinitionHandle handle)
    {
        MetadataReader reader = context.Reader;
        FieldDefinition field = reader.GetFieldDefinition(handle);
        string? access = Access.OfField(field.Attributes);
        string name = reader.GetString(field.Name);
        if (access is null || (string.Equals(access, "protected", StringComparison.Ordinal) && !owner.Inheritable) || name.StartsWith('<') || field.Attributes.HasFlag(FieldAttributes.RTSpecialName))
        {
            return;
        }

        string type = field.DecodeSignature(context.Provider, owner.Names);
        string required = CustomAttributes.Has(reader, field.GetCustomAttributes(), "System.Runtime.CompilerServices.RequiredMemberAttribute")
            ? "required"
            : string.Empty;
        string detail = Words.Join("field", access, Shape(field.Attributes), required, "->", type) + ConstantValue(reader, field);
        context.Lines.Add(context.Framework, "MEMBER", owner.Key + "::" + name, detail);
    }

    private static string Shape(FieldAttributes attributes)
    {
        string isStatic = attributes.HasFlag(FieldAttributes.Static) ? "static" : string.Empty;
        string kind = attributes.HasFlag(FieldAttributes.Literal) ? "const" : attributes.HasFlag(FieldAttributes.InitOnly) ? "readonly" : string.Empty;
        return Words.Join(isStatic, kind);
    }

    private static string ConstantValue(MetadataReader reader, FieldDefinition field)
    {
        ConstantHandle handle = field.GetDefaultValue();
        if (handle.IsNil)
        {
            return string.Empty;
        }

        Constant constant = reader.GetConstant(handle);
        object? value = constant.TypeCode == ConstantTypeCode.NullReference ? null : reader.GetBlobReader(constant.Value).ReadConstant(constant.TypeCode);
        return " = " + (value is null ? "null" : Convert.ToString(value, CultureInfo.InvariantCulture));
    }
}
