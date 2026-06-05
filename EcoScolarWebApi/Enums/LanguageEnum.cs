using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace EcoScolarWebApi.Enums;

/// <summary>
/// Enumeration that represents the language of an PhysicalItem.
/// It can be FR, DE or IT.
/// </summary>
public enum LanguageEnum
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
