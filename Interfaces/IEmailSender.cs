using System.Threading.Tasks;

namespace LinkshellManager.Interfaces
{
    // Interface for email sender service
    public interface IEmailSender
    {
        Task SendEmailAsync(string email, string subject, string message);
    }
}