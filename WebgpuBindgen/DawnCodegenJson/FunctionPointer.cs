using System.Collections.Immutable;

namespace WebgpuBindgen.DawnCodegenJson;

public class FunctionPointer : CodegenItem
{
    public ReturnType? Returns { get; init; }
    public ImmutableArray<Arg> Args { get; init; }
    public ImmutableArray<Tag> Tags { get; init; }
}