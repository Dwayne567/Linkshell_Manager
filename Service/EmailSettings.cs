namespace LinkshellManager.Services
{
    // Settings for email service
    public class EmailSettings
    {
        public string SmtpUsername { get; set; }
        public string SmtpPassword { get; set; }
        public string SmtpHost { get; set; }
        public int SmtpPort { get; set; }
        public string FromEmail { get; set; }
        public string FromName { get; set; }
    }
}