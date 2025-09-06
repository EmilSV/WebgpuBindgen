using System.Collections.Immutable;

namespace WebgpuBindgen.DawnCodegenJson;

public class CallbackFunction : CodegenItem
{
    public ImmutableArray<Arg> Args { get; init; }
    public ImmutableArray<Tag> Tags { get; init; }
}