using System.Text.Json.Serialization;

namespace WebgpuBindgen.DawnCodegenJson;


public class Arg
{
    public required string Name { get; init; }
    public required string Type { get; init; }
    public string? Annotation { get; init; }
    public bool? Optional { get; init; }
    public string? Default { get; init; }
    [JsonPropertyName("no_default")]
    public bool? NoDefault { get; init; }
    public NumberOrString Length { get; init; }
}