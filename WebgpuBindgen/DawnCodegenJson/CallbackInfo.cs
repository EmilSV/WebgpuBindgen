using System.Collections.Immutable;

namespace WebgpuBindgen.DawnCodegenJson;

public class CallbackInfo : CodegenItem
{
    public ImmutableArray<Method> Members { get; init; }
    public ImmutableArray<Tag> Tags { get; init; }
}