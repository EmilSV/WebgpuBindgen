using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace WebgpuBindgen.DawnCodegenJson
{
    public class Method
    {
        public required string Name { get; init; }
        public ReturnType? Returns { get; init; }
        [JsonPropertyName("no autolock")]
        public bool? NoAutoLock { get; init; }
        public ImmutableArray<Arg> Parameters { get; init; }
        public ImmutableArray<Tag> Tags { get; init; }
    }
}