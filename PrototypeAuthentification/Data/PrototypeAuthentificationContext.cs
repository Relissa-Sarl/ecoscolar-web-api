using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PrototypeAuthentification.Models;

namespace PrototypeAuthentification.Data
{
    public class PrototypeAuthentificationContext : DbContext
    {
        public PrototypeAuthentificationContext (DbContextOptions<PrototypeAuthentificationContext> options)
            : base(options)
        {
        }

        public DbSet<User> User { get; set; } = default!;
    }
}
