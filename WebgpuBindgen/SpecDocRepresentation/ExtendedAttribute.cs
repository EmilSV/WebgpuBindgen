namespace WebgpuBindgen.SpecDocRepresentation;

public class ExtendedAttribute
{
    public class RhsType
    {
        public required string Type { get; set; }
        public required ValueType Value { get; set; }
    }

    public class ValueType
    {
        public required string Value { get; set; }
    }

    public required string Type { get; set; }
    public required string Name { get; set; }

    public required Argument[] Arguments { get; set; }
}