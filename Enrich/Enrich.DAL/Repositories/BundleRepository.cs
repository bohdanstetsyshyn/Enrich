using Enrich.DAL.Data;
using Enrich.DAL.Entities;
using Enrich.DAL.Entities.Enums;
using Enrich.DAL.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Enrich.DAL.Repositories
{
    public class BundleRepository(ApplicationDbContext dbContext) : IBundleRepository
    {
        public async Task<Bundle?> GetBundleByIdAsync(int bundleId)
        {
            return await dbContext.Bundles.FindAsync(bundleId);
        }

        public async Task<Bundle?> GetBundleByIdWithDetailsAsync(int bundleId)
        {
            return await dbContext.Bundles
                .Include(b => b.Owner)
                .Include(b => b.Words)
                .Include(b => b.Categories)
                .Include(b => b.Tags)
                .AsSplitQuery()
                .FirstOrDefaultAsync(b => b.Id == bundleId);
        }

        public async Task<IEnumerable<Bundle>> GetUserBundlesAsync(string userId)
        {
            return await dbContext.Bundles
                .Where(b => b.OwnerId == userId || b.UserBundles.Any(ub => ub.UserId == userId))
                .Include(b => b.Words)
                .Include(b => b.Categories)
                .Include(b => b.Tags)
                .AsSplitQuery()
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
        }

        public async Task<(IEnumerable<Bundle> Items, int Total)> GetUserBundlesPageAsync(
            string userId,
            string? searchTerm,
            string? categoryFilter = null,
            string? difficultyLevel = null,
            int? minWordCount = null,
            int? maxWordCount = null,
            int page = 1,
            int pageSize = 6)
        {
            var query = dbContext.Bundles
                .Where(b => b.OwnerId == userId || b.UserBundles.Any(ub => ub.UserId == userId))
                .Include(b => b.Words)
                .Include(b => b.Categories)
                .Include(b => b.Tags)
                .AsSplitQuery()
                .AsQueryable();

            // Search filter
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var st = searchTerm.Trim().ToLower();
                query = query.Where(b => b.Title.ToLower().Contains(st) ||
                                         (b.Description != null && b.Description.ToLower().Contains(st)));
            }

            if (!string.IsNullOrWhiteSpace(categoryFilter))
            {
                var categories = categoryFilter.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                              .Select(c => c.ToLower()).ToArray();
                query = query.Where(b => b.Categories.Any(c => categories.Contains(c.Name.ToLower())));
            }

            if (!string.IsNullOrWhiteSpace(difficultyLevel))
            {
                var levels = difficultyLevel.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                           .Select(l => l.ToLower()).ToArray();
                query = query.Where(b => b.DifficultyLevels.Any(dl => levels.Contains(dl.ToLower())));
            }

            if (minWordCount.HasValue)
            {
                query = query.Where(b => b.Words.Count >= minWordCount.Value);
            }

            if (maxWordCount.HasValue)
            {
                query = query.Where(b => b.Words.Count <= maxWordCount.Value);
            }

            var total = await query.CountAsync();

            var bundles = await query
                .OrderByDescending(b => b.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (bundles, total);
        }

        public async Task<(IEnumerable<Bundle> Items, int Total)> GetSystemBundlesPageAsync(
            string? searchTerm,
            string? category,
            string? difficultyLevel,
            int? minWordCount,
            int? maxWordCount,
            int page,
            int pageSize)
        {
            var query = dbContext.Bundles
                .Where(b => b.IsSystem && b.Status == BundleStatus.Published)
                .Include(b => b.Categories)
                .Include(b => b.Words)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var st = searchTerm.Trim().ToLower();
                query = query.Where(b => b.Title.ToLower().Contains(st) ||
                                         (b.Description != null && b.Description.ToLower().Contains(st)));
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                var catsLower = category.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                       .Select(c => c.ToLower()).ToArray();
                query = query.Where(b => b.Categories.Any(c => catsLower.Contains(c.Name.ToLower())));
            }

            if (!string.IsNullOrWhiteSpace(difficultyLevel))
            {
                var levelsLower = difficultyLevel.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                                .Select(l => l.ToLower()).ToArray();
                query = query.Where(b => b.DifficultyLevels.Any(dl => levelsLower.Contains(dl.ToLower())));
            }

            if (minWordCount.HasValue)
            {
                query = query.Where(b => b.Words.Count >= minWordCount.Value);
            }

            if (maxWordCount.HasValue)
            {
                query = query.Where(b => b.Words.Count <= maxWordCount.Value);
            }

            var total = await query.CountAsync();

            var items = await query
                .OrderBy(b => b.Title)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, total);
        }

        public async Task UpdateBundleAsync(Bundle bundle)
        {
            bundle.CreatedAt = DateTime.SpecifyKind(bundle.CreatedAt, DateTimeKind.Utc);
            bundle.UpdatedAt = DateTime.SpecifyKind(bundle.UpdatedAt, DateTimeKind.Utc);
            if (bundle.ReviewedAt.HasValue)
            {
                bundle.ReviewedAt = DateTime.SpecifyKind(bundle.ReviewedAt.Value, DateTimeKind.Utc);
            }

            dbContext.Bundles.Update(bundle);
            await dbContext.SaveChangesAsync();
        }

        public async Task SyncBundleRelationsAsync(int bundleId, IEnumerable<int>? wordIds, IEnumerable<int>? categoryIds)
        {
            var bundle = await dbContext.Bundles
                .Include(b => b.Words)
                .Include(b => b.Categories)
                .FirstOrDefaultAsync(b => b.Id == bundleId);

            if (bundle != null)
            {
                bundle.Words.Clear();
                if (wordIds != null && wordIds.Any())
                {
                    var words = await dbContext.Words.Where(w => wordIds.Contains(w.Id)).ToListAsync();
                    foreach (var w in words)
                    {
                        bundle.Words.Add(w);
                    }
                }

                bundle.Categories.Clear();
                if (categoryIds != null && categoryIds.Any())
                {
                    var cats = await dbContext.Categories.Where(c => categoryIds.Contains(c.Id)).ToListAsync();
                    foreach (var c in cats)
                    {
                        bundle.Categories.Add(c);
                    }
                }

                await dbContext.SaveChangesAsync();
            }
        }

        public async Task DeleteBundleAsync(int bundleId)
        {
            var bundle = await dbContext.Bundles
                .Include(b => b.Words)
                .Include(b => b.Categories)
                .Include(b => b.Tags)
                .Include(b => b.UserBundles)
                .AsSplitQuery()
                .FirstOrDefaultAsync(b => b.Id == bundleId);

            if (bundle != null)
            {
                bundle.Words.Clear();
                bundle.Categories.Clear();
                bundle.Tags.Clear();
                bundle.UserBundles.Clear();

                dbContext.Bundles.Remove(bundle);
                await dbContext.SaveChangesAsync();
            }
        }

        public async Task<bool> BundleTitleExistsForUserAsync(string userId, string titleLower)
        {
            return await dbContext.Bundles
                .AnyAsync(b =>
                    b.OwnerId == userId &&
                    b.Title.ToLower() == titleLower);
        }

        public async Task AddWordsToBundleAsync(int bundleId, IEnumerable<int> wordIds)
        {
            var bundle = await dbContext.Bundles
                .Include(b => b.Words)
                .FirstOrDefaultAsync(b => b.Id == bundleId);

            if (bundle != null)
            {
                var words = await dbContext.Words
                    .Where(w => wordIds.Contains(w.Id))
                    .ToListAsync();

                foreach (var word in words)
                {
                    if (!bundle.Words.Any(w => w.Id == word.Id))
                    {
                        bundle.Words.Add(word);
                    }
                }

                await dbContext.SaveChangesAsync();
            }
        }

        public async Task RemoveWordsFromBundleAsync(int bundleId, IEnumerable<int> wordIds)
        {
            var bundle = await dbContext.Bundles
                .Include(b => b.Words)
                .FirstOrDefaultAsync(b => b.Id == bundleId);

            if (bundle != null)
            {
                var wordsToRemove = bundle.Words.Where(w => wordIds.Contains(w.Id)).ToList();

                foreach (var word in wordsToRemove)
                {
                    bundle.Words.Remove(word);
                }

                await dbContext.SaveChangesAsync();
            }
        }

        public async Task AddCategoriesToBundleAsync(int bundleId, IEnumerable<int> categoryIds)
        {
            var bundle = await dbContext.Bundles
                .Include(b => b.Categories)
                .FirstOrDefaultAsync(b => b.Id == bundleId);

            if (bundle != null)
            {
                var categories = await dbContext.Categories
                    .Where(c => categoryIds.Contains(c.Id))
                    .ToListAsync();

                foreach (var category in categories)
                {
                    if (!bundle.Categories.Any(c => c.Id == category.Id))
                    {
                        bundle.Categories.Add(category);
                    }
                }

                await dbContext.SaveChangesAsync();
            }
        }

        public async Task AddTagsToBundleAsync(int bundleId, IEnumerable<int> tagIds)
        {
            var bundle = await dbContext.Bundles
                .Include(b => b.Tags)
                .FirstOrDefaultAsync(b => b.Id == bundleId);

            if (bundle != null)
            {
                var tags = await dbContext.Tags
                    .Where(t => tagIds.Contains(t.Id))
                    .ToListAsync();

                foreach (var tag in tags)
                {
                    if (!bundle.Tags.Any(t => t.Id == tag.Id))
                    {
                        bundle.Tags.Add(tag);
                    }
                }

                await dbContext.SaveChangesAsync();
            }
        }

        public async Task<Bundle> CreateBundleAsync(Bundle bundle)
        {
            dbContext.Bundles.Add(bundle);
            await dbContext.SaveChangesAsync();
            return bundle;
        }

        public async Task<bool> UserHasBundleAsync(string userId, int bundleId)
        {
            return await dbContext.UserBundles
                .AnyAsync(ub => ub.UserId == userId && ub.BundleId == bundleId);
        }

        public async Task SaveUserBundleAsync(UserBundle userBundle)
        {
            await dbContext.UserBundles.AddAsync(userBundle);
            await dbContext.SaveChangesAsync();
        }
        public async Task<Bundle?> GetBundleWithWordsAsync(int bundleId)
        {
            return await dbContext.Bundles
                .Include(b => b.Words)
                .FirstOrDefaultAsync(b => b.Id == bundleId);
        }
    }
}
