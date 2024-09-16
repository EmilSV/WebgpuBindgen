using CapiGenerator.CModel;
using CapiGenerator.Parser;
using CppAst;

namespace WebgpuBindgen;

public class WebgpuBindgenConstantParser : ConstantParser
{
    protected override bool ShouldSkip(CppMacro constant)
    {
        if (constant.Name.StartsWith("WGPU_") && constant.Name.EndsWith("_INIT"))
        {
            return true;
        }

        if (constant.Name == "WGPU_COMMA")
        {
            return true;
        }

        return base.ShouldSkip(constant);
    }
}