using System.Text.Json.Nodes;

namespace WebgpuBindgen.DawnCodegenJson;


public class Member
{
    public required string Name { get; init; }
    public required string Type { get; init; }
    public string? Annotation { get; init; }
    public bool? Optional { get; init; }
    public NumberOrString Length { get; init; }
    public JsonValue? Default { get; init; }
}