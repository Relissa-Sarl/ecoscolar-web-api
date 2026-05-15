using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace EcoscolarWebApi.Utils.Enums
{
    /// <summary>
    /// Enumeration that represents the condition of an item.
    /// It can be NEW, LIKE NEW or USED.
    /// </summary>
    public enum Condition
    {
        [EnumMember(Value = "NEW")]
        [JsonPropertyName("NEW")]
        NEW,
        [EnumMember(Value = "LIKE NEW")]
        [JsonPropertyName("LIKE NEW")]
        LIKE_NEW,
        [EnumMember(Value = "USED")]
        [JsonPropertyName("USED")]
        USED
    }
}
