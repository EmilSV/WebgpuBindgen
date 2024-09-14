using WebgpuBindgen.SpecDocRepresentation.Defaults;
using WebgpuBindgen.SpecDocRepresentation.Types;

namespace WebgpuBindgen.SpecDocRepresentation;

public class Argument
{
    public required string Type { get; set; }
    public required string Name { get; set; }
    public required IdlTypeBase IdlType { get; set; }
    public required DefaultValueBase? Default { get; set; }
    public required bool Optional { get; set; }
    public required bool Variadic { get; set; }
}