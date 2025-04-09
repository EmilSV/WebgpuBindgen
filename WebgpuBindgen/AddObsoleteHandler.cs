using CapiGenerator.CSModel;

namespace WebgpuBindgen;

public static class AddObsoleteHandler
{
    public static void AddObsoleteTo(List<CSStruct> structs, List<CSEnum> enums, List<CSStaticClass> staticClasses)
    {
        foreach (var item in structs)
        {
            if (ObsoleteList.Items.Contains(item.GetFullName()))
            {
                item.Attributes.Add(CSAttribute<ObsoleteAttribute>.Create(
                    ["\"This Type is obsolete and will be removed in a future version.\"", "false"], []
                ));
            }

            foreach (var field in item.Fields)
            {
                if (ObsoleteList.Items.Contains(field.GetFullName()))
                {
                    field.Attributes.Add(CSAttribute<ObsoleteAttribute>.Create(
                        ["\"This Field is obsolete and will be removed in a future version.\"", "false"], []
                    ));
                }
            }

            foreach (var method in item.Methods)
            {
                if (ObsoleteList.Items.Contains(method.GetFullName()))
                {
                    method.Attributes.Add(CSAttribute<ObsoleteAttribute>.Create(
                        ["\"This Method is obsolete and will be removed in a future version.\"", "false"], []
                    ));
                }
            }
        }

        foreach (var item in enums)
        {
            if (ObsoleteList.Items.Contains(item.GetFullName()))
            {
                item.Attributes.Add(CSAttribute<ObsoleteAttribute>.Create(
                    ["\"This Enum is obsolete and will be removed in a future version.\"", "false"], []
                ));
            }

            foreach (var field in item.Values)
            {
                if (ObsoleteList.Items.Contains(field.GetFullName()))
                {
                    field.Attributes.Add(CSAttribute<ObsoleteAttribute>.Create(
                        ["\"This Field is obsolete and will be removed in a future version.\"", "false"], []
                    ));
                }
            }
        }

        foreach(var item in translation.StaticClasses)
        {
            if (ObsoleteList.Items.Contains(item.GetFullName()))
            {
                item.Attributes.Add(CSAttribute<ObsoleteAttribute>.Create(
                    ["\"This Type is obsolete and will be removed in a future version.\"", "false"], []
                ));
            }

            foreach (var field in item.Fields)
            {
                if (ObsoleteList.Items.Contains(field.GetFullName()))
                {
                    field.Attributes.Add(CSAttribute<ObsoleteAttribute>.Create(
                        ["\"This Field is obsolete and will be removed in a future version.\"", "false"], []
                    ));
                }
            }
        }
    }
}