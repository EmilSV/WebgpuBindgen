using System.Text.Json.Serialization;

namespace WebgpuBindgen.SpecDocRepresentation.Types;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(IdlTypeTypedef), typeDiscriminator: "typedef-type")]
[JsonDerivedType(typeof(IdlTypeAttribute), typeDiscriminator: "attribute-type")]
[JsonDerivedType(typeof(IdlTypeConst), typeDiscriminator: "const-type")]
[JsonDerivedType(typeof(IdlTypeDictionary), typeDiscriminator: "dictionary-type")]
[JsonDerivedType(typeof(IdlTypeReturn), typeDiscriminator: "return-type")]
public abstract class IdlTypeBase
{
    public required string Type { get; set; }
    public required ExtendedAttribute[] ExtAttrs { get; set; }
    public required string Generic { get; set; }
    public required bool Nullable { get; set; }
    public required bool Union { get; set; }
    public required IdlTypeStringUnion IdlType { get; set; }
}