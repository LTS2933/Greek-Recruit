using System.ComponentModel.DataAnnotations;

namespace GreekRecruit.Models;

public class Interview
{
    [Key]
    public int interview_id { get; set; }

    public int organization_id { get; set; }

    public int pnm_id { get; set; }

    public int? interviewer_user_id { get; set; }

    public DateTime interview_datetime { get; set; }

    public string? location { get; set; }

    public string? notes { get; set; }

    public bool is_completed { get; set; } = false;
}
