using System.ComponentModel.DataAnnotations;

namespace GreekRecruit.Models
{
    public class PointsCategory
    {
        [Key]
        public int PointsCategoryID { get; set; }
        public string ActionName { get; set; }
        public int PointsValue { get; set; }
    }
}
