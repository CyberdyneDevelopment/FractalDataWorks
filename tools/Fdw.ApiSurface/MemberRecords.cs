using System.Collections.Generic;
using System.Reflection.Metadata;

namespace Fdw.ApiSurface;

/// <summary>Writes the records of one type's members. Property and event accessors are not listed as methods; the property or event carries them.</summary>
internal static class MemberRecords
{
    public static void Write(SurfaceContext context, TypeDefinition definition, OwnerInfo owner)
    {
        HashSet<MethodDefinitionHandle> accessors = AccessorsOf(context.Reader, definition);
        foreach (MethodDefinitionHandle handle in definition.GetMethods())
        {
            if (!accessors.Contains(handle))
            {
                MethodRecords.Write(context, owner, handle);
            }
        }

        foreach (PropertyDefinitionHandle handle in definition.GetProperties())
        {
            PropertyRecords.Write(context, owner, handle);
        }

        foreach (FieldDefinitionHandle handle in definition.GetFields())
        {
            FieldRecords.Write(context, owner, handle);
        }

        foreach (EventDefinitionHandle handle in definition.GetEvents())
        {
            EventRecords.Write(context, owner, handle);
        }
    }

    private static HashSet<MethodDefinitionHandle> AccessorsOf(MetadataReader reader, TypeDefinition definition)
    {
        HashSet<MethodDefinitionHandle> accessors = [];
        foreach (PropertyDefinitionHandle handle in definition.GetProperties())
        {
            PropertyAccessors found = reader.GetPropertyDefinition(handle).GetAccessors();
            accessors.UnionWith([found.Getter, found.Setter]);
            accessors.UnionWith(found.Others);
        }

        foreach (EventDefinitionHandle handle in definition.GetEvents())
        {
            EventAccessors found = reader.GetEventDefinition(handle).GetAccessors();
            accessors.UnionWith([found.Adder, found.Remover, found.Raiser]);
            accessors.UnionWith(found.Others);
        }

        return accessors;
    }
}
