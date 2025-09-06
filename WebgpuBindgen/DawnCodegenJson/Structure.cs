using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace WebgpuBindgen.DawnCodegenJson;

public class Structure : CodegenItem
{
    [JsonConverter(typeof(ExtensibleConverter))]
    public Extensible? Extensible { get; init; }
    public bool? Out { get; init; }
    [JsonConverter(typeof(ExtensibleConverter))]
    public Extensible? Chained { get; init; }
    [JsonPropertyName("chain")]
    public ImmutableArray<string> ChainRoots { get; init; }
    public ImmutableArray<Tag> Tags { get; init; }
    public ImmutableArray<Member> Members { get; init; }
}