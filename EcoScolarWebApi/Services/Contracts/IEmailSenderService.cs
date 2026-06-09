using EcoScolarWebApi.Models;
using Microsoft.AspNetCore.Identity;

namespace EcoScolarWebApi.Services.Contracts;

public interface IEmailSenderService : IEmailSender<User>
{
    Task SendItemSoldEmailAsync(User seller, Advert advert);
}
