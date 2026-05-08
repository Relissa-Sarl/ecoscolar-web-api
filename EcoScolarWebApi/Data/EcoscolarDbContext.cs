using EcoscolarWebApi.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EcoscolarWebApi.Data
{
    public class EcoscolarDbContext : IdentityDbContext<User>
    {
        public EcoscolarDbContext(DbContextOptions<EcoscolarDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> User { get; set; } = default!;
    }
}
