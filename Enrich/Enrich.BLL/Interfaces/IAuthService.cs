using Enrich.BLL.Common;
using Enrich.BLL.DTOs;

namespace Enrich.BLL.Interfaces
{
    public interface IAuthService
    {
        Task<Result> RegisterUserAsync(UserSignupDTO dto);

        Task<Result> LoginAsync(LoginDTO dto);

        Task LogoutAsync();
    }
}