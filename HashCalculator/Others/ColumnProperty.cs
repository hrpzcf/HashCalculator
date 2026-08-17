using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Controls;

namespace HashCalculator;

public class ColumnProperty
{
    /// <summary>
    /// 将 DataGridLength 以字符串形式（如 "Auto"、"266.7"）序列化/反序列化，
    /// 与旧版 Newtonsoft.Json 输出的配置格式保持一致。
    /// </summary>
    private sealed class DataGridLengthJsonConverter : JsonConverter<DataGridLength>
    {
        private static readonly DataGridLengthConverter Converter = new();

        public override DataGridLength Read(ref Utf8JsonReader reader, Type typeToConvert,
            JsonSerializerOptions options)
        {
            string value = reader.GetString();
            return (DataGridLength)Converter.ConvertFromInvariantString(value);
        }

        public override void Write(Utf8JsonWriter writer, DataGridLength value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(Converter.ConvertToInvariantString(value));
        }
    }

    public int Index { get; set; }

    [JsonConverter(typeof(DataGridLengthJsonConverter))]
    public DataGridLength Width { get; set; }

    public ColumnProperty(int index, DataGridLength width)
    {
        this.Index = index;
        this.Width = width;
    }
}
