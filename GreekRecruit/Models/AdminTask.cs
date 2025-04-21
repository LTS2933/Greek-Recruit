using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace GreekRecruit.Models
{
    public class AdminTask
    {
        [Key]
        public int task_id { get; set; }

        public int user_id { get; set; }

        public int organization_id { get; set; }

        [Required]
        public string title { get; set; }

        public string? task_description { get; set; }

        public DateTime? due_date { get; set; }

        public bool is_completed { get; set; } = false;

        public DateTime date_created { get; set; } = DateTime.Now;

        public DateTime? date_completed { get; set; }

    }
}

