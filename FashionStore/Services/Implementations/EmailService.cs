// File: EmailService.cs
using System.Net;
using System.Net.Mail;
namespace FashionStore.Services.Implementations;

public class EmailService : IEmailService
{
    private readonly IConfiguration _cfg;
    private readonly ILogger<EmailService> _log;
    public EmailService(IConfiguration cfg, ILogger<EmailService> log) { _cfg = cfg; _log = log; }

    public async Task<bool> SendAsync(string to, string subject, string htmlBody)
    {
        var host = _cfg["Smtp:Host"];
        if (string.IsNullOrWhiteSpace(host))
        {
            _log.LogWarning("SMTP chưa cấu hình, không gửi mail thật. To={To} | Subject={Subject}\n{Body}", to, subject, htmlBody);
            return false;
        }
        try
        {
            var user = _cfg["Smtp:User"];
            using var client = new SmtpClient(host, int.Parse(_cfg["Smtp:Port"] ?? "587"))
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(user, _cfg["Smtp:Password"])
            };
            using var msg = new MailMessage
            {
                From = new MailAddress(string.IsNullOrWhiteSpace(_cfg["Smtp:From"]) ? user! : _cfg["Smtp:From"]!, "FashionStore"),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            msg.To.Add(to);
            await client.SendMailAsync(msg);
            return true;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Gửi mail tới {To} thất bại", to);
            return false;
        }
    }
}