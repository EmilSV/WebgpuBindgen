using System.Text.Json.Serialization;

namespace WebgpuBindgen.SpecDocRepresentation.Defaults;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(DefaultString), typeDiscriminator: "string")]
[JsonDerivedType(typeof(DefaultSequence), typeDiscriminator: "sequence")]
[JsonDerivedType(typeof(DefaultNumber), typeDiscriminator: "number")]
[JsonDerivedType(typeof(DefaultDictionary), typeDiscriminator: "dictionary")]
[JsonDerivedType(typeof(DefaultBoolean), typeDiscriminator: "boolean")]
public abstract class DefaultValueBase
{
}