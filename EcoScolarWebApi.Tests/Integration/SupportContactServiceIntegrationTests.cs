using System.Security.Claims;
using EcoScolarWebApi.Commun;
using EcoScolarWebApi.Data;
using EcoScolarWebApi.DTOs.Support;
using EcoScolarWebApi.Models;
using EcoScolarWebApi.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace EcoScolarWebApi.Tests.Integration;

/// <summary>
/// Tests d'intégration SupportContactService (UC-02).
/// </summary>
public class SupportContactServiceIntegrationTests : IDisposable
{
    private readonly ServiceProvider _provider;
    private readonly EcoscolarDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly SupportContactService _service;

    public SupportContactServiceIntegrationTests()
    {
        _provider = IntegrationTestIdentityHelper.CreateIdentityProvider(out _context);
        _userManager = _provider.GetRequiredService<UserManager<User>>();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Support:DestinationEmail"] = "support@test.local"
            })
            .Build();

        _service = new SupportContactService(
            _context,
            _userManager,
            configuration,
            NullLogger<SupportContactService>.Instance);
    }

    [Fact]
    public async Task SubmitAsync_PersistsTicket_AndReturnsId()
    {
        var request = new SupportContactRequestDto
        {
            Email = "support.service@example.com",
            Subject = "Question compte",
            Message = "Je n'arrive plus à me connecter depuis hier."
        };

        var result = await _service.SubmitAsync(request, user: null);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Id.Should().BeGreaterThan(0);

        var stored = await _context.SupportTickets.SingleAsync(t => t.Id == result.Data.Id);
        stored.Email.Should().Be(request.Email);
        stored.Subject.Should().Be(request.Subject);
        stored.Message.Should().Be(request.Message);
        stored.UserId.Should().BeNull();

        var welcome = await _context.SupportTicketMessages
            .Where(m => m.TicketId == stored.Id)
            .ToListAsync();
        welcome.Should().ContainSingle(m => m.IsFromSupport);
    }

    [Fact]
    public async Task SubmitAsync_ReturnsInvalid_WhenMessageIsWhitespaceOnly()
    {
        var request = new SupportContactRequestDto
        {
            Email = "user@example.com",
            Subject = "Objet valide",
            Message = "          "
        };

        var result = await _service.SubmitAsync(request, user: null);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Conflict);
        result.Errors.Should().Contain("Veuillez saisir un message.");
        (await _context.SupportTickets.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task SubmitAsync_ReturnsInvalid_WhenSubjectIsWhitespaceOnly()
    {
        var request = new SupportContactRequestDto
        {
            Email = "user@example.com",
            Subject = "     ",
            Message = "Message de test suffisamment long."
        };

        var result = await _service.SubmitAsync(request, user: null);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Conflict);
        result.Errors.Should().Contain("Veuillez saisir l'objet du message.");
        (await _context.SupportTickets.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task SubmitAsync_AssociatesUserId_WhenAuthenticated()
    {
        var user = await CreateUserAsync("auth.support@example.com");
        var principal = CreatePrincipal(user.Id);
        var request = new SupportContactRequestDto
        {
            Email = "auth.support@example.com",
            Subject = "Suivi commande",
            Message = "Ma commande n'est toujours pas arrivée."
        };

        var result = await _service.SubmitAsync(request, principal);

        result.IsSuccess.Should().BeTrue();
        var stored = await _context.SupportTickets.SingleAsync(t => t.Id == result.Data!.Id);
        stored.UserId.Should().Be(user.Id);
    }

    [Fact]
    public async Task GetMyTicketsAsync_ReturnsUnauthorized_WhenPrincipalIsEmpty()
    {
        var result = await _service.GetMyTicketsAsync(new ClaimsPrincipal());

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Unauthorized);
    }

    [Fact]
    public async Task GetMyTicketsAsync_ReturnsTickets_OrderedByCreatedAtDesc()
    {
        var user = await CreateUserAsync("list.support@example.com");
        var principal = CreatePrincipal(user.Id);

        _context.SupportTickets.AddRange(
            new SupportTicket
            {
                Email = user.Email!,
                Subject = "Ancien",
                Message = "Premier message envoyé au support.",
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow.AddHours(-2)
            },
            new SupportTicket
            {
                Email = user.Email!,
                Subject = "Récent",
                Message = "Deuxième message envoyé au support.",
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow.AddMinutes(-5)
            });
        await _context.SaveChangesAsync();

        var result = await _service.GetMyTicketsAsync(principal);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(2);
        result.Data![0].Subject.Should().Be("Récent");
        result.Data[1].Subject.Should().Be("Ancien");
    }

    [Fact]
    public async Task GetMyTicketsAsync_MatchesTickets_ByEmail_WhenUserIdIsNull()
    {
        var user = await CreateUserAsync("email.match@example.com");
        var principal = CreatePrincipal(user.Id);

        _context.SupportTickets.Add(new SupportTicket
        {
            Email = user.Email!,
            Subject = "Sans userId",
            Message = "Ticket créé avant association utilisateur.",
            UserId = null,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var result = await _service.GetMyTicketsAsync(principal);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().ContainSingle(t => t.Subject == "Sans userId");
    }

    [Fact]
    public async Task AddTicketMessageAsync_PersistsUserReply()
    {
        var user = await CreateUserAsync("chat.support@example.com");
        var principal = CreatePrincipal(user.Id);
        var submit = await _service.SubmitAsync(new SupportContactRequestDto
        {
            Email = user.Email!,
            Subject = "Bug interface",
            Message = "Le bouton de validation ne s'affiche pas correctement."
        }, principal);

        var reply = await _service.AddTicketMessageAsync(
            principal,
            submit.Data!.Id,
            new SupportTicketMessageRequestDto { Message = "Voici une capture d'écran en pièce jointe." });

        reply.IsSuccess.Should().BeTrue();
        reply.Data!.IsFromSupport.Should().BeFalse();

        var messages = await _service.GetTicketMessagesAsync(principal, submit.Data.Id);
        messages.Data.Should().Contain(m => m.Body.Contains("capture"));
    }

    private async Task<User> CreateUserAsync(string email)
    {
        var user = new User { UserName = email, Email = email };
        (await _userManager.CreateAsync(user, "Password123!")).Succeeded.Should().BeTrue();
        return user;
    }

    private static ClaimsPrincipal CreatePrincipal(string userId) =>
        new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId)], "TestAuth"));

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        _provider.Dispose();
    }
}
