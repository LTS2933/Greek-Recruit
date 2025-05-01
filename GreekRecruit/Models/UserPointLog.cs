using System.ComponentModel.DataAnnotations;

namespace GreekRecruit.Models
{
    public class UserPointLog
    {
        [Key]
        public int UserPointLogID { get; set; }
        public int UserID { get; set; }
        public int PointsCategoryID { get; set; }
        public int PointsAwarded { get; set; }
        public DateTime DateAwarded { get; set; }
    }

}
