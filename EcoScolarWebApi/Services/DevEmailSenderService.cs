using EcoScolarWebApi.Models;
using Microsoft.AspNetCore.Identity;

namespace EcoScolarWebApi.Services;

public class DevEmailSenderService : IEmailSender<User>
{
	private readonly ILogger<DevEmailSenderService> _logger;

	// On injecte le système de logs de .NET
	public DevEmailSenderService(ILogger<DevEmailSenderService> logger)
	{
		_logger = logger;
	}

	public Task SendConfirmationLinkAsync(User user, string email, string confirmationLink)
	{
		return Task.CompletedTask;
	}

	public Task SendPasswordResetLinkAsync(User user, string email, string resetLink)
	{
		return Task.CompletedTask;
	}

	public Task SendPasswordResetCodeAsync(User user, string email, string resetCode)
	{
		// _logger.LogWarning s'affiche en JAUNE fluo dans la console, impossible de le rater !
		_logger.LogWarning("=============================================");
		_logger.LogWarning("🔔 SIMULATION EMAIL POUR : {Email}", email);
		_logger.LogWarning("🔑 CODE DE RÉINITIALISATION : {Code}", resetCode);
		_logger.LogWarning("=============================================");

		return Task.CompletedTask;
	}
}
