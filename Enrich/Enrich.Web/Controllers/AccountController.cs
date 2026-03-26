using Enrich.BLL.DTOs;
using Enrich.BLL.Interfaces;
using Enrich.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Enrich.Web.Controllers
{
    public class AccountController(
        ILogger<AccountController> logger,
        IAuthService authService,
        IUserService userService) : Controller
    {
        [HttpGet]
        public IActionResult Signup()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Settings");
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Signup(SignupViewModel model)
        {
            if (!ModelState.IsValid)
            {
                logger.LogWarning(
                    "Провалена валідація форми реєстрації для Username: {Username}, Email: {Email}.",
                    model.Username,
                    model.Email);

                return View(model);
            }

            var userDto = new UserSignupDTO
            {
                Email = model.Email,
                Username = model.Username,
                Password = model.Password,
            };

            var result = await authService.RegisterUserAsync(userDto);

            if (result.IsSuccess)
            {
                logger.LogInformation(
                    "Успішно зареєстровано нового користувача Username: {Username}, Email: {Email}",
                    model.Username,
                    model.Email);

                var loginDto = new LoginDTO
                {
                    Email = userDto.Email,
                    Password = userDto.Password,
                    RememberMe = true,
                };

                await authService.LoginAsync(loginDto);
                return RedirectToAction("Settings", "Account");
            }

            logger.LogWarning(
                "Помилка Identity при реєстрації Username: {Username}, Email: {Email}. Помилка: {ErrorMessage}",
                model.Username,
                model.Email,
                result.ErrorMessage);

            ModelState.AddModelError(string.Empty, result.ErrorMessage!);

            return View(model);
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Settings");
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                logger.LogWarning(
                    "Провалена валідація форми входу для Email: {Email}.",
                    model.Email);

                return View(model);
            }

            var loginDto = new LoginDTO
            {
                Email = model.Email,
                Password = model.Password,
                RememberMe = model.RememberMe
            };

            var result = await authService.LoginAsync(loginDto);

            if (result.IsSuccess)
            {
                logger.LogInformation("Користувач з Email: {Email} успішно увійшов.", model.Email);
                return RedirectToAction("Settings", "Account");
            }

            logger.LogWarning("Невдала спроба входу для Email: {Email}. Причина: {ErrorMessage}", model.Email, result.ErrorMessage);
            ModelState.AddModelError(string.Empty, result.ErrorMessage!);
            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var userId = userService.GetCurrentUserId(User);
            await authService.LogoutAsync();
            logger.LogInformation("Користувач {UserId} вийшов з системи.", userId);
            return RedirectToAction("Login", "Account");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Settings(string tab = "profile")
        {
            var profileDto = await userService.GetCurrentUserProfileAsync(User);

            if (profileDto == null)
            {
                logger.LogWarning("Спроба доступу до налаштувань неавторизованим користувачем.");
                return NotFound("Користувача не знайдено.");
            }

            var model = new UpdateProfileViewModel
            {
                Email = profileDto.Email,
                Username = profileDto.Username,
                Bio = profileDto.Bio
            };

            ViewBag.ActiveTab = tab;
            return View(model);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Settings(UpdateProfileViewModel model, string tab = "profile")
        {
            var profileDto = await userService.GetCurrentUserProfileAsync(User);
            if (profileDto == null)
            {
                return NotFound();
            }

            model.Email = profileDto.Email;

            if (!ModelState.IsValid)
            {
                ViewBag.ActiveTab = tab;
                return View(model);
            }

            var updateDto = new UpdateProfileDTO { Username = model.Username, Bio = model.Bio };
            var result = await userService.UpdateProfileAsync(profileDto.Id, updateDto);

            if (result.IsSuccess)
            {
                logger.LogInformation("Користувач {UserId} успішно оновив профіль.", profileDto.Id);
                TempData["SuccessMessage"] = "Your profile has been successfully updated.";
                return RedirectToAction("Settings", new { tab });
            }

            ModelState.AddModelError(string.Empty, result.ErrorMessage!);

            ViewBag.ActiveTab = tab;
            return View(model);
        }
    }
}