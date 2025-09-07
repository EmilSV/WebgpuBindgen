using System.Text.Json.Serialization;

namespace WebgpuBindgen.DawnCodegenJson;

[JsonConverter(typeof(ReturnTypeConverter))]
public class ReturnType(string? type, bool? optional)
{
    public string? Type { get; private set; } = type;
    public bool? Optional { get; private set; } = optional;
}