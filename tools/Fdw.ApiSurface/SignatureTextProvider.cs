using System.Collections.Immutable;
using System.Reflection.Metadata;

namespace Fdw.ApiSurface;

/// <summary>Prints the types of a signature as text. An instantiation prints as Name&lt;A,B&gt;; a modifier prints as modreq(...) or modopt(...).</summary>
internal sealed class SignatureTextProvider : ISignatureTypeProvider<string, GenericNames>
{
    public string GetPrimitiveType(PrimitiveTypeCode typeCode) => "System." + typeCode;

    public string GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) =>
        HandleNames.Definition(reader, handle);

    public string GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) =>
        HandleNames.Reference(reader, handle);

    public string GetTypeFromSpecification(MetadataReader reader, GenericNames genericContext, TypeSpecificationHandle handle, byte rawTypeKind) =>
        reader.GetTypeSpecification(handle).DecodeSignature(this, genericContext);

    public string GetSZArrayType(string elementType) => elementType + "[]";

    public string GetArrayType(string elementType, ArrayShape shape) => elementType + "[" + new string(',', shape.Rank - 1) + "]";

    public string GetByReferenceType(string elementType) => elementType + "&";

    public string GetPointerType(string elementType) => elementType + "*";

    public string GetPinnedType(string elementType) => elementType;

    public string GetGenericInstantiation(string genericType, ImmutableArray<string> typeArguments) =>
        genericType + "<" + string.Join(",", typeArguments) + ">";

    public string GetGenericTypeParameter(GenericNames genericContext, int index) => genericContext.Type[index];

    public string GetGenericMethodParameter(GenericNames genericContext, int index) => genericContext.Method[index];

    public string GetModifiedType(string modifier, string unmodifiedType, bool isRequired) =>
        unmodifiedType + (isRequired ? " modreq(" : " modopt(") + modifier + ")";

    public string GetFunctionPointerType(MethodSignature<string> signature) =>
        "fnptr(" + string.Join(",", signature.ParameterTypes) + ")->" + signature.ReturnType;
}
