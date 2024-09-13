using System.Text.Json.Serialization;

namespace WebgpuBindgen.SpecDocRepresentation.Members;


[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(FieldMember), typeDiscriminator: "field")]
[JsonDerivedType(typeof(AttributeMember), typeDiscriminator: "attribute")]
[JsonDerivedType(typeof(OperationMember), typeDiscriminator: "dfn")]
public abstract class WebidlMemberBase
{
    public required string Type { get; set; }
}
