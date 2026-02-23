using System.Net;
using System.Net.Mail;

namespace TravelTripApi.Services;

/// <summary>
/// Email 發送服務
/// 用於發送密碼重置、驗證碼等郵件
/// </summary>
public class EmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// 發送密碼重置郵件
    /// </summary>
    /// <param name="toEmail">收件人 Email</param>
    /// <param name="resetToken">重置 Token</param>
    /// <param name="userName">使用者名稱</param>
    public async Task<bool> SendPasswordResetEmailAsync(string toEmail, string resetToken, string userName)
    {
        try
        {
            // 從設定檔讀取前端 URL（如果沒有設定則使用預設值）
            var frontendUrl = _configuration["Frontend:Url"] ?? "http://localhost:3000";
            var resetUrl = $"{frontendUrl}/reset-password?token={resetToken}";

            var subject = "TravelTrip - 密碼重置請求";
            var body = $@"
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #4CAF50; color: white; padding: 20px; text-align: center; }}
                        .content {{ padding: 20px; background-color: #f9f9f9; }}
                        .button {{ 
                            display: inline-block; 
                            padding: 12px 24px; 
                            background-color: #4CAF50; 
                            color: white; 
                            text-decoration: none; 
                            border-radius: 4px; 
                            margin: 20px 0;
                        }}
                        .footer {{ padding: 20px; text-align: center; font-size: 12px; color: #666; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>TravelTrip 密碼重置</h1>
                        </div>
                        <div class='content'>
                            <p>親愛的 {userName}，</p>
                            <p>我們收到了您的密碼重置請求。請點擊下方按鈕來重置您的密碼：</p>
                            <p style='text-align: center;'>
                                <a href='{resetUrl}' class='button'>重置密碼</a>
                            </p>
                            <p>或複製以下連結到瀏覽器：</p>
                            <p style='word-break: break-all; color: #666;'>{resetUrl}</p>
                            <p><strong>此連結將在 1 小時後過期。</strong></p>
                            <p>如果您沒有申請密碼重置，請忽略此郵件。</p>
                        </div>
                        <div class='footer'>
                            <p>© 2024 TravelTrip. All rights reserved.</p>
                        </div>
                    </div>
                </body>
                </html>
            ";

            await SendEmailAsync(toEmail, subject, body);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"發送密碼重置郵件失敗: {toEmail}");
            return false;
        }
    }

    /// <summary>
    /// 發送密碼變更成功通知
    /// </summary>
    public async Task<bool> SendPasswordChangedNotificationAsync(string toEmail, string userName)
    {
        try
        {
            var subject = "TravelTrip - 密碼已變更";
            var body = $@"
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #4CAF50; color: white; padding: 20px; text-align: center; }}
                        .content {{ padding: 20px; background-color: #f9f9f9; }}
                        .footer {{ padding: 20px; text-align: center; font-size: 12px; color: #666; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>密碼已成功變更</h1>
                        </div>
                        <div class='content'>
                            <p>親愛的 {userName}，</p>
                            <p>您的帳戶密碼已於 {DateTime.Now:yyyy-MM-dd HH:mm:ss} 成功變更。</p>
                            <p><strong>如果這不是您本人的操作，請立即聯繫我們的客服。</strong></p>
                        </div>
                        <div class='footer'>
                            <p>© 2024 TravelTrip. All rights reserved.</p>
                        </div>
                    </div>
                </body>
                </html>
            ";

            await SendEmailAsync(toEmail, subject, body);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"發送密碼變更通知失敗: {toEmail}");
            return false;
        }
    }

    /// <summary>
    /// 基礎 Email 發送方法
    /// </summary>
    private async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        // 從設定檔讀取 SMTP 設定
        var smtpHost = _configuration["Email:Smtp:Host"] ?? "smtp.gmail.com";
        var smtpPort = int.Parse(_configuration["Email:Smtp:Port"] ?? "587");
        var smtpUsername = _configuration["Email:Smtp:Username"] ?? "";
        var smtpPassword = _configuration["Email:Smtp:Password"] ?? "";
        var fromEmail = _configuration["Email:From"] ?? smtpUsername;
        var fromName = _configuration["Email:FromName"] ?? "TravelTrip";

        // 如果沒有設定 SMTP，則只記錄日誌（開發環境）
        if (string.IsNullOrEmpty(smtpUsername) || string.IsNullOrEmpty(smtpPassword))
        {
            _logger.LogWarning($"SMTP 未設定，郵件未實際發送");
            _logger.LogInformation($"[模擬發送郵件] To: {toEmail}, Subject: {subject}");
            _logger.LogInformation($"[郵件內容]\n{htmlBody}");
            return;
        }

        using var smtpClient = new SmtpClient(smtpHost, smtpPort)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(smtpUsername, smtpPassword)
        };

        var mailMessage = new MailMessage
        {
            From = new MailAddress(fromEmail, fromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };

        mailMessage.To.Add(toEmail);

        await smtpClient.SendMailAsync(mailMessage);
        _logger.LogInformation($"郵件發送成功: {toEmail}");
    }
}
