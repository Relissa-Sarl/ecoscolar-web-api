namespace EcoScolarWebApi.Enums;

public enum TransactionStatus
{
    /// <summary>
    /// Transaction initiated but payment is pending
    /// </summary>
    PENDING_PAYMENT,

    /// <summary>
    /// Payment failed or was rejected
    /// </summary>
    PAYMENT_FAILED,

    /// <summary>
    /// Payment successful, waiting for the seller to ship the item
    /// </summary>
    PAID_WAITING_SHIPPING,

    /// <summary>
    /// Item shipped by the seller, waiting for the buyer to receive it
    /// </summary>
    SHIPPED,

    /// <summary>
    /// Transaction fully completed (item received, funds transferred to seller)
    /// </summary>
    COMPLETED,

    /// <summary>
    /// Transaction cancelled (e.g. by seller, or payment timeout)
    /// </summary>
    CANCELLED,

    /// <summary>
    /// Buyer raised a dispute
    /// </summary>
    DISPUTED
}
