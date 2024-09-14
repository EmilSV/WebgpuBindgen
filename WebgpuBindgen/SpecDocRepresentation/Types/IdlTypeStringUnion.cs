using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WebgpuBindgen.SpecDocRepresentation.Types;

[JsonConverter(typeof(IdlTypeConverter))]
public readonly struct IdlTypeStringUnion
{
    private readonly object? _value = null;

    public IdlTypeStringUnion(string? value)
    {
        _value = value;
    }

    public IdlTypeStringUnion(IdlTypeBase[]? value)
    {
        _value = value;
    }

    public IdlTypeStringUnion(IdlTypeBase? value)
    {
        _value = value;
    }

    public bool IsNull => _value == null;

    public bool TryToString([NotNullWhen(true)] out string? str)
    {
        if (_value is string v)
        {
            str = v;
            return true;
        }
        str = null;
        return false;
    }

    public bool TryToIdlType([NotNullWhen(true)] out IdlTypeBase? idlType)
    {
        if (_value is IdlTypeBase v)
        {
            idlType = v;
            return true;
        }
        idlType = null;
        return false;
    }

    public bool TryToIdlTypeArray([NotNullWhen(true)] out IdlTypeBase[]? idlTypeArray)
    {
        if (_value is IdlTypeBase[] v)
        {
            idlTypeArray = v;
            return true;
        }
        idlTypeArray = null;
        return false;
    }

    public static implicit operator IdlTypeStringUnion(string? value)
    {
        return new IdlTypeStringUnion(value);
    }

    public static implicit operator IdlTypeStringUnion(IdlTypeBase value)
    {
        return new IdlTypeStringUnion(value);
    }

    public static implicit operator IdlTypeStringUnion(IdlTypeBase[] value)
    {
        return new IdlTypeStringUnion(value);
    }
}

file class IdlTypeConverter : JsonConverter<IdlTypeStringUnion>
{
    public override IdlTypeStringUnion Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            return reader.GetString();
        }
        else if (reader.TokenType == JsonTokenType.StartArray)
        {
            var list = new List<IdlTypeBase>();
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                {
                    break;
                }
                list.Add(JsonSerializer.Deserialize<IdlTypeBase>(ref reader, options)!);
            }


            return list.ToArray();
        }
        else if (reader.TokenType == JsonTokenType.StartObject)
        {
            return JsonSerializer.Deserialize<IdlTypeBase>(ref reader, options)!;
        }
        throw new JsonException($"Unexpected token type: {reader.TokenType}");
    }

    public override void Write(Utf8JsonWriter writer, IdlTypeStringUnion value, JsonSerializerOptions options)
    {
        if (value.TryToString(out var v))
        {
            writer.WriteStringValue(v);
        }
        else if (value.TryToIdlTypeArray(out var array))
        {
            writer.WriteStartArray();
            foreach (var item in array)
            {
                JsonSerializer.Serialize(writer, item, options);
            }
            writer.WriteEndArray();
        }
        else if (value.TryToIdlType(out var idlType))
        {
            JsonSerializer.Serialize(writer, idlType, options);
        }
        else
        {
            throw new JsonException("Unexpected value type: " + value.GetType());
        }
    }
}