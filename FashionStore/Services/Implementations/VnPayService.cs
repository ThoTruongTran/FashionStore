// File: VnPayService.cs
using System.Net;
using System.Security.Cryptography;
using System.Text;
namespace FashionStore.Services.Implementations;

public class VnPayService : IVnPayService
{
    private readonly IConfiguration _cfg;
    public VnPayService(IConfiguration cfg) => _cfg = cfg;

    private string TmnCode => _cfg["VnPay:TmnCode"] ?? "";
    private string Secret => _cfg["VnPay:HashSecret"] ?? "";
    public bool IsConfigured => !string.IsNullOrWhiteSpace(TmnCode) && !string.IsNullOrWhiteSpace(Secret);

    public string CreatePaymentUrl(string orderCode, decimal amount, string ipAddress, string returnUrl)
    {
        var now = DateTime.Now;
        var txnRef = orderCode + now.ToString("HHmmss");     // 6 ký tự cuối dùng để tách lại mã đơn
        var p = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["vnp_Version"] = "2.1.0",
            ["vnp_Command"] = "pay",
            ["vnp_TmnCode"] = TmnCode,
            ["vnp_Amount"] = ((long)(amount * 100)).ToString(),
            ["vnp_CreateDate"] = now.ToString("yyyyMMddHHmmss"),
            ["vnp_ExpireDate"] = now.AddMinutes(15).ToString("yyyyMMddHHmmss"),
            ["vnp_CurrCode"] = "VND",
            ["vnp_IpAddr"] = ipAddress,
            ["vnp_Locale"] = "vn",
            ["vnp_OrderInfo"] = $"Thanh toan don hang {orderCode}",
            ["vnp_OrderType"] = "other",
            ["vnp_ReturnUrl"] = returnUrl,
            ["vnp_TxnRef"] = txnRef
        };
        var query = Build(p);
        return $"{_cfg["VnPay:BaseUrl"]}?{query}&vnp_SecureHash={Sign(query)}";
    }

    public VnPayResult? ParseReturn(IQueryCollection query)
    {
        var p = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var kv in query)
            if (kv.Key.StartsWith("vnp_") && kv.Key != "vnp_SecureHash" && kv.Key != "vnp_SecureHashType")
                p[kv.Key] = kv.Value.ToString();

        var received = query["vnp_SecureHash"].ToString();
        if (string.IsNullOrEmpty(received) || !string.Equals(received, Sign(Build(p)), StringComparison.OrdinalIgnoreCase))
            return null;

        var code = p.GetValueOrDefault("vnp_ResponseCode", "");
        var status = p.GetValueOrDefault("vnp_TransactionStatus", "00");
        long.TryParse(p.GetValueOrDefault("vnp_Amount", "0"), out var amount);
        return new VnPayResult
        {
            Success = code == "00" && status == "00",
            ResponseCode = code,
            TxnRef = p.GetValueOrDefault("vnp_TxnRef", ""),
            TransactionNo = p.GetValueOrDefault("vnp_TransactionNo", ""),
            Amount = amount / 100
        };
    }

    private static string Build(SortedDictionary<string, string> p) =>
        string.Join("&", p.Where(kv => !string.IsNullOrEmpty(kv.Value))
            .Select(kv => $"{WebUtility.UrlEncode(kv.Key)}={WebUtility.UrlEncode(kv.Value)}"));

    private string Sign(string data)
    {
        using var h = new HMACSHA512(Encoding.UTF8.GetBytes(Secret));
        return Convert.ToHexString(h.ComputeHash(Encoding.UTF8.GetBytes(data))).ToLower();
    }
}