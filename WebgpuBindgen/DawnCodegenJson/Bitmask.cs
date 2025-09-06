using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace WebgpuBindgen.DawnCodegenJson;


public class Bitmask : CodegenItem
{
    [JsonPropertyName("emscripten_no_enum_table")]
    public bool? EmscriptenNoEnumTable { get; init; }
    [JsonPropertyName("emscripten_string_to_int")]
    public bool? EmscriptenStringToInt { get; init; }
    [JsonPropertyName("_comment")]
    public string? Comment { get; init; }

    public ImmutableArray<Tag> Tags { get; init; }

    public ImmutableArray<EnumValue> Values { get; init; }
}