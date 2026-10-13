namespace FashionStore.Models.Entities;
public class Role
{
    public int Id { get; set; }
    [Required, StringLength(50)] public string Name { get; set; } = string.Empty;
    public ICollection<AppUser> Users { get; set; } = new List<AppUser>();
}