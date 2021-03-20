using System.Threading.Tasks;
using Models;

namespace Sisbi.Services.Contracts
{
    public interface IEmailService
    {
        Task SendEmailAsync(EmailRequest emailRequest);
    }
}