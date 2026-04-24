using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace PrototypeAuthentification.Models
{
    public class User : IdentityUser
    {
        public string Name { get; set; }
    }
}
