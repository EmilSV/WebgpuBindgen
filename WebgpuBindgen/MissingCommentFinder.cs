using System.Text;
using System.Linq;
using CapiGenerator.CSModel;

namespace WebgpuBindgen;


public static class MissingCommentFinder
{
    public static async Task WriteMissingComments(TranslationResult translationResult, Stream outputStream)
    {
        using var writer = new StreamWriter(outputStream, new UTF8Encoding(false), 4096, true);
        foreach (var item in translationResult.Structs)
        {
            var hasSummery = (item.Comments?.Summary?.Description?.Length ?? 0) > 0;

            if (!hasSummery)
            {
                await writer.WriteLineAsync(item.GetFullName());
            }

            foreach (var field in item.Fields)
            {
                var hasFieldSummery = (field.Comments?.Summary?.Description?.Length ?? 0) > 0;
                if (!hasFieldSummery && field.AccessModifier == CSAccessModifier.Public)
                {
                    await writer.WriteLineAsync($"{field.GetFullName()}");
                }
            }

            foreach (var method in item.Methods)
            {
                var hasMethodSummery = (method.Comments?.Summary?.Description?.Length ?? 0) > 0;
                if (!hasMethodSummery && method.AccessModifier == CSAccessModifier.Public)
                {
                    await writer.WriteLineAsync($"{item.GetFullName()}");
                }
                foreach (var param in method.Parameters)
                {
                    var hasParamSummery = (method.Comments?.Parameters.FirstOrDefault(i => i.Name == param.Name)?.Description?.Length ?? 0) > 0;
                    if (!hasParamSummery)
                    {
                        await writer.WriteLineAsync($"{method.GetFullName()}.{param.Name}");
                    }
                }
            }
        }

        foreach (var item in translationResult.StaticClasses)
        {
            var hasSummery = (item.Comments?.Summary?.Description?.Length ?? 0) > 0;

            if (!hasSummery)
            {
                await writer.WriteLineAsync(item.GetFullName());
            }

            foreach (var field in item.Fields)
            {
                var hasFieldSummery = (field.Comments?.Summary?.Description?.Length ?? 0) > 0;
                if (!hasFieldSummery && field.AccessModifier == CSAccessModifier.Public)
                {
                    await writer.WriteLineAsync($"{field.GetFullName()}");
                }
            }

            foreach (var method in item.Methods)
            {
                var hasMethodSummery = (method.Comments?.Summary?.Description?.Length ?? 0) > 0;
                if (!hasMethodSummery && method.AccessModifier == CSAccessModifier.Public)
                {
                    await writer.WriteLineAsync($"{item.GetFullName()}");
                }

                foreach (var param in method.Parameters)
                {
                    var hasParamSummery = (method.Comments?.Parameters.FirstOrDefault(i => i.Name == param.Name)?.Description?.Length ?? 0) > 0;
                    if (!hasParamSummery)
                    {
                        await writer.WriteLineAsync($"{method.GetFullName()}.{param.Name}");
                    }
                }
            }
        }

        foreach (var item in translationResult.Enums)
        {
            var hasSummery = (item.Comments?.Summary?.Description?.Length ?? 0) > 0;

            if (!hasSummery)
            {
                await writer.WriteLineAsync(item.GetFullName());
            }
            
            foreach (var value in item.Values)
            {
                var hasValueSummery = (value.Comments?.Summary?.Description?.Length ?? 0) > 0;
                if (!hasValueSummery)
                {
                    await writer.WriteLineAsync($"{value.GetFullName()}");
                }
            }
        }
    }
}