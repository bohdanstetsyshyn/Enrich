using Enrich.DAL.Entities;

namespace Enrich.DAL.Interfaces
{
    public interface IBundleRepository
    {
        Task<Bundle?> GetBundleByIdAsync(int bundleId);

        Task<Bundle?> GetBundleByIdWithDetailsAsync(int bundleId);

        Task<IEnumerable<Bundle>> GetUserBundlesAsync(string userId);

        Task<(IEnumerable<Bundle> Items, int Total)> GetUserBundlesPageAsync(
            string userId,
            string? searchTerm,
            string? categoryFilter = null,
            string? difficultyLevel = null,
            int? minWordCount = null,
            int? maxWordCount = null,
            int page = 1,
            int pageSize = 6);

        Task UpdateBundleAsync(Bundle bundle);

        Task SyncBundleRelationsAsync(int bundleId, IEnumerable<int>? wordIds, IEnumerable<int>? categoryIds);

        Task DeleteBundleAsync(int bundleId);

        Task<bool> BundleTitleExistsForUserAsync(string userId, string titleLower);

        Task AddWordsToBundleAsync(int bundleId, IEnumerable<int> wordIds);

        Task RemoveWordsFromBundleAsync(int bundleId, IEnumerable<int> wordIds);

        Task AddCategoriesToBundleAsync(int bundleId, IEnumerable<int> categoryIds);

        Task AddTagsToBundleAsync(int bundleId, IEnumerable<int> tagIds);

        Task<(IEnumerable<Bundle> Items, int Total)> GetSystemBundlesPageAsync(
            string? searchTerm,
            string? category,
            string? difficultyLevel,
            int? minWordCount,
            int? maxWordCount,
            int page,
            int pageSize);

        Task<Bundle> CreateBundleAsync(Bundle bundle);

        Task<bool> UserHasBundleAsync(string userId, int bundleId);

        Task SaveUserBundleAsync(UserBundle userBundle);
        
        Task<Bundle?> GetBundleWithWordsAsync(int bundleId);
    }
}
