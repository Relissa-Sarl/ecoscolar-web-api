using EcoscolarWebApi.Utils.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoscolarWebApi.Models
{
    [Table("PhysicalItems")]
    public class PhysicalItems : Adverts
    {
        public Condition Condition { get; set; }
        public float? Weight { get; set; }
        public virtual ICollection<Pictures> Pictures { get; set; } = new List<Pictures>();
    }
}
