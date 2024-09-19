using CapiGenerator.CSModel;

namespace WebgpuBindgen;

public static class StaticClassFixer
{
    public static Task RemoveMembers(List<CSStaticClass> staticClasses)
    {
        foreach (var item in staticClasses)
        {
            var method = item.Methods.First(i => i.Name?.EndsWith("GetProcAddress") ?? false);
            item.Methods.Remove(method);

            var method2 = item.Methods.First(i => i.Name?.EndsWith("GetProcAddress2") ?? false);
            item.Methods.Remove(method2);

            var field = item.Fields.First(i => i.Name?.EndsWith("SKIP_PROCS") ?? false);
            item.Fields.Remove(field);
        }

        return Task.CompletedTask;
    }

    public static Task FixMemberNames(List<CSStaticClass> staticClasses)
    {
        foreach (var item in staticClasses)
        {
            foreach (var method in item.Methods)
            {
                const string prefix = "wgpu";
                if (method.Name?.StartsWith(prefix) ?? false)
                {
                    method.Name = method.Name.Substring(prefix.Length);
                }
            }

            foreach (var field in item.Fields)
            {
                const string prefix = "WGPU_";
                if (field.Name?.StartsWith(prefix) ?? false)
                {
                    field.Name = field.Name.Substring(prefix.Length);
                }
            }
        }

        return Task.CompletedTask;
    }

    public static Task MakeStaticClassesPartial(List<CSStaticClass> staticClasses)
    {
        foreach (var item in staticClasses)
        {
            item.IsPartial = true;
        }

        return Task.CompletedTask;
    }
}
