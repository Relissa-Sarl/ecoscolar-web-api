using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PrototypePurchasingProcess.Models;

namespace PrototypePurchasingProcess.Data
{
    public class PrototypePurchasingProcessContext : DbContext
    {
        public PrototypePurchasingProcessContext (DbContextOptions<PrototypePurchasingProcessContext> options)
            : base(options)
        {
        }

        public DbSet<PrototypePurchasingProcess.Models.Advert> Advert { get; set; } = default!;
    }
}
