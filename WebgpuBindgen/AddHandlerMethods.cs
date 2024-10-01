using CapiGenerator.CSModel;
using static CapiGenerator.CSModel.CSClassMemberModifierConsts;

namespace WebgpuBindgen;

public static class AddHandlerMethods
{
    public static Task AddMethods(List<CSStruct> structs, CSStaticClass staticClass)
    {
        foreach (var item in structs)
        {
            if (!item.Name.EndsWith("Handle"))
            {
                continue;
            }

            var typeNameWithoutHandle = item.Name[0..^"Handle".Length];

            var methodsToAdd = staticClass.Methods
                .Where(
                    i => i.Parameters.Count > 0 &&
                    i.Parameters[0].Type.Type == item &&
                    (i.Name?.StartsWith(typeNameWithoutHandle) ?? false))
                .ToList();

            foreach (var method in methodsToAdd)
            {
                string name = GetMethodName(typeNameWithoutHandle, methodsToAdd, method);
                var parameter = method.Parameters.Skip(1).ToArray();
                var returnType = method.ReturnType;
                item.Methods.Add(new(PUBLIC, returnType, name, parameter)
                {
                    Body = $"=> {method.GetFullName()}({string.Join(", ", ["this", .. parameter.Select(i => i.Name)])});",
                });
            }
        }

        return Task.CompletedTask;
    }

    private static string GetMethodName(string typeNameWithoutHandle, List<CSMethod> methodsToAdd, CSMethod method)
    {
        string name;
        string nameWithoutNumberAtEnd = method.Name!;

        // Fix two names are the same expect for number at the end
        for (int i = nameWithoutNumberAtEnd.Length - 1; i >= 0; i--)
        {
            if (!char.IsNumber(nameWithoutNumberAtEnd[i]))
            {
                nameWithoutNumberAtEnd = nameWithoutNumberAtEnd[0..(i + 1)];
                break;
            }
        }

        if (methodsToAdd.Any(i => i.Name == nameWithoutNumberAtEnd))
        {
            name = nameWithoutNumberAtEnd;
        }
        else
        {
            name = method.Name!;
        }

        name = name![typeNameWithoutHandle.Length..];
        return name;
    }
}