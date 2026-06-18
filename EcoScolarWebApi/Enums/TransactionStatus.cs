using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace EcoScolarWebApi.Enums;

public enum TransactionStatus
{
    [EnumMember(Value = "PENDING")]
    [JsonPropertyName("PENDING")]
    PENDING,
    
    [EnumMember(Value = "PAID_WAITING_SHIPPING")]
    [JsonPropertyName("PAID_WAITING_SHIPPING")]
    PAID_WAITING_SHIPPING,

    [EnumMember(Value = "SHIPPED")]
    [JsonPropertyName("SHIPPED")]
    SHIPPED,

    [EnumMember(Value = "COMPLETED")]
    [JsonPropertyName("COMPLETED")]
    COMPLETED,

    [EnumMember(Value = "CANCELLED")]
    [JsonPropertyName("CANCELLED")]
    CANCELLED,

    [EnumMember(Value = "DISPUTED")]
    [JsonPropertyName("DISPUTED")]
    DISPUTED,

    [EnumMember(Value = "PAID_WAITING_ACCEPTANCE")]
    [JsonPropertyName("PAID_WAITING_ACCEPTANCE")]
    PAID_WAITING_ACCEPTANCE,

    [EnumMember(Value = "PAID_WAITING_COMPLETION")]
    [JsonPropertyName("PAID_WAITING_COMPLETION")]
    PAID_WAITING_COMPLETION
}
