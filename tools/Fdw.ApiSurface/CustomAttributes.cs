using System;
using System.Reflection.Metadata;

namespace Fdw.ApiSurface;

/// <summary>Finds an attribute on a declaration by the full name of its type.</summary>
internal static class CustomAttributes
{
    public static bool Has(MetadataReader reader, CustomAttributeHandleCollection handles, string fullName)
    {
        foreach (CustomAttributeHandle handle in handles)
        {
            if (string.Equals(AttributeTypeName(reader, reader.GetCustomAttribute(handle).Constructor), fullName, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static string AttributeTypeName(MetadataReader reader, EntityHandle constructor)
    {
        EntityHandle owner = constructor.Kind == HandleKind.MemberReference
            ? reader.GetMemberReference((MemberReferenceHandle)constructor).Parent
            : reader.GetMethodDefinition((MethodDefinitionHandle)constructor).GetDeclaringType();
        return owner.Kind switch
        {
            HandleKind.TypeReference => HandleNames.Reference(reader, (TypeReferenceHandle)owner),
            HandleKind.TypeDefinition => HandleNames.Definition(reader, (TypeDefinitionHandle)owner),
            _ => string.Empty,
        };
    }
}
