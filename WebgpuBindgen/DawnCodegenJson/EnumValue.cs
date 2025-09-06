using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace WebgpuBindgen.DawnCodegenJson;

public class EnumValue
{
    public required int Value { get; init; }
    public required string Name { get; init; }
    public string? Jsrepr { get; init; }
    public bool? Valid { get; init; }
    [JsonPropertyName("emscripten_string_to_int")]
    public bool? EmscriptenStringToInt { get; init; }
    public ImmutableArray<Tag> Tags { get; init; }
}
