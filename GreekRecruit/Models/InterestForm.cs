using System.ComponentModel.DataAnnotations;

namespace GreekRecruit.Models
{
    public class InterestForm
    {
        [Key]
        public int form_id { get; set; }

        [Required]
        public int organization_id { get; set; }

        [Required]
        public string form_name { get; set; }

        public DateTime date_created { get; set; } = DateTime.Now;
    }
}
