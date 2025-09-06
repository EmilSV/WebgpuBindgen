using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace WebgpuBindgen.DawnCodegenJson;

public class Native : CodegenItem
{
    [JsonPropertyName("wasm type")]
    public string? WasmType { get; init; }
    [JsonPropertyName("wire transparent")]
    public bool? WireTransparent { get; init; }
}