using System.Security.Claims;
using Enrich.BLL.Common;
using Enrich.BLL.DTOs;
using Enrich.BLL.Interfaces;
using Enrich.DAL.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Enrich.BLL.Services
{
    public class UserService(
        UserManager<User> userManager,
        ILogger<UserService> logger) : IUserService
    {
        public async Task<Result> UpdateProfileAsync(string userId, UpdateProfileDTO profileDto)
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                logger.LogWarning("Спроба оновити неіснуючого користувача з ID: {UserId}", userId);
                return Result.Failure("Користувача не знайдено.");
            }

            user.Bio = profileDto.Bio;
            user.UpdatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);

            if (user.UserName != profileDto.Username)
            {
                var setUsernameResult = await userManager.SetUserNameAsync(user, profileDto.Username);

                if (!setUsernameResult.Succeeded)
                {
                    logger.LogError(
                        "Не вдалося змінити Username для {UserId}: {Errors}",
                        userId,
                        string.Join(", ", setUsernameResult.Errors.Select(e => e.Description)));

                    return Result.Failure(string.Join(", ", setUsernameResult.Errors.Select(e => e.Description)));
                }

                await userManager.UpdateNormalizedUserNameAsync(user);
            }

            var updateResult = await userManager.UpdateAsync(user);

            if (updateResult.Succeeded)
            {
                logger.LogInformation("Профіль користувача {UserId} успішно оновлено.", userId);
                return Result.Success();
            }

            logger.LogError(
                "Помилка при фінальному збереженні користувача {UserId}: {Errors}",
                userId,
                string.Join(", ", updateResult.Errors.Select(e => e.Description)));

            return Result.Failure(string.Join(", ", updateResult.Errors.Select(e => e.Description)));
        }

        public async Task<IEnumerable<UserDTO>> GetAllUsersAsync()
        {
            var users = await userManager.Users.ToListAsync();

            return [.. users.Select(u => new UserDTO
            {
                Id = u.Id,
                Username = u.UserName ?? string.Empty,
                Email = u.Email ?? string.Empty,
                IsLockedOut = u.LockoutEnd.HasValue && u.LockoutEnd > DateTimeOffset.UtcNow
            })];
        }

        public async Task<Result> RestrictUserAsync(RestrictAccountDTO dto)
        {
            var user = await userManager.FindByIdAsync(dto.UserId);
            if (user == null)
            {
                logger.LogWarning("Спроба заблокувати неіснуючого користувача з ID: {UserId}", dto.UserId);
                return Result.Failure("Користувача не знайдено.");
            }

            var lockoutEndDate = DateTimeOffset.UtcNow.AddDays(dto.LockoutDays);
            var result = await userManager.SetLockoutEndDateAsync(user, lockoutEndDate);

            if (result.Succeeded)
            {
                await userManager.UpdateSecurityStampAsync(user);

                logger.LogInformation(
                    "Користувача {UserId} успішно заблоковано до {LockoutEndDate}. Причина: {Reason}",
                    dto.UserId,
                    lockoutEndDate,
                    dto.Reason);

                return Result.Success();
            }

            logger.LogError(
                "Помилка при блокуванні користувача {UserId}: {Errors}",
                dto.UserId,
                string.Join(", ", result.Errors.Select(e => e.Description)));

            return Result.Failure(string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        public async Task<Result> RestoreUserAsync(RestoreAccountDTO dto)
        {
            var user = await userManager.FindByIdAsync(dto.UserId);
            if (user == null)
            {
                logger.LogWarning("Спроба розблокувати неіснуючого користувача з ID: {UserId}", dto.UserId);
                return Result.Failure("Користувача не знайдено.");
            }

            var result = await userManager.SetLockoutEndDateAsync(user, null);

            if (result.Succeeded)
            {
                await userManager.UpdateSecurityStampAsync(user);

                logger.LogInformation("Акаунт користувача {UserId} успішно розблоковано.", dto.UserId);
                return Result.Success();
            }

            logger.LogError(
                "Помилка при розблокуванні користувача {UserId}: {Errors}",
                dto.UserId,
                string.Join(", ", result.Errors.Select(e => e.Description)));

            return Result.Failure(string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        public async Task<UserDTO?> GetCurrentUserProfileAsync(ClaimsPrincipal userPrincipal)
        {
            var user = await userManager.GetUserAsync(userPrincipal);
            if (user == null)
            {
                return null;
            }

            return new UserDTO
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                Username = user.UserName ?? string.Empty,
                Bio = user.Bio ?? string.Empty,
            };
        }

        public string? GetCurrentUserId(ClaimsPrincipal userPrincipal)
        {
            return userManager.GetUserId(userPrincipal);
        }
    }
}