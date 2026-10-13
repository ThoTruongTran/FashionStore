namespace FashionStore.Models.Entities;
public class Size
{
    public int Id { get; set; }
    [Required, StringLength(20)] public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}