using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PrototypeAuthentification.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace PrototypeAuthentification.Data
{
    public class PrototypeAuthentificationContext : IdentityDbContext<User>
    {
        public PrototypeAuthentificationContext (DbContextOptions<PrototypeAuthentificationContext> options)
            : base(options)
        {
        }

        public DbSet<User> User { get; set; } = default!;
    }
}
