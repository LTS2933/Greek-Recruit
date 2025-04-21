using System.ComponentModel.DataAnnotations;

namespace GreekRecruit.Models
{
    public class PNMVoteTracker
    {
        [Key]
        public int tracker_id { get; set; }

        [Required]
        public int vote_session_id { get; set; }

        [Required]
        public int user_id { get; set; }

        public DateTime vote_time { get; set; } = DateTime.Now;
    }
}
