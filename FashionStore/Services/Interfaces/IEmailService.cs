namespace FashionStore.Services.Interfaces;
public interface IEmailService
{
    /// true = đã gửi thật; false = chưa cấu hình SMTP hoặc gửi lỗi
    Task<bool> SendAsync(string to, string subject, string htmlBody);
}