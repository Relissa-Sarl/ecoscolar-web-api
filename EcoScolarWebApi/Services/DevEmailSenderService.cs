using EcoScolarWebApi.Models;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Identity;
using MimeKit;

namespace EcoScolarWebApi.Services;

public class DevEmailSenderService : IEmailSender<User>
{
    private readonly IConfiguration _configuration;

    public DevEmailSenderService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    // Cette méthode est appelée par Identity pour le endpoint /forgotPassword
    public async Task SendPasswordResetLinkAsync(User user, string email, string resetLink)
    {
        var subject = "Réinitialisation de votre mot de passe - EcoScolar";
        var body = $"<p>Bonjour {user.UserName},</p>" +
                   $"<p>Pour réinitialiser votre mot de passe, veuillez cliquer sur le lien suivant :</p>" +
                   $"<p><a href='{resetLink}'>{resetLink}</a></p>";

        await SendEmailAsync(email, subject, body);
    }

    // Cette méthode est appelée pour la confirmation de compte (/register)
    public async Task SendConfirmationLinkAsync(User user, string email, string confirmationLink)
    {
        var subject = "Confirmez votre adresse e-mail - EcoScolar";
        var body = $"<p>Bienvenue sur EcoScolar !</p>" +
                   $"<p>Veuillez confirmer votre compte en cliquant <a href='{confirmationLink}'>ici</a>.</p>";

        await SendEmailAsync(email, subject, body);
    }

    // Cette méthode est appelée pour le code de double authentification (2FA)
    public async Task SendPasswordResetCodeAsync(User user, string email, string resetCode)
    {
        var subject = "Code de réinitialisation de mot de passe";
        var body = $"<p>Votre code de réinitialisation est : <strong>{resetCode}</strong></p>";

        await SendEmailAsync(email, subject, body);
    }

    // Facto de la logique d'envoi MailKit vers Mailpit
    private async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
    {
        // Récupération de la config (ou valeurs par défaut pour le dev local)
        var smtpHost = _configuration["Smtp:Host"] ?? "localhost";
        var smtpPort = _configuration.GetValue<int>("Smtp:Port", 1025);

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("EcoScolar", "no-reply@ecoscolar.local"));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder { HtmlBody = htmlMessage };
        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        try
        {
            // Connexion à Mailpit
            await client.ConnectAsync(smtpHost, smtpPort, MailKit.Security.SecureSocketOptions.None);
            await client.SendAsync(message);
        }
        catch (Exception ex)
        {
            // À adapter selon votre système de logs (ex: ILogger)
            Console.WriteLine($"[Mailpit Error] Impossible d'envoyer le mail : {ex.Message}");
        }
        finally
        {
            await client.DisconnectAsync(true);
        }
    }
}