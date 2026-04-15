using Enrich.BLL.DTOs;
using Enrich.BLL.Interfaces;
using Enrich.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Enrich.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CategoryController(
        ICategoryService categoryService,
        ILogger<CategoryController> logger) : BaseController
    {
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var categories = await categoryService.GetAllCategoriesAsync();
            logger.LogInformation("Адміністратор {UserId} переглянув список категорій.", CurrentUserId);
            return View(categories);
        }

        [HttpGet]
        public IActionResult Create() => View(new CategoryViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoryViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var result = await categoryService.CreateCategoryAsync(new CategoryDTO { Name = model.Name });

            if (!result.IsSuccess)
            {
                ModelState.AddModelError("", result.ErrorMessage!);
                return View(model);
            }

            TempData["SuccessMessage"] = "Category created successfully!";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var category = await categoryService.GetCategoryByIdAsync(id);
            if (category == null) return NotFound();

            return View(new CategoryViewModel { Id = category.Id, Name = category.Name });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CategoryViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var result = await categoryService.UpdateCategoryAsync(new CategoryDTO { Id = model.Id, Name = model.Name });

            if (!result.IsSuccess)
            {
                ModelState.AddModelError("", result.ErrorMessage!);
                return View(model);
            }

            TempData["SuccessMessage"] = "Category updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await categoryService.DeleteCategoryAsync(id);
            if (!result.IsSuccess) TempData["ErrorMessage"] = result.ErrorMessage;
            else TempData["SuccessMessage"] = "Category deleted successfully!";

            return RedirectToAction(nameof(Index));
        }
    }
}