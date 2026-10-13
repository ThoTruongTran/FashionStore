namespace FashionStore.Models.ViewModels;
public class PaymentResultViewModel
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public string? OrderCode { get; set; }
}