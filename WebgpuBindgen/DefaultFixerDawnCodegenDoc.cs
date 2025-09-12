using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using CapiGenerator.CSModel;
using CapiGenerator.CSModel.ConstantToken;
using WebgpuBindgen.DawnCodegenJson;

namespace WebgpuBindgen;

public static class DefaultFixerDawnCodegenDoc
{
    public static void Fix(
        List<CSStruct> structs, List<CSStaticClass> staticClasses, List<CSEnum> enums, DawnCodegenDoc doc)
    {
        foreach (var item in structs)
        {
            var dawnCodegenName = DawnCodegenDoc.ToDawnCodegenName(item.Name);
            var dawnStruct = doc.Items.TryGetValue(dawnCodegenName, out var structDoc) ? structDoc : null;
            if (dawnStruct is not WebgpuBindgen.DawnCodegenJson.Structure structure || (!structure.Tags.IsDefaultOrEmpty && structure.Tags.Contains(Tag.Dawn)))
            {
                continue;
            }

            foreach (var field in item.Fields)
            {
                var fieldDawnName = DawnCodegenDoc.ToDawnCodegenName(field.Name);
                var dawnField = structure.Members.FirstOrDefault(i => i.Name == fieldDawnName);
                if (dawnField is null)
                {
                    continue;
                }

                if (dawnField.Default is not null && field.DefaultValue.Value is null)
                {
                    var newDefaultValue = ToCSDefaultValue(dawnField.Default, field, staticClasses);
                    if (newDefaultValue is not null)
                    {
                        field.DefaultValue = newDefaultValue.Value;
                    }
                }
            }
        }
    }

    private static CSDefaultValue? ToCSDefaultValue(JsonValue jsonValue, CSField field, List<CSStaticClass> cSStaticClasses)
    {
        switch (jsonValue.GetValueKind())
        {
            case JsonValueKind.String:
                {
                    var strValue = ((string?)jsonValue) ?? throw new InvalidOperationException("String value is null");
                    bool? boolValue = strValue.ToLower() switch
                    {
                        "true" => true,
                        "false" => false,
                        _ => null
                    };

                    if (boolValue is not null)
                    {
                        return new CSDefaultValue(boolValue.Value);
                    }

                    var intValue = long.TryParse(strValue, out var intParsed) ? intParsed : (long?)null;
                    if (intValue is null)
                    {
                        if (strValue.StartsWith("0x"))
                        {
                            intValue = long.TryParse(strValue[2..], NumberStyles.HexNumber, null, out var hexParsed) ? hexParsed : (long?)null;
                        }
                    }

                    if (intValue is not null)
                    {
                        return new CSDefaultValue(intValue.Value);
                    }

                    double? doubleValue = double.TryParse(strValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var doubleParsed) ? doubleParsed : (double?)null;
                    if (doubleValue is null)
                    {
                        if (strValue.EndsWith("f", StringComparison.InvariantCultureIgnoreCase))
                        {
                            doubleValue = double.TryParse(strValue[..^1], NumberStyles.Float, CultureInfo.InvariantCulture, out var floatParsed) ? floatParsed : (double?)null;
                        }
                    }
                    if (doubleValue is not null)
                    {
                        return new CSDefaultValue(doubleValue.Value);
                    }

                    var memberName = NameConvertor.ConvertName(strValue, NameStyle.DawnCodegen, NameStyle.CSharp);
                    var constMemberName = NameConvertor.ConvertName(strValue, NameStyle.DawnCodegen, NameStyle.CSharpConstant);
                    if (field.Type.Type is CSEnum enumType)
                    {
                        var enumMember = enumType.Values.FirstOrDefault(m => m.Name == memberName || m.Name == constMemberName);
                        if (enumMember is not null)
                        {
                            return new CSDefaultValue(new CSConstantExpression([new CSConstIdentifierToken(enumMember, false)]));
                        }
                    }

                    foreach (var staticClass in cSStaticClasses)
                    {
                        var constField = staticClass.Fields.FirstOrDefault(f => f.Name == memberName || f.Name == constMemberName);
                        if (constField is not null)
                        {
                            return new CSDefaultValue(new CSConstantExpression([new CSConstIdentifierToken(constField, false)]));
                        }
                    }
                    if (strValue.Trim().ToLower() is "null" or "nullptr")
                    {
                        return new CSDefaultValue(new CSConstantExpression([CSNullToken.Instance]));
                    }

                    // Special case for "zero" to map to default
                    if (strValue.Equals("zero", StringComparison.InvariantCultureIgnoreCase) && field.Type.Type is CSStruct)
                    {
                         return new CSDefaultValue(new CSConstantExpression([CSDefaultToken.Instance]));
                    }

                    throw new NotSupportedException($"Unsupported default string value: {strValue}");
                }
            case JsonValueKind.Number:
                {
                    double doubleValue = (double)jsonValue;
                    int? intValue = Math.Abs(doubleValue % 1) <= (Double.Epsilon * 100) ? (int?)Convert.ToInt32(doubleValue) : null;

                    if (intValue is not null)
                    {
                        return new CSDefaultValue(intValue.Value);
                    }
                    else
                    {
                        return new CSDefaultValue(doubleValue);
                    }
                }
            case JsonValueKind.True:
                return new CSDefaultValue(true);
            case JsonValueKind.False:
                return new CSDefaultValue(false);
            default:
                throw new NotSupportedException($"Unsupported JSON value kind: {jsonValue.GetValueKind()}");
        }
    }
}