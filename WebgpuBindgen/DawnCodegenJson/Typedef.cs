using System.Collections.Immutable;

namespace WebgpuBindgen.DawnCodegenJson;



public class Typedef : CodegenItem
{
    public required string Type { get; init; }
    public ImmutableArray<Tag> Tags { get; init; }
}