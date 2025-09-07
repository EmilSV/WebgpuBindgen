using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace WebgpuBindgen.DawnCodegenJson;

public class Function : CodegenItem
{
    public ReturnType? Returns { get; init; }
    [JsonPropertyName("_comment")]
    public string? Comment { get; init; }
    public ImmutableArray<Arg> Args { get; init; }
    public ImmutableArray<Tag> Tags { get; init; }
}
