using CapiGenerator.CSModel;

namespace WebgpuBindgen;


public static class EnumFixer
{

    public static Task FixEnums(IEnumerable<CSEnum> enums)
    {
        foreach (var item in enums)
        {
            string name = item.Name;
            var force32BitItem = item.Values.First(i => i.Name.EndsWith("_Force32"));
            item.Values.Remove(force32BitItem);
            foreach (var value in item.Values)
            {
                string newName = value.Name;
                if (value.Name.StartsWith($"{name}_"))
                {
                    newName = value.Name.Substring(name.Length + 1);
                }

                if (newName.Length > 1 && char.IsDigit(newName[0]) && newName[1] == 'D')
                {
                    char digit = newName[0];
                    newName = $"D{digit}{newName.Substring(2)}";
                }
            }

            if (name.StartsWith("WGPU"))
            {
                item.Name = name.Substring(4);
            }
        }

        return Task.CompletedTask;
    }

    public static Task FixFlagEnumAttributes(List<CSEnum> enums, List<CSStruct> structs)
    {
        foreach (var item in enums)
        {
            if (item.Attributes.Any(a => a is CSAttribute<FlagsAttribute>))
            {
                continue;
            }

            string name = item.Name;
            var flagStruct = structs.Find(i => i.Name == $"{name}Flags");

            item.Attributes.Add(CSAttribute<FlagsAttribute>.Create(
                [],
                []
            ));
        }

        return Task.CompletedTask;
    }
}