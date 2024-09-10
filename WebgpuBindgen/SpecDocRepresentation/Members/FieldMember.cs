using System.Text.Json.Serialization;

namespace WebgpuBindgen.SpecDocRepresentation.Members;

public class FieldMember : WebidlMember
{
    [JsonPropertyName("default")]
    public BaseDefaultValue? DefaultValue { get; set; }
    public required IdlType IdlType { get; set; }
    public required bool Required { get; set; }
}