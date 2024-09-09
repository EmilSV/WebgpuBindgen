namespace WebgpuBindgen.SpecDocRepresentation;

public abstract class RootWebidlType
{
    public required string Type { get; set; }
    public required string Name { get; set; }
    public required string Inheritance { get; set; }
    
    public required bool Partial { get; set; }
}