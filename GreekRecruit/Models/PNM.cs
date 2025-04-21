using System.ComponentModel.DataAnnotations;

namespace GreekRecruit.Models
{
    public class PNM
    {
        [Key]
        public int pnm_id { get; set; }

        public int organization_id { get; set; }
        public String pnm_fname { get; set; }
        public String pnm_lname { get; set; }
        public String? pnm_email { get; set; }
        public String? pnm_phone { get; set; }
        public Double? pnm_gpa { get; set; }
        public String? pnm_major { get; set; }

        public String? pnm_schoolyear { get; set; }

        public String? pnm_instagramhandle { get; set; }
        public byte[]? pnm_profilepicture { get; set; }

        public String? pnm_comments { get; set; }

        public String? pnm_status { get; set; } = "Pending";

        public string? pnm_semester { get; set; } //shouldn't be null but just in case

        public DateTime pnm_dateadded { get; set; } = DateTime.Now;

        public string? pnm_profilepictureurl { get; set; }

    }
}
