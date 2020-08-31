using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class Vector3Converter : JsonConverter<UnityEngine.Vector3>
{
    public override UnityEngine.Vector3 ReadJson(JsonReader reader, Type objectType, UnityEngine.Vector3 existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        JObject jobject = JObject.Load(reader);
        existingValue = new UnityEngine.Vector3(
            jobject.Value<float>("x"),
            jobject.Value<float>("y"),
            jobject.Value<float>("z"));
        return existingValue;
    }

    public override void WriteJson(JsonWriter writer, UnityEngine.Vector3 value, JsonSerializer serializer)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("x");
        writer.WriteValue(value.x);
        writer.WritePropertyName("y");
        writer.WriteValue(value.y);
        writer.WritePropertyName("z");
        writer.WriteValue(value.z);
        writer.WriteEndObject();
    }
}

public class QuaternionConverter : JsonConverter<UnityEngine.Quaternion>
{
    public override UnityEngine.Quaternion ReadJson(JsonReader reader, Type objectType, UnityEngine.Quaternion existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        JObject obj = JObject.Load(reader);

        existingValue = new UnityEngine.Quaternion(
            obj.Value<float>("x"),
            obj.Value<float>("y"),
            obj.Value<float>("z"),
            obj.Value<float>("w"));

        return existingValue;
    }

    public override void WriteJson(JsonWriter writer, UnityEngine.Quaternion value, JsonSerializer serializer)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("x");
        writer.WriteValue(value.x);
        writer.WritePropertyName("y");
        writer.WriteValue(value.y);
        writer.WritePropertyName("z");
        writer.WriteValue(value.z);
        writer.WritePropertyName("w");
        writer.WriteValue(value.w);
        writer.WriteEndObject();
    }
}

