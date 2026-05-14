using System.ComponentModel.DataAnnotations.Schema;

namespace EcoscolarWebApi.Models
{
    [Table("PhysicalItems")]
    public class PhysicalItems : Adverts
    {
        public string Condition { get; set; }
    }
}
