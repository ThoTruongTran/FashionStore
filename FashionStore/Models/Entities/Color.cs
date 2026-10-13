namespace FashionStore.Models.Entities;
public class Color
{
    public int Id { get; set; }
    [Required, StringLength(50)] public string Name { get; set; } = string.Empty;
    [StringLength(7)] public string HexCode { get; set; } = "#000000";
}