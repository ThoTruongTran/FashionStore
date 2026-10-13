namespace FashionStore.Services.Interfaces;
public interface IPasswordResetService
{
    string CreateToken(AppUser user);
    Task<AppUser?> ValidateAsync(string token);
}