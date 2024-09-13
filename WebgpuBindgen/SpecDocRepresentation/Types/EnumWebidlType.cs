namespace WebgpuBindgen.SpecDocRepresentation.Types;

public class EnumWebidlType : RootWebidlTypeBase
{
    public sealed class EnumValue
    {
        public required string Type { get; set; }
        public required string Value { get; set; }
    }

    public required EnumValue[] Values { get; set; }
    public required object[] ExtAttrs { get; set; }
}