using Enrich.BLL.DTOs;
using Enrich.BLL.Interfaces;
using Enrich.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Enrich.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController(
        IUserService userService,
        ILogger<AdminController> logger) : Controller
    {
        [HttpGet]
        public async Task<IActionResult> Browse()
        {
            logger.LogInformation("Адміністратор переглядає список усіх користувачів.");

            var usersDto = await userService.GetAllUsersAsync();
            var model = usersDto.Select(u => new UserListViewModel
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                Role = "User",
                IsLockedOut = u.IsLockedOut
            }).ToList();

            return View(model);
        }

        [HttpGet]
        public IActionResult Restrict(string id, string username)
        {
            var model = new RestrictAccountViewModel
            {
                UserId = id,
                Username = username
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restrict(RestrictAccountViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var dto = new RestrictAccountDTO
            {
                UserId = model.UserId,
                Reason = model.Reason
            };

            var result = await userService.RestrictUserAsync(dto);

            if (result.IsSuccess)
            {
                logger.LogInformation("Адміністратор заблокував акаунт {UserId}", model.UserId);
                TempData["SuccessMessage"] = $"Акаунт користувача {model.Username} успішно заблоковано.";
                return RedirectToAction("Browse");
            }

            ModelState.AddModelError(string.Empty, result.ErrorMessage!);

            return View(model);
        }

        [HttpGet]
        public IActionResult Restore(string id, string username)
        {
            var model = new RestoreAccountViewModel
            {
                UserId = id,
                Username = username
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(RestoreAccountViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var dto = new RestoreAccountDTO
            {
                UserId = model.UserId
            };

            var result = await userService.RestoreUserAsync(dto);

            if (result.IsSuccess)
            {
                logger.LogInformation("Адміністратор розблокував акаунт {UserId}", model.UserId);
                TempData["SuccessMessage"] = $"Акаунт користувача {model.Username} успішно розблоковано.";
                return RedirectToAction("Browse");
            }

            ModelState.AddModelError(string.Empty, result.ErrorMessage!);

            return View(model);
        }
    }
}