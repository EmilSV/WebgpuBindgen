using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace WebgpuBindgen.DawnCodegenJson;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Tag
{
    Dawn,
    Emscripten,
    Native,
    Compat,
    Art,
    Upstream
}