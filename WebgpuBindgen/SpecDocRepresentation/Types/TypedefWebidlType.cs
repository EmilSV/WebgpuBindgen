namespace WebgpuBindgen.SpecDocRepresentation.Types;

public class TypedefWebidlType : RootWebidlTypeBase
{
    public required IdlTypeBase IdlType { get; set; }
    public required object[] ExtAttrs { get; set; }
}