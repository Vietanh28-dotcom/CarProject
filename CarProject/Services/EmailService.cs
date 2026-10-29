using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace CarProject.Services;

public class EmailService : IEmailService
{
    private readonly SmtpSettings _smtp;

    public EmailService(IOptions<SmtpSettings> smtp)
    {
        _smtp = smtp.Value;
    }

    public async Task<bool> GuiEmailXacNhan(string email, string token, string callbackUrl)
    {
        try
        {
            using var client = new SmtpClient(_smtp.Host, _smtp.Port)
            {
                Credentials = new NetworkCredential(_smtp.UserName, _smtp.Password),
                EnableSsl = _smtp.EnableSsl
            };
            var link = $"{callbackUrl}?token={WebUtility.UrlEncode(token)}&email={WebUtility.UrlEncode(email)}";
            var mail = new MailMessage
            {
                From = new MailAddress(_smtp.From),
                Subject = "Xác nhận đăng ký tài khoản - Premium Car Showroom",
                Body = $"""
                <html>
                <body style="font-family:'Inter',sans-serif;background:#111;color:#fff;padding:40px">
                <div style="max-width:480px;margin:auto;border:1px solid rgba(255,255,255,0.08);padding:32px">
                <h2 style="font-family:'Playfair Display',serif;letter-spacing:2px;">Premium Car Showroom</h2>
                <p>Chào bạn,</p>
                <p>Vui lòng nhấn nút bên dưới để xác nhận đăng ký tài khoản:</p>
                <a href="{link}" style="display:inline-block;padding:12px 32px;background:#fff;color:#000;text-decoration:none;font-weight:600;letter-spacing:1px;border-radius:4px;">Xác nhận đăng ký</a>
                <p style="margin-top:24px;font-size:0.85rem;color:rgba(255,255,255,0.4);">Nếu bạn không yêu cầu đăng ký, hãy bỏ qua email này.</p>
                </div>
                </body>
                </html>
                """,
                IsBodyHtml = true
            };
            mail.To.Add(email);
            await client.SendMailAsync(mail);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

public class SmtpSettings
{
    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public string UserName { get; set; } = "";
    public string Password { get; set; } = "";
    public string From { get; set; } = "";
    public bool EnableSsl { get; set; } = true;
}
