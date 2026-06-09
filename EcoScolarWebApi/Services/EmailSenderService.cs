using EcoScolarWebApi.Models;
using EcoScolarWebApi.Services.Contracts;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Identity;
using MimeKit;

namespace EcoScolarWebApi.Services;

public class EmailSenderService : IEmailSenderService
{
    private readonly IConfiguration _configuration;

    public EmailSenderService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendPasswordResetLinkAsync(User user, string email, string resetLink)
    {
        var subject = "Réinitialisation de votre mot de passe - EcoScolar";
        var body = $"<p>Bonjour {user.UserName},</p>" +
                   $"<p>Pour réinitialiser votre mot de passe, veuillez cliquer sur le lien suivant :</p>" +
                   $"<p><a href='{resetLink}'>{resetLink}</a></p>";

        await SendEmailAsync(email, subject, body);
    }

    public async Task SendConfirmationLinkAsync(User user, string email, string confirmationLink)
    {
        var subject = "Confirmez votre adresse e-mail - EcoScolar";
        var body = $"<p>Bienvenue sur EcoScolar !</p>" +
                   $"<p>Veuillez confirmer votre compte en cliquant <a href='{confirmationLink}'>ici</a>.</p>";

        await SendEmailAsync(email, subject, body);
    }

    public async Task SendPasswordResetCodeAsync(User user, string email, string resetCode)
    {
        var subject = "Réinitialisation de votre mot de passe - EcoScolar";

        var encodedToken = Uri.EscapeDataString(resetCode);

        var frontendResetLink = $"http://localhost:3000/reset-password?email={Uri.EscapeDataString(email)}&token={encodedToken}";

        var body = $"""
        <div style="font-family: sans-serif; line-height: 1.5; color: #333;">
            <p>Bonjour {user.Nickname ?? user.UserName ?? ""},</p>
            <p>Nous avons reçu une demande de réinitialisation de mot de passe pour votre compte <strong>EcoScolar</strong>.</p>
            <p>Pour choisir un nouveau mot de passe, veuillez cliquer sur le bouton ci-dessous :</p>
            <p style="margin: 24px 0;">
                <a href="{frontendResetLink}" 
                   style="background-color: #007bff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px; display: inline-block; font-weight: bold;">
                   Réinitialiser mon mot de passe
                </a>
            </p>
            <p style="font-size: 12px; color: #666;">
                Si le bouton ne fonctionne pas, vous pouvez copier-coller ce lien dans votre navigateur :<br/>
                <a href="{frontendResetLink}">{frontendResetLink}</a>
            </p>
            <hr style="border: none; border-top: 1px solid #eee; margin: 20px 0;" />
            <p style="font-size: 12px; color: #999;">Si vous n'êtes pas à l'origine de cette demande, vous pouvez ignorer cet e-mail en toute sécurité.</p>
        </div>
        """;

        await SendEmailAsync(email, subject, body);
    }

    public async Task SendItemSoldEmailAsync(User seller, Advert advert)
    {
        var subject = "Votre article a été vendu ! - EcoScolar";
        var body = $"""
        <div style="font-family: sans-serif; line-height: 1.5; color: #333;">
            <p>Bonjour {seller.Nickname},</p>
            <p>Bonne nouvelle ! Votre annonce "<strong>{advert.Title}</strong>" a été achetée.</strong>.</p>
            <p>Nous vous remercions pour votre confiance sur notre plateforme.</p>
            <hr style="border: none; border-top: 1px solid #eee; margin: 20px 0;" />
        </div>
        """;

        await SendEmailAsync(seller.Email!, subject, body);
    }

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