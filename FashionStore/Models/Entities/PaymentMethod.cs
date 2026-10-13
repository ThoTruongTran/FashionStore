namespace FashionStore.Models.Entities;
public class PaymentMethod
{
    public int Id { get; set; }
    [Required, StringLength(100)] public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}