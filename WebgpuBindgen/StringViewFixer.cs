using CapiGenerator.CSModel;
using CapiGenerator.CSModel.ConstantToken;
using static CapiGenerator.CSModel.CSClassMemberModifierConsts;

namespace WebgpuBindgen;

public static class StringViewFixer
{
    public static Task FixStringViewClassMembers(List<CSStruct> structs, List<CSStaticClass> staticClasses)
    {
        CSStruct stringViewStruct = structs.Where(i => i.Name.StartsWith("StringView", StringComparison.OrdinalIgnoreCase)).Single();

        if (stringViewStruct.Fields.Where(i => i.Name.StartsWith("NullValue", StringComparison.OrdinalIgnoreCase)).Any())
        {
            throw new Exception("StringView struct already has a field called NullValue");
        }

        var hasDataField = stringViewStruct.Fields.Any(i => i.Name.Equals("Data", StringComparison.InvariantCulture));
        var hasLengthField = stringViewStruct.Fields.Any(i => i.Name.Equals("Length", StringComparison.InvariantCulture));

        if (!hasDataField && !hasLengthField)
        {
            throw new Exception("StringView struct does not have a field called Data and a field called Length");
        }

        CSStaticClass webgpuConstantClass = staticClasses.Where(i => i.Name.StartsWith("WebGPU", StringComparison.OrdinalIgnoreCase)).Single();

        var strlenField = webgpuConstantClass.Fields.Where(i => i.Name.Equals("STRLEN", StringComparison.InvariantCulture)).Single();

        var stringViewTypeInstance = new CSTypeInstance(stringViewStruct);

        CSField NullValueField = new(PUBLIC | READONLY | STATIC, stringViewTypeInstance, "NullValue")
        {
            DefaultValue = new([new CSArbitraryCodeToken(
                $$"""
                new() { Data = null, Length = {{strlenField.GetFullName()}} }
                """
            )])
        };

        stringViewStruct.Fields.Add(NullValueField);

        static bool IsPointer(CSTypeInstance type)
        {
            var modifiers = type.GetModifiersAsSpan();
            foreach (var modifier in modifiers)
            {
                if (modifier == CsPointerType.Instance)
                {
                    return true;
                }
            }

            return false;
        }

        foreach (var item in structs)
        {
            var members = item.Fields.Where(
                    x =>
                    !x.IsRequired &&
                    x.GetterBody is null &&
                    x.SetterBody is null &&
                    !IsPointer(x.Type) &&
                    x.Type.Type == stringViewStruct);
            foreach (var member in members)
            {
                if (member.DefaultValue == CSDefaultValue.NullValue)
                {
                    member.DefaultValue = new([new CSConstIdentifierToken(NullValueField, false)]);
                }
            }
        }

        return Task.CompletedTask;
    }
}