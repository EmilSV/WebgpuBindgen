namespace WebgpuBindgen.DawnCodegenJson;


public class Constant : CodegenItem
{
    public required string Type { get; init; }
    public required string Value { get; init; }
}