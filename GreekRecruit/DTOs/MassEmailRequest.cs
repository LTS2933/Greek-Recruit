namespace GreekRecruit.DTOs
{
    public class MassEmailRequest
    {
        public string Subject { get; set; }
        public string Message { get; set; }
        public List<string> Recipients { get; set; }
    }
}
