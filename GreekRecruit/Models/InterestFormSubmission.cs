using System.ComponentModel.DataAnnotations;

namespace GreekRecruit.Models
{
    public class InterestFormSubmission
    {
        [Key]
        public int submission_id { get; set; }

        [Required]
        public int form_id { get; set; }

        [Required]
        public int organization_id { get; set; }

        [Required]
        public string pnm_fname { get; set; }

        [Required]
        public string pnm_lname { get; set; }

        [Required]
        [EmailAddress]
        public string pnm_email { get; set; }

        [Required]
        [Phone]
        public string pnm_phone { get; set; }

        [Required]
        public string pnm_schoolyear { get; set; }

        public string? pnm_major { get; set; }

        public double? pnm_gpa { get; set; }

        public string? pnm_instagramhandle { get; set; }

        public string? pnm_membersknown { get; set; }

        public byte[]? pnm_profilepicture { get; set; }

        public DateTime date_submitted { get; set; } = DateTime.Now;

        public string? pnm_profilepictureurl { get; set; }
    }
}
