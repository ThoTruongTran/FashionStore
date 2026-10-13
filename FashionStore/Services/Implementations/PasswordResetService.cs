// File: PasswordResetService.cs
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
namespace FashionStore.Services.Implementations;

// Token được mã hóa + có hạn 30 phút. Token gắn với "dấu vân tay" của mật khẩu hiện tại,
// nên sau khi đổi mật khẩu token cũ tự vô hiệu (chỉ dùng được 1 lần). Không cần thêm bảng.
public class PasswordResetService : IPasswordResetService
{
    private readonly ITimeLimitedDataProtector _protector;
    private readonly ApplicationDbContext _db;

    public PasswordResetService(IDataProtectionProvider provider, ApplicationDbContext db)
    {
        _protector = provider.CreateProtector("FashionStore.PasswordReset").ToTimeLimitedDataProtector();
        _db = db;
    }

    public string CreateToken(AppUser user) =>
        _protector.Protect($"{user.Id}|{Stamp(user)}", TimeSpan.FromMinutes(30));

    public async Task<AppUser?> ValidateAsync(string token)
    {
        try
        {
            var parts = _protector.Unprotect(token).Split('|');
            if (parts.Length != 2 || !int.TryParse(parts[0], out var id)) return null;
            var user = await _db.Users.FindAsync(id);
            return user != null && user.IsActive && Stamp(user) == parts[1] ? user : null;
        }
        catch (CryptographicException) { return null; }   // sai hoặc hết hạn
    }

    private static string Stamp(AppUser u) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(u.PasswordHash)))[..16];
}