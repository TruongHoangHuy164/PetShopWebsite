using Microsoft.AspNetCore.Identity.UI.Services;

public class EmailSender : IEmailSender
{
    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        Console.WriteLine($"Gửi email đến: {email}\nTiêu đề: {subject}\nNội dung: {htmlMessage}");
        return Task.CompletedTask;
    }
}
