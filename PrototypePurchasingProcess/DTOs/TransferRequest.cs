namespace PrototypePurchasingProcess.DTOs
{
    public class TransferRequest
    {
        public long Amount { get; set; }
        public string TransferGroup { get; set; }
        public string ConnectedAccountId { get; set; }
    }
}
