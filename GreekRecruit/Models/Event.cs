using System.ComponentModel.DataAnnotations;

namespace GreekRecruit.Models
{
    public class Event
    {
        [Key]
        public int event_id { get; set; }

        public string event_name { get; set; }
        public DateTime event_datetime { get; set; } = DateTime.Now
        .AddSeconds(-DateTime.Now.Second)
        .AddMilliseconds(-DateTime.Now.Millisecond);
        public string event_address { get; set; }
        public string? event_description { get; set; }

        public int organization_id { get; set; }

    }
}
