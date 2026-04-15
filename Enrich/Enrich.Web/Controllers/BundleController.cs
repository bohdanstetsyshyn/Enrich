using Enrich.BLL.DTOs;
using Enrich.BLL.Interfaces;
using Enrich.BLL.Settings;
using Enrich.DAL.Entities.Enums;
using Enrich.DAL.Interfaces;
using Enrich.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Enrich.Web.Controllers
{
    [Authorize]
    public class BundleController(
        ILogger<BundleController> logger,
        IBundleService bundleService,
        ICategoryRepository categoryRepository,
        IWordRepository wordRepository,
        IOptions<PaginationSettings> paginationOptions) : BaseController
    {
        [HttpGet]
        public async Task<IActionResult> Index(
            string? search = null,
            string? categoryFilter = null,
            string? levelFilter = null,
            int? minWordCount = null,
            int? maxWordCount = null,
            int page = 1,
            int pageSize = 0)
        {
            if (pageSize <= 0)
            {
                pageSize = paginationOptions.Value.DefaultUserBundlesPageSize;
            }

            var pagedBundles = await bundleService.GetUserBundlesPageAsync(
                CurrentUserId,
                search,
                categoryFilter,
                levelFilter,
                minWordCount,
                maxWordCount,
                page,
                pageSize);

            logger.LogInformation(
                "Користувач {UserId} переглянув сторінку {Page} своїх бандлів. Пошук: '{Search}', Категорії: '{Categories}', Рівні: '{Levels}', Слів: {MinWords}-{MaxWords}",
                CurrentUserId,
                page,
                search ?? "(немає)",
                categoryFilter ?? "(немає)",
                levelFilter ?? "(немає)",
                minWordCount ?? 0,
                maxWordCount ?? 0);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_MyBundleListPartial", pagedBundles);
            }

            var categories = await categoryRepository.GetAllCategoriesAsync();

            var viewModel = new BundleIndexViewModel
            {
                Bundles = pagedBundles,
                SearchTerm = search,
                LevelFilter = levelFilter,
                MinWordCount = minWordCount,
                MaxWordCount = maxWordCount,
                Categories = categories,
                PageSize = pageSize
            };

            return View(viewModel);
        }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var bundle = await bundleService.GetBundleByIdAsync(id);

        if (bundle == null)
        {
            logger.LogWarning("Користувач {UserId} намагався переглянути неіснуючу колекцію {BundleId}.", CurrentUserId, id);
            return NotFound();
        }

        if (!bundle.IsSystem && bundle.OwnerId != CurrentUserId && !bundle.IsPublic)
        {
            logger.LogWarning("Доступ заборонено: Користувач {UserId} намагався переглянути приватну колекцію {BundleId}.", CurrentUserId, id);
            return Forbid();
        }

        logger.LogInformation("Користувач {UserId} переглядає вміст колекції {BundleId} (\"{Title}\").", CurrentUserId, id, bundle.Title);
        
        return View(bundle);
    }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var categories = await categoryRepository.GetAllCategoriesAsync();
            var words = await wordRepository.GetAllWordsAsync();
            var availableLevels = new List<string> { "A1", "A2", "B1", "B2", "C1", "C2" };

            var viewModel = new CreateBundleViewModel
            {
                Categories = categories.Select(c => (c.Id, c.Name)).ToList(),
                Words = words.Select(w => new WordItemViewModel { Id = w.Id, Term = w.Term, Translation = w.Translation }).ToList(),
                AvailableLevels = availableLevels
            };

            logger.LogInformation("User {UserId} opened create bundle form.", CurrentUserId);
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateBundleViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                logger.LogWarning(
                    "Validation failed for create bundle form by user {UserId}. Errors: {Errors}",
                    CurrentUserId,
                    errors);

                var categories = await categoryRepository.GetAllCategoriesAsync();
                var words = await wordRepository.GetAllWordsAsync();
                var availableLevels = new List<string> { "A1", "A2", "B1", "B2", "C1", "C2" };

                model.Categories = categories.Select(c => (c.Id, c.Name)).ToList();
                model.Words = words.Select(w => new WordItemViewModel { Id = w.Id, Term = w.Term, Translation = w.Translation }).ToList();
                model.AvailableLevels = availableLevels;

                return View(model);
            }

            var dto = new CreateBundleDTO
            {
                Title = model.Title.Trim(),
                Description = model.Description?.Trim(),
                DifficultyLevels = model.DifficultyLevels?.Any() == true ? model.DifficultyLevels.ToArray() : [],
                ImageUrl = model.ImageUrl,
                CategoryIds = model.CategoryIds?.Any() == true ? model.CategoryIds : null,
                WordIds = model.WordIds?.Any() == true ? model.WordIds : null
            };

            var result = await bundleService.CreateBundleAsync(CurrentUserId, dto);

            if (!result.IsSuccess)
            {
                logger.LogWarning(
                    "Error creating bundle '{Title}' for user {UserId}: {Error}",
                    model.Title,
                    CurrentUserId,
                    result.ErrorMessage);

                ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Failed to create bundle.");

                var categories = await categoryRepository.GetAllCategoriesAsync();
                var words = await wordRepository.GetAllWordsAsync();
                var availableLevels = new List<string> { "A1", "A2", "B1", "B2", "C1", "C2" };

                model.Categories = categories.Select(c => (c.Id, c.Name)).ToList();
                model.Words = words.Select(w => new WordItemViewModel { Id = w.Id, Term = w.Term, Translation = w.Translation }).ToList();
                model.AvailableLevels = availableLevels;

                return View(model);
            }

            logger.LogInformation(
                "Bundle '{Title}' successfully created by user {UserId}.",
                model.Title,
                CurrentUserId);

            TempData["SuccessMessage"] = $"Bundle '{model.Title}' successfully created!";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var bundle = await bundleService.GetBundleByIdAsync(id);

            if (bundle == null)
            {
                logger.LogWarning(
                    "User {UserId} attempted to edit non-existent bundle {BundleId}.",
                    CurrentUserId,
                    id);

                return NotFound();
            }

            if (bundle.OwnerId != CurrentUserId)
            {
                logger.LogWarning(
                    "User {UserId} attempted to edit someone else's bundle {BundleId}.",
                    CurrentUserId,
                    id);

                return Forbid();
            }

            var viewModel = new EditBundleViewModel
            {
                Id = bundle.Id,
                Title = bundle.Title,
                Description = bundle.Description,
                ImageUrl = bundle.ImageUrl,
                Status = Enum.Parse<BundleStatus>(bundle.Status, ignoreCase: true),
                CategoryIds = bundle.CategoryIds,
                WordIds = bundle.WordIds,
                DifficultyLevels = bundle.DifficultyLevels?.ToList() ?? []
            };

            var categories = await categoryRepository.GetAllCategoriesAsync();
            var words = await wordRepository.GetAllWordsAsync();
            var availableLevels = new List<string> { "A1", "A2", "B1", "B2", "C1", "C2" };

            viewModel.Categories = categories.Select(c => (c.Id, c.Name)).ToList();
            viewModel.Words = words.Select(w => new WordItemViewModel { Id = w.Id, Term = w.Term, Translation = w.Translation }).ToList();
            viewModel.AvailableLevels = availableLevels;

            logger.LogInformation(
                "User {UserId} opened edit form for bundle {BundleId}.",
                CurrentUserId,
                id);

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditBundleViewModel model)
        {
            if (id != model.Id)
            {
                logger.LogWarning(
                    "ID mismatch when editing bundle for user {UserId}.",
                    CurrentUserId);

                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                logger.LogWarning(
                    "Validation failed for edit bundle form {BundleId} by user {UserId}. Errors: {Errors}",
                    id,
                    CurrentUserId,
                    errors);

                var categories = await categoryRepository.GetAllCategoriesAsync();
                var words = await wordRepository.GetAllWordsAsync();
                var availableLevels = new List<string> { "A1", "A2", "B1", "B2", "C1", "C2" };

                model.Categories = categories.Select(c => (c.Id, c.Name)).ToList();
                model.Words = words.Select(w => new WordItemViewModel { Id = w.Id, Term = w.Term, Translation = w.Translation }).ToList();
                model.AvailableLevels = availableLevels;

                return View(model);
            }

            var dto = new CreateBundleDTO
            {
                Title = model.Title.Trim(),
                Description = model.Description?.Trim(),
                DifficultyLevels = model.DifficultyLevels?.Any() == true ? model.DifficultyLevels.ToArray() : [],
                ImageUrl = model.ImageUrl,
                CategoryIds = model.CategoryIds?.Any() == true ? model.CategoryIds : null,
                WordIds = model.WordIds?.Any() == true ? model.WordIds : null
            };

            var result = await bundleService.UpdateBundleAsync(CurrentUserId, id, dto);

            if (!result.IsSuccess)
            {
                logger.LogWarning(
                    "Error editing bundle {BundleId} by user {UserId}: {Error}",
                    id,
                    CurrentUserId,
                    result.ErrorMessage);

                ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Failed to update bundle.");
                var categories = await categoryRepository.GetAllCategoriesAsync();
                var words = await wordRepository.GetAllWordsAsync();
                var availableLevels = new List<string> { "A1", "A2", "B1", "B2", "C1", "C2" };

                model.Categories = categories.Select(c => (c.Id, c.Name)).ToList();
                model.Words = words.Select(w => new WordItemViewModel { Id = w.Id, Term = w.Term, Translation = w.Translation }).ToList();
                model.AvailableLevels = availableLevels;
                return View(model);
            }

            logger.LogInformation(
                "Bundle {BundleId} successfully updated by user {UserId}.",
                id,
                CurrentUserId);

            TempData["SuccessMessage"] = "Bundle successfully updated!";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await bundleService.DeleteBundleAsync(CurrentUserId, id);

            if (!result.IsSuccess)
            {
                logger.LogWarning(
                    "Error deleting bundle {BundleId} by user {UserId}: {Error}",
                    id,
                    CurrentUserId,
                    result.ErrorMessage);

                TempData["ErrorMessage"] = result.ErrorMessage ?? "Failed to delete bundle.";
                return RedirectToAction("Index");
            }

            logger.LogInformation(
                "Bundle {BundleId} successfully deleted by user {UserId}.",
                id,
                CurrentUserId);

            TempData["SuccessMessage"] = "Bundle successfully deleted!";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> GetMyBundles(
            string? searchTerm,
            string? categoryFilter = null,
            string? levelFilter = null,
            int? minWordCount = null,
            int? maxWordCount = null,
            int page = 1,
            int pageSize = 0)
        {
            if (pageSize <= 0)
            {
                pageSize = paginationOptions.Value.DefaultUserBundlesPageSize;
            }

            var pageResult = await bundleService.GetUserBundlesPageAsync(
                CurrentUserId,
                searchTerm,
                categoryFilter,
                levelFilter,
                minWordCount,
                maxWordCount,
                page,
                pageSize);

            logger.LogInformation(
                "User {UserId} received {BundleCount} bundles (total {Total}).",
                CurrentUserId,
                pageResult.Items.Count(),
                pageResult.TotalCount);

            return Json(pageResult);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddWords(int bundleId, [FromBody] IEnumerable<int> wordIds)
        {
            if (!wordIds.Any())
            {
                logger.LogWarning(
                    "User {UserId} attempted to add an empty word list to bundle {BundleId}.",
                    CurrentUserId,
                    bundleId);

                return BadRequest("Please select at least one word.");
            }

            var result = await bundleService.AddWordsToBundleAsync(CurrentUserId, bundleId, wordIds);

            if (!result.IsSuccess)
            {
                logger.LogWarning(
                    "Error adding words to bundle {BundleId} by user {UserId}: {Error}",
                    bundleId,
                    CurrentUserId,
                    result.ErrorMessage);

                return BadRequest(result.ErrorMessage);
            }

            logger.LogInformation(
                "User {UserId} successfully added {Count} words to bundle {BundleId}.",
                CurrentUserId,
                wordIds.Count(),
                bundleId);

            return Ok("Words successfully added to bundle!");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveWords(int bundleId, [FromBody] IEnumerable<int> wordIds)
        {
            if (!wordIds.Any())
            {
                logger.LogWarning(
                    "User {UserId} attempted to remove an empty word list from bundle {BundleId}.",
                    CurrentUserId,
                    bundleId);

                return BadRequest("Please select at least one word.");
            }

            var result = await bundleService.RemoveWordsFromBundleAsync(CurrentUserId, bundleId, wordIds);

            if (!result.IsSuccess)
            {
                logger.LogWarning(
                    "Error removing words from bundle {BundleId} by user {UserId}: {Error}",
                    bundleId,
                    CurrentUserId,
                    result.ErrorMessage);

                return BadRequest(result.ErrorMessage);
            }

            logger.LogInformation(
                "User {UserId} successfully removed {Count} words from bundle {BundleId}.",
                CurrentUserId,
                wordIds.Count(),
                bundleId);

            return Ok("Words successfully removed from bundle!");
        }

        [HttpPost]
        public async Task<IActionResult> SaveSystemBundle(int id)
        {
            var result = await bundleService.SaveSystemBundleAsync(CurrentUserId, id);

            if (!result.IsSuccess)
            {
                logger.LogWarning("Failed attempt to save system bundle {BundleId} by user {UserId}: {Error}", id, CurrentUserId, result.ErrorMessage);
                return BadRequest(new { message = result.ErrorMessage });
            }

            return Ok();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitForReview(int id)
        {
            var result = await bundleService.SubmitBundleForReviewAsync(CurrentUserId, id);

            if (!result.IsSuccess)
            {
                logger.LogWarning(
                    "Error submitting bundle {BundleId} for review by user {UserId}: {Error}",
                    id,
                    CurrentUserId,
                    result.ErrorMessage);

                return BadRequest(new { message = result.ErrorMessage });
            }

            logger.LogInformation(
                "Bundle {BundleId} successfully submitted for review by user {UserId}.",
                id,
                CurrentUserId);

            return Ok(new { message = "Done! Your collection has been sent to moderators." });
        }
    }
}
