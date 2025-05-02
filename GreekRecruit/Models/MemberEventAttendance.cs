using System;
using System.ComponentModel.DataAnnotations;

namespace GreekRecruit.Models
{
    public class MemberEventAttendance
    {
        [Key]
        public int MemberEventAttendanceId { get; set; }
        public int EventId { get; set; }

        public int UserId { get; set; }

        public DateTime CheckedInAt { get; set; }
    }
}
