using System;
using System.Reflection;
using System.Reflection.Metadata;

namespace Fdw.ApiSurface;

/// <summary>What a consumer outside the assembly can reach: public, or protected where a subclass can. Anything else is not API.</summary>
internal static class Access
{
    public static string? OfMethod(MethodAttributes attributes)
    {
        return (attributes & MethodAttributes.MemberAccessMask) switch
        {
            MethodAttributes.Public => "public",
            MethodAttributes.Family or MethodAttributes.FamORAssem => "protected",
            _ => null,
        };
    }

    public static string? OfField(FieldAttributes attributes)
    {
        return (attributes & FieldAttributes.FieldAccessMask) switch
        {
            FieldAttributes.Public => "public",
            FieldAttributes.Family or FieldAttributes.FamORAssem => "protected",
            _ => null,
        };
    }

    public static string? Higher(string? first, string? second)
    {
        if (string.Equals(first, "public", StringComparison.Ordinal) || string.Equals(second, "public", StringComparison.Ordinal))
        {
            return "public";
        }

        if (first is not null)
        {
            return first;
        }

        return second;
    }

    public static bool IsVisible(MetadataReader reader, TypeDefinition definition)
    {
        switch (definition.Attributes & TypeAttributes.VisibilityMask)
        {
            case TypeAttributes.Public:
                return true;
            case TypeAttributes.NestedPublic:
            case TypeAttributes.NestedFamily:
            case TypeAttributes.NestedFamORAssem:
                return IsVisible(reader, reader.GetTypeDefinition(definition.GetDeclaringType()));
            default:
                return false;
        }
    }
}
