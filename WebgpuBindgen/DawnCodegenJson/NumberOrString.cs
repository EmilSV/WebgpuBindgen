using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace WebgpuBindgen.DawnCodegenJson;


[JsonConverter(typeof(NumberOrStringConverter))]
public readonly struct NumberOrString
{
    public enum Type
    {
        Undefined,
        Null,
        Number,
        Integer,
        String
    }
    private readonly Type _type;
    private readonly double _number;
    private readonly string? _string;

    public bool TryGetValue(out double number)
    {
        if (_type == Type.Number || _type == Type.Integer)
        {
            number = _number;
            return true;
        }
        number = default;
        return false;
    }

    public bool TryGetValue(out int number)
    {
        if (_type == Type.Integer)
        {
            number = (int)_number;
            return true;
        }
        number = default;
        return false;
    }

    public bool TryGetValue([NotNullWhen(true)] out string? str)
    {
        if (_type == Type.String)
        {
            str = _string!;
            return true;
        }
        str = default;
        return false;
    }

    public Type GetValueType() => _type;


    public NumberOrString(double number)
    {
        _type = Type.Number;
        _number = number;
        _string = null;
    }

    public NumberOrString(int number)
    {
        _type = Type.Integer;
        _number = number;
        _string = null;
    }

    public NumberOrString(string str)
    {
        _type = Type.String;
        _number = default;
        _string = str;
    }

    private NumberOrString(Type type)
    {
        _type = type;
        _number = default;
        _string = null;
    }

    public static NumberOrString Null => new(Type.Null);
    public static NumberOrString Undefined => new(Type.Undefined);
}