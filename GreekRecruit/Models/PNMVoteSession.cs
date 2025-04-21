using System.ComponentModel.DataAnnotations;

namespace GreekRecruit.Models;
public class PNMVoteSession
{
    [Key]
    public int vote_session_id { get; set; }

    public int pnm_id { get; set; }

    public DateTime session_open_dt { get; set; } = DateTime.Now;
    public DateTime? session_close_dt { get; set; } // null if still open

    public int yes_count { get; set; } = 0;
    public int no_count { get; set; } = 0;

    public bool voting_open_yn { get; set; } = true;
}

