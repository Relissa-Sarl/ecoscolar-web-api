using System.Text.Json.Serialization;

namespace EcoScolarWebApi.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DisputeReason
{
    ItemNotReceived,
    NotAsDescribed,
    Damaged
}
