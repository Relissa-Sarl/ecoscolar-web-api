using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace EcoScolarWebApi.Enums
{
    /// <summary>
    /// Enumeration that represents the language of an Adverts.
    /// It can be FR, DE or IT.
    /// </summary>
    public enum Language
    {
        [EnumMember(Value = "FR")]
        [JsonPropertyName("FR")]
        FR,
        [EnumMember(Value = "DE")]
        [JsonPropertyName("DE")]
        DE,
        [EnumMember(Value = "IT")]
        [JsonPropertyName("IT")]
        IT
    }
}
