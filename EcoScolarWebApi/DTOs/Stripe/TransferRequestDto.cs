namespace EcoScolarWebApi.DTOs.Stripe;

public class TransferRequestDto
{
    public long Amount { get; set; }
    public string TransferGroup { get; set; }
    public string ConnectedAccountId { get; set; }
}
