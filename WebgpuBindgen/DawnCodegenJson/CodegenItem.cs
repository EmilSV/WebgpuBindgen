using System.Text.Json;
using System.Text.Json.Serialization;

namespace WebgpuBindgen.DawnCodegenJson;



[JsonPolymorphic(
    TypeDiscriminatorPropertyName = "category",
    UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToBaseType)]
[JsonDerivedType(typeof(Structure), typeDiscriminator: "structure")]
[JsonDerivedType(typeof(Function), typeDiscriminator: "function")]
[JsonDerivedType(typeof(FunctionPointer), typeDiscriminator: "function pointer")]
[JsonDerivedType(typeof(CodegenEnum), typeDiscriminator: "enum")]
[JsonDerivedType(typeof(Bitmask), typeDiscriminator: "bitmask")]
[JsonDerivedType(typeof(Native), typeDiscriminator: "native")]
[JsonDerivedType(typeof(CodegenObject), typeDiscriminator: "object")]
[JsonDerivedType(typeof(CallbackInfo), typeDiscriminator: "callback info")]
[JsonDerivedType(typeof(CallbackFunction), typeDiscriminator: "callback function")]
public class CodegenItem
{
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? Extra { get; set; }
}