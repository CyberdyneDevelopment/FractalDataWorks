using System;
using System.Reflection.Metadata;

namespace Fdw.ApiSurface;

/// <summary>Writes the record of one event, reachable when its add accessor is.</summary>
internal static class EventRecords
{
    public static void Write(SurfaceContext context, OwnerInfo owner, EventDefinitionHandle handle)
    {
        MetadataReader reader = context.Reader;
        EventDefinition definition = reader.GetEventDefinition(handle);
        EventAccessors accessors = definition.GetAccessors();
        if (accessors.Adder.IsNil)
        {
            return;
        }

        MethodDefinition adder = reader.GetMethodDefinition(accessors.Adder);
        string? access = Access.OfMethod(adder.Attributes);
        if (access is null || (string.Equals(access, "protected", StringComparison.Ordinal) && !owner.Inheritable))
        {
            return;
        }

        string type = HandleNames.Entity(reader, definition.Type, context.Provider, owner.Names);
        string detail = Words.Join("event", access, MemberModifiers.Of(adder.Attributes), "->", type);
        context.Lines.Add(context.Framework, "MEMBER", owner.Key + "::" + reader.GetString(definition.Name), detail);
    }
}
