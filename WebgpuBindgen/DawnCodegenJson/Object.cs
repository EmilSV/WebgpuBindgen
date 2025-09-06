using System.Collections.Immutable;

namespace WebgpuBindgen.DawnCodegenJson;


public class CodegenObject : CodegenItem
{
    public ImmutableArray<Tag> Tags { get; init; }
    public ImmutableArray<Method> Methods { get; init; }
}