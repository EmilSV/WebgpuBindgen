using System.Text.Json;

namespace WebgpuBindgen;

public static class JsonOptions
{
    public static readonly JsonSerializerOptions Value = new()
    {
        PropertyNameCaseInsensitive = true,
    };
}