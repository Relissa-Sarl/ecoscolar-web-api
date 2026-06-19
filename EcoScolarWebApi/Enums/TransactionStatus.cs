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

    // Tutoring: package paid, hours credited, funds held in escrow until the student
    // confirms, the tutor-declared delay elapses, or the package expires.
    // Appended last on purpose: Status is persisted as int, so existing values must keep their ordinals.
    [EnumMember(Value = "PAID_WAITING_COMPLETION")]
    [JsonPropertyName("PAID_WAITING_COMPLETION")]
    PAID_WAITING_COMPLETION,

    // Tutoring: package paid but awaiting the tutor's accept/refuse decision (UC-09 E6-02).
    // Refusal or acceptance timeout -> CANCELLED + refund. Acceptance -> PAID_WAITING_COMPLETION.
    [EnumMember(Value = "PAID_WAITING_ACCEPTANCE")]
    [JsonPropertyName("PAID_WAITING_ACCEPTANCE")]
    PAID_WAITING_ACCEPTANCE
}
