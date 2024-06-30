using System.Diagnostics.CodeAnalysis;
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
                    newName = value.Name[(name.Length + 1)..];
                }

                if (newName.Length > 1 && char.IsDigit(newName[0]) && newName[1] == 'D')
                {
                    char digit = newName[0];
                    newName = $"D{digit}{newName[2..]}";
                }

                value.Name = newName;
            }

            if (name.StartsWith("WGPU"))
            {
                item.Name = name[4..];
            }
        }

        return Task.CompletedTask;
    }

    public static Task FixFlagEnumAttributes(List<CSEnum> enums, List<CSStruct> structs, List<CSStaticClass> staticClasses)
    {
        List<CSStruct> flagStructsToRemove = new();

        foreach (CSEnum item in enums)
        {
            if (item.Attributes.Any(a => a is CSAttribute<FlagsAttribute>))
            {
                continue;
            }

            string nonFlagName = item.Name;
            string flagName = $"{nonFlagName}Flags";
            var flagStruct = structs.Find(i => i.Name == flagName);
            if (flagStruct == null)
            {
                continue;
            }

            flagStructsToRemove.Add(flagStruct);

            bool Predicate(ICSType type, [NotNullWhen(true)] out ICSType? newType)
            {
                if (type == flagStruct)
                {
                    newType = item;
                    return true;
                }
                newType = null;
                return false;
            }

            ITypeReplace.ReplaceTypes([.. enums, .. structs, .. staticClasses], Predicate);

            item.Attributes.Add(CSAttribute<FlagsAttribute>.Create(
                [],
                []
            ));
        }

        foreach (var item in flagStructsToRemove)
        {
            structs.Remove(item);
        }

        structs.RemoveAll(i => i.Name.EndsWith("Flag"));

        return Task.CompletedTask;
    }
}