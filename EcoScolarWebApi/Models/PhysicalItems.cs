using EcoscolarWebApi.Utils.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoscolarWebApi.Models
{
    [Table("PhysicalItems")]
    public class PhysicalItems : Adverts
    {
        [Required]
        public Condition Condition { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Weight { get; set; }
        public virtual ICollection<Pictures> Pictures { get; set; } = new List<Pictures>();
    }
}
