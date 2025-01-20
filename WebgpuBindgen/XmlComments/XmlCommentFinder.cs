using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using CapiGenerator.CSModel;

namespace WebgpuBindgen.XmlComments;

public static partial class XmlCommentFinder
{
    public static ICommendableItem? FindComments(TranslationResult translationResult, string location)
    {
        location = RemoveWhitespace(location);
        foreach (var csEnum in translationResult.Enums)
        {
            var fullName = RemoveWhitespace(csEnum.GetFullName());
            if (fullName == location)
            {
                return csEnum;
            }

            foreach (var field in csEnum.Values)
            {
                var fullNameField = RemoveWhitespace(field.GetFullName());
                if (fullNameField == location)
                {
                    return field;
                }
            }
        }

        foreach (var csStruct in translationResult.Structs)
        {
            {
                var typeFullName = RemoveWhitespace(csStruct.GetFullName());
                if (typeFullName == location)
                {
                    return csStruct;
                }
            }

            foreach (var field in csStruct.Fields)
            {
                var fullNameField = RemoveWhitespace(field.GetFullName());
                if (fullNameField == location)
                {
                    return field;
                }
            }

            foreach (var method in csStruct.Methods)
            {
                var fullName = RemoveWhitespace(method.GetFullName());
                var fullNameWithParameters = RemoveWhitespace(method.GetFullNameWithParameters());
                if (fullName == location)
                {
                    return method;
                }
                else if (fullNameWithParameters == location)
                {
                    return method;
                }
            }

            foreach (var constructor in csStruct.Constructors)
            {
                var fullName = RemoveWhitespace(constructor.GetFullName());
                var fullNameWithParameters = RemoveWhitespace(constructor.GetFullNameWithParameters());
                if (fullName == location)
                {
                    return constructor;
                }
                else if (fullNameWithParameters == location)
                {
                    return constructor;
                }
            }
        }

        foreach (var staticClass in translationResult.StaticClasses)
        {
            {
                var typeFullName = RemoveWhitespace(staticClass.GetFullName());
                if (typeFullName == location)
                {
                    return staticClass;
                }
            }

            foreach (var field in staticClass.Fields)
            {
                var fullName = RemoveWhitespace(field.GetFullName());
                if (fullName == location)
                {
                    return field;
                }
            }

            foreach (var method in staticClass.Methods)
            {
                var fullName = RemoveWhitespace(method.GetFullName());
                var fullNameWithParameters = RemoveWhitespace(method.GetFullNameWithParameters());
                if (fullName == location)
                {
                    return method;
                }
                else if (fullNameWithParameters == location)
                {
                    return method;
                }
            }
        }

        return null;
    }

    [return: NotNullIfNotNull("str")]
    private static string? RemoveWhitespace(string? str)
    {
        if (str == null)
        {
            return str;
        }

        var RegexWhitespace = WhiteSpaceRegex();
        return RegexWhitespace.Replace(str, " ");
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhiteSpaceRegex();
}