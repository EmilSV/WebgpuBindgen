using System.Text.Json.Serialization;

namespace WebgpuBindgen.SpecDocRepresentation.Members;


[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(FieldMember), typeDiscriminator: "field")]
[JsonDerivedType(typeof(AttributeMember), typeDiscriminator: "attribute")]
[JsonDerivedType(typeof(ConstMember), typeDiscriminator: "const")]
[JsonDerivedType(typeof(OperationMember), typeDiscriminator: "operation")]
[JsonDerivedType(typeof(SetlikeMember), typeDiscriminator: "setlike")]
[JsonDerivedType(typeof(ConstructorMember), typeDiscriminator: "constructor")]
public abstract class WebidlMemberBase
{
    public required string Type { get; set; }
}
