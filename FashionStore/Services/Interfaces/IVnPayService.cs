namespace FashionStore.Services.Interfaces;

public class VnPayResult
{
    public bool Success { get; set; }
    public string TxnRef { get; set; } = "";
    public long Amount { get; set; }          // đơn vị VND (đã chia 100)
    public string ResponseCode { get; set; } = "";
    public string TransactionNo { get; set; } = "";
}

public interface IVnPayService
{
    bool IsConfigured { get; }
    string CreatePaymentUrl(string orderCode, decimal amount, string ipAddress, string returnUrl);
    VnPayResult? ParseReturn(IQueryCollection query);   // null nếu sai chữ ký
}