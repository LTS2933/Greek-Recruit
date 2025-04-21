using System.ComponentModel.DataAnnotations;
namespace GreekRecruit.Models
{
    public class Organization
    {
        [Key]
        public int organization_id { get; set; }

        public string organization_name { get; set; }
    }
}
