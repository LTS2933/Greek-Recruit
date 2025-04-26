using System.ComponentModel.DataAnnotations;
namespace GreekRecruit.Models
{
    public class Organization
    {
        [Key]
        public int organization_id { get; set; }

        public string organization_name { get; set; }

        public string? smtp_server { get; set; }
        public int? smtp_port { get; set; }
        public string? smtp_username { get; set; }
        public string? smtp_password { get; set; }
    }
}
