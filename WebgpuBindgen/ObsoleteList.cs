using System.Collections.Frozen;

namespace WebgpuBindgen;


public static class ObsoleteList
{
    public static FrozenSet<string> Items = FrozenSet.Create(
        "WebGpuSharp.FFI.WebGPU_FFI.CommandEncoderWriteTimestamp"
    );
}