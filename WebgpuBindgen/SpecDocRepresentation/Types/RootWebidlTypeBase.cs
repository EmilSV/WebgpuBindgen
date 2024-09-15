using System.Text.Json.Serialization;

namespace WebgpuBindgen.SpecDocRepresentation.Types;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(EnumWebidlType), typeDiscriminator: "enum")]
[JsonDerivedType(typeof(DictionaryWebidlType), typeDiscriminator: "dictionary")]
[JsonDerivedType(typeof(InterfaceWebidlType), typeDiscriminator: "interface")]
[JsonDerivedType(typeof(InterfaceMixinWebidlType), typeDiscriminator: "interface mixin")]
[JsonDerivedType(typeof(NamespaceWebidlType), typeDiscriminator: "namespace")]
[JsonDerivedType(typeof(TypedefWebidlType), typeDiscriminator: "typedef")]
public abstract class RootWebidlTypeBase
{
    public required string Name { get; set; }
}