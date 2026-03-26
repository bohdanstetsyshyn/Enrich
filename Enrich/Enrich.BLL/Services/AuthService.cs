using Enrich.BLL.Common;
using Enrich.BLL.DTOs;
using Enrich.BLL.Interfaces;
using Enrich.DAL.Entities;
using Microsoft.AspNetCore.Identity;

namespace Enrich.BLL.Services
{
    public class AuthService(
        UserManager<User> userManager,
        SignInManager<User> signInManager) : IAuthService
    {
        public async Task<Result> RegisterUserAsync(UserSignupDTO dto)
        {
            var user = new User
            {
                UserName = dto.Username,
                Email = dto.Email
            };

            var identityResult = await userManager.CreateAsync(user, dto.Password);

            if (identityResult.Succeeded)
            {
                return Result.Success();
            }

            var errorMessage = string.Join(", ", identityResult.Errors.Select(e => e.Description));
            return Result.Failure(errorMessage);
        }

        public async Task<Result> LoginAsync(LoginDTO dto)
        {
            var user = await userManager.FindByEmailAsync(dto.Email);
            if (user == null)
            {
                return Result.Failure("Невірний email або пароль.");
            }

            var signInResult = await signInManager.PasswordSignInAsync(
                user.UserName!,
                dto.Password,
                dto.RememberMe,
                lockoutOnFailure: false);

            if (signInResult.Succeeded)
            {
                return Result.Success();
            }

            if (signInResult.IsLockedOut)
            {
                return Result.Failure("Ваш акаунт заблоковано. Зверніться до адміністратора.");
            }

            return Result.Failure("Невірний email або пароль.");
        }

        public async Task LogoutAsync()
        {
            await signInManager.SignOutAsync();
        }
    }
}