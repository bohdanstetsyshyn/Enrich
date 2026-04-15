using Enrich.BLL.Common;
using Enrich.BLL.DTOs;
using Enrich.BLL.Interfaces;
using Enrich.BLL.Settings;
using Enrich.DAL.Entities;
using Enrich.DAL.Entities.Enums;
using Enrich.DAL.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Enrich.BLL.Services
{
    public class BundleService(
        IBundleRepository bundleRepository,
        ICategoryRepository categoryRepository,
        IOptions<PaginationSettings> paginationOptions,
        ILogger<BundleService> logger) : IBundleService
    {
        private readonly PaginationSettings _pagination = paginationOptions.Value;

        public async Task<Result> CreateBundleAsync(string userId, CreateBundleDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Title))
            {
                logger.LogWarning("Attempt to create a bundle with an empty title by user {UserId}.", userId);
                return "Bundle title cannot be empty.";
            }

            var titleLower = dto.Title.Trim().ToLowerInvariant();

            var duplicateExists = await bundleRepository.BundleTitleExistsForUserAsync(userId, titleLower);

            if (duplicateExists)
            {
                logger.LogWarning(
                    "User {UserId} attempted to create a duplicate bundle: '{Title}'.",
                    userId,
                    dto.Title);

                return $"You already have a bundle named '{dto.Title}'.";
            }

            var now = DateTime.UtcNow;

            var bundle = new Bundle
            {
                Title = dto.Title.Trim(),
                Description = dto.Description?.Trim(),
                DifficultyLevels = dto.DifficultyLevels ?? [],
                ImageUrl = dto.ImageUrl,
                Status = BundleStatus.Draft,
                IsSystem = false,
                IsPublic = false,
                OwnerId = userId,
                CreatedAt = now,
                UpdatedAt = now,
                Words = new List<Word>(),
                Categories = new List<Category>(),
                Tags = new List<Tag>()
            };

            try
            {
                var createdBundle = await bundleRepository.CreateBundleAsync(bundle);

                if (dto.WordIds != null && dto.WordIds.Any())
                {
                    await bundleRepository.AddWordsToBundleAsync(createdBundle.Id, dto.WordIds);
                }

                if (dto.CategoryIds != null && dto.CategoryIds.Any())
                {
                    await bundleRepository.AddCategoriesToBundleAsync(createdBundle.Id, dto.CategoryIds);
                }

                if (dto.TagIds != null && dto.TagIds.Any())
                {
                    await bundleRepository.AddTagsToBundleAsync(createdBundle.Id, dto.TagIds);
                }

                logger.LogInformation(
                    "Користувач {UserId} успішно створив новий бандл '{Title}' (ID: {BundleId}) зі статусом {Status}.",
                    userId,
                    createdBundle.Title,
                    createdBundle.Id,
                    createdBundle.Status);

                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Error creating bundle '{Title}' by user {UserId}.",
                    dto.Title,
                    userId);

                return "An error occurred while creating the bundle. Please try again.";
            }
        }

        public async Task<BundleDTO?> GetBundleByIdAsync(int bundleId)
        {
            var bundle = await bundleRepository.GetBundleByIdWithDetailsAsync(bundleId);

            if (bundle == null)
            {
                logger.LogWarning("Спроба отримати неіснуючий бандл з ID: {BundleId}.", bundleId);
                return null;
            }

            return MapBundleToDTO(bundle);
        }

        public async Task<IEnumerable<BundleDTO>> GetUserBundlesAsync(string userId)
        {
            logger.LogInformation("Отримання списку бандлів користувача {UserId}.", userId);

            var bundles = await bundleRepository.GetUserBundlesAsync(userId);

            var bundleDTOs = bundles.Select(MapBundleToDTO).ToList();

            logger.LogInformation(
                "Успішно отримано {Count} бандлів для користувача {UserId}.",
                bundleDTOs.Count,
                userId);

            return bundleDTOs;
        }

        public async Task<PagedResult<BundleDTO>> GetUserBundlesPageAsync(
            string userId,
            string? searchTerm,
            string? categoryFilter = null,
            string? difficultyLevel = null,
            int? minWordCount = null,
            int? maxWordCount = null,
            int page = 1,
            int pageSize = 0)
        {
            if (pageSize <= 0)
            {
                pageSize = _pagination.DefaultUserBundlesPageSize;
            }

            logger.LogInformation(
                "Отримання сторінки {Page} бандлів користувача {UserId} з пошуком '{SearchTerm}', категорії: '{Category}', рівні: '{Level}'.",
                page,
                userId,
                searchTerm ?? "(немає)",
                categoryFilter ?? "(немає)",
                difficultyLevel ?? "(немає)");

            var (bundles, total) = await bundleRepository.GetUserBundlesPageAsync(
                userId,
                searchTerm,
                categoryFilter,
                difficultyLevel,
                minWordCount,
                maxWordCount,
                page,
                pageSize);

            var bundleDTOs = bundles.Select(MapBundleToDTO).ToList();

            var result = new PagedResult<BundleDTO>
            {
                Items = bundleDTOs,
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };

            logger.LogInformation(
                "Успішно отримано {Count} бандлів (всього {Total}) для користувача {UserId}.",
                bundleDTOs.Count,
                total,
                userId);

            return result;
        }

        public async Task<PagedResult<SystemBundleDTO>> GetSystemBundlesAsync(
            string? searchTerm,
            string? category,
            string? difficultyLevel,
            int? minWordCount,
            int? maxWordCount,
            int page,
            int pageSize)
        {
            logger.LogInformation("Отримання сторінки системних бандлів: сторінка={Page}, розмір={PageSize}", page, pageSize);

            if (page <= 0)
            {
                page = 1;
            }

            if (pageSize <= 0)
            {
                pageSize = _pagination.DefaultSystemBundlesPageSize;
            }

            var (bundles, total) = await bundleRepository.GetSystemBundlesPageAsync(
                searchTerm,
                category,
                difficultyLevel,
                minWordCount,
                maxWordCount,
                page,
                pageSize);

            var items = bundles.Select(b => new SystemBundleDTO
            {
                Id = b.Id,
                Title = b.Title,
                Description = b.Description,
                ImageUrl = b.ImageUrl,
                WordCount = b.Words?.Count ?? 0,
                DifficultyLevels = b.DifficultyLevels ?? [],
                Categories = b.Categories?.Select(c => c.Name).ToList() ?? []
            }).ToList();

            return new PagedResult<SystemBundleDTO>
            {
                Items = items,
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<IEnumerable<Category>> GetAllCategoriesAsync()
        {
            return await categoryRepository.GetAllCategoriesAsync();
        }

        public async Task<Result> UpdateBundleAsync(string userId, int bundleId, CreateBundleDTO dto)
        {
            var bundle = await bundleRepository.GetBundleByIdAsync(bundleId);

            if (bundle == null)
            {
                logger.LogWarning(
                    "User {UserId} attempted to update non-existent bundle {BundleId}.",
                    userId,
                    bundleId);

                return "Bundle not found.";
            }

            if (bundle.OwnerId != userId)
            {
                logger.LogWarning(
                    "User {UserId} attempted to update someone else's bundle {BundleId} (owner: {OwnerId}).",
                    userId,
                    bundleId,
                    bundle.OwnerId);

                return "You do not have permission to update this bundle.";
            }

            if (bundle.Status != BundleStatus.Draft && bundle.Status != BundleStatus.Rejected)
            {
                logger.LogWarning(
                    "User {UserId} attempted to update bundle {BundleId} which is in status {Status}.",
                    userId,
                    bundleId,
                    bundle.Status);

                return "You cannot edit a bundle that is under review or published.";
            }

            if (!bundle.Title.Equals(dto.Title, StringComparison.OrdinalIgnoreCase))
            {
                var titleLower = dto.Title.Trim().ToLowerInvariant();
                var duplicateExists = await bundleRepository.BundleTitleExistsForUserAsync(userId, titleLower);

                if (duplicateExists)
                {
                    logger.LogWarning(
                        "User {UserId} attempted to update bundle {BundleId} with a duplicate title '{Title}'.",
                        userId,
                        bundleId,
                        dto.Title);

                    return $"You already have a bundle named '{dto.Title}'.";
                }

                bundle.Title = dto.Title.Trim();
            }

            bundle.Description = dto.Description?.Trim();
            bundle.DifficultyLevels = dto.DifficultyLevels ?? [];
            bundle.ImageUrl = dto.ImageUrl;
            bundle.UpdatedAt = DateTime.UtcNow;

            try
            {
                await bundleRepository.UpdateBundleAsync(bundle);
                await bundleRepository.SyncBundleRelationsAsync(bundle.Id, dto.WordIds, dto.CategoryIds);

                logger.LogInformation(
                    "Bundle {BundleId} of user {UserId} successfully updated.",
                    bundleId,
                    userId);

                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Error updating bundle {BundleId} by user {UserId}.",
                    bundleId,
                    userId);

                return "An error occurred while updating the bundle. Please try again.";
            }
        }

        public async Task<Result> DeleteBundleAsync(string userId, int bundleId)
        {
            var bundle = await bundleRepository.GetBundleByIdAsync(bundleId);

            if (bundle == null)
            {
                logger.LogWarning(
                    "User {UserId} attempted to delete non-existent bundle {BundleId}.",
                    userId,
                    bundleId);

                return "Bundle not found.";
            }

            if (bundle.OwnerId != userId)
            {
                logger.LogWarning(
                    "User {UserId} attempted to delete someone else's bundle {BundleId} (owner: {OwnerId}).",
                    userId,
                    bundleId,
                    bundle.OwnerId);

                return "You do not have permission to delete this bundle.";
            }

            if (bundle.Status != BundleStatus.Draft && bundle.Status != BundleStatus.Rejected)
            {
                logger.LogWarning(
                    "User {UserId} attempted to delete bundle {BundleId} which is in status {Status}.",
                    userId,
                    bundleId,
                    bundle.Status);

                return "You cannot delete a bundle that is under review or published.";
            }

            try
            {
                await bundleRepository.DeleteBundleAsync(bundleId);

                logger.LogInformation(
                    "Bundle {BundleId} for user {UserId} successfully deleted.",
                    bundleId,
                    userId);

                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Error deleting bundle {BundleId} by user {UserId}.",
                    bundleId,
                    userId);

                return "An error occurred while deleting the bundle. Please try again.";
            }
        }

        public async Task<Result> AddWordsToBundleAsync(string userId, int bundleId, IEnumerable<int> wordIds)
        {
            var bundle = await bundleRepository.GetBundleByIdAsync(bundleId);

            if (bundle == null)
            {
                logger.LogWarning(
                    "User {UserId} attempted to add words to non-existent bundle {BundleId}.",
                    userId,
                    bundleId);

                return "Bundle not found.";
            }

            if (bundle.OwnerId != userId)
            {
                logger.LogWarning(
                    "User {UserId} attempted to add words to someone else's bundle {BundleId}.",
                    userId,
                    bundleId);

                return "You do not have permission to modify this bundle.";
            }

            try
            {
                await bundleRepository.AddWordsToBundleAsync(bundleId, wordIds);

                logger.LogInformation(
                    "User {UserId} successfully added {Count} words to bundle {BundleId}.",
                    userId,
                    wordIds.Count(),
                    bundleId);

                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Error adding words to bundle {BundleId} by user {UserId}.",
                    bundleId,
                    userId);

                return "An error occurred while adding words. Please try again.";
            }
        }

        public async Task<Result> RemoveWordsFromBundleAsync(string userId, int bundleId, IEnumerable<int> wordIds)
        {
            var bundle = await bundleRepository.GetBundleByIdAsync(bundleId);

            if (bundle == null)
            {
                logger.LogWarning(
                    "User {UserId} attempted to remove words from non-existent bundle {BundleId}.",
                    userId,
                    bundleId);

                return "Bundle not found.";
            }

            if (bundle.OwnerId != userId)
            {
                logger.LogWarning(
                    "User {UserId} attempted to remove words from someone else's bundle {BundleId}.",
                    userId,
                    bundleId);

                return "You do not have permission to modify this bundle.";
            }

            try
            {
                await bundleRepository.RemoveWordsFromBundleAsync(bundleId, wordIds);

                logger.LogInformation(
                    "User {UserId} successfully removed {Count} words from bundle {BundleId}.",
                    userId,
                    wordIds.Count(),
                    bundleId);

                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Error removing words from bundle {BundleId} by user {UserId}.",
                    bundleId,
                    userId);

                return "An error occurred while removing words. Please try again.";
            }
        }

        private static BundleDTO MapBundleToDTO(Bundle bundle)
        {
            return new BundleDTO
            {
                Id = bundle.Id,
                Title = bundle.Title,
                Description = bundle.Description,
                DifficultyLevels = bundle.DifficultyLevels,
                ImageUrl = bundle.ImageUrl,
                Status = bundle.Status.ToString(),
                IsPublic = bundle.IsPublic,
                IsSystem = bundle.IsSystem,
                OwnerId = bundle.OwnerId,
                CreatedAt = bundle.CreatedAt,
                UpdatedAt = bundle.UpdatedAt,
                WordCount = bundle.Words?.Count ?? 0,
                CategoryCount = bundle.Categories?.Count ?? 0,
                TagCount = bundle.Tags?.Count ?? 0,
                Categories = bundle.Categories?.Select(c => c.Name).ToList() ?? [],
                CategoryIds = bundle.Categories?.Select(c => c.Id).ToList() ?? [],
                WordIds = bundle.Words?.Select(w => w.Id).ToList() ?? [],
                Words = bundle.Words?.Select(w => new WordItemViewModel 
                { 
                    Id = w.Id, 
                    Term = w.Term, 
                    Translation = w.Translation 
                }).ToList() ?? []
            };
        }

        public async Task<Result> SaveSystemBundleAsync(string userId, int bundleId)
        {
            var bundle = await bundleRepository.GetBundleByIdAsync(bundleId);

            if (bundle == null || !bundle.IsSystem)
            {
                logger.LogWarning("User {UserId} attempted to save non-existent or non-system bundle {BundleId}.", userId, bundleId);
                return "Collection not found or is not a system collection.";
            }

            var alreadySaved = await bundleRepository.UserHasBundleAsync(userId, bundleId);
            if (alreadySaved)
            {
                return "You have already saved this collection.";
            }

            var userBundle = new UserBundle
            {
                UserId = userId,
                BundleId = bundleId,
                SavedAt = DateTime.UtcNow
            };

            try
            {
                await bundleRepository.SaveUserBundleAsync(userBundle);
                logger.LogInformation("User {UserId} successfully saved system bundle {BundleId}.", userId, bundleId);
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error saving system bundle {BundleId} by user {UserId}.", bundleId, userId);
                return "An error occurred while saving the collection.";
            }
        }

        public async Task<Result> SubmitBundleForReviewAsync(string userId, int bundleId)
        {
            var bundle = await bundleRepository.GetBundleByIdAsync(bundleId);

            if (bundle == null)
            {
                logger.LogWarning("User {UserId} attempted to submit non-existent bundle {BundleId} for review.", userId, bundleId);
                return "Bundle not found.";
            }

            if (bundle.OwnerId != userId)
            {
                logger.LogWarning("User {UserId} attempted to submit someone else's bundle {BundleId} for review.", userId, bundleId);
                return "You do not have permission to submit this bundle.";
            }

            if (bundle.Status != BundleStatus.Draft && bundle.Status != BundleStatus.Rejected)
            {
                logger.LogWarning(
                    "User {UserId} attempted to submit bundle {BundleId} for review but it is already in status {Status}.",
                    userId,
                    bundleId,
                    bundle.Status);

                return "This bundle is already submitted or published.";
            }

            bundle.Status = BundleStatus.PendingReview;
            bundle.UpdatedAt = DateTime.UtcNow;

            try
            {
                await bundleRepository.UpdateBundleAsync(bundle);
                logger.LogInformation("User {UserId} successfully submitted bundle {BundleId} for review.", userId, bundleId);
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error submitting bundle {BundleId} for review by user {UserId}.", bundleId, userId);
                return "An error occurred while submitting the bundle.";
            }
        }
    }
}
