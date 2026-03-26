using Enrich.BLL.Common;
using Enrich.BLL.DTOs;
using Enrich.DAL.Entities;

namespace Enrich.BLL.Interfaces
{
    public interface IWordService
    {
        Task<Result> CreatePersonalWordAsync(string userId, CreatePersonalWordDTO dto);

        Task<Result> DeleteWordAsync(string userId, int wordId);

        Task<IEnumerable<PersonalWordDTO>> GetPersonalWordsAsync(string userId);

        Task<PagedResult<PersonalWordDTO>> GetPersonalWordsAsync(string userId, string? searchTerm, string? category, string? partOfSpeech, string? difficultyLevel, int page, int pageSize);

        Task<PagedResult<SystemWordDTO>> GetSystemWordsAsync(string userId, string? searchTerm, string? category, string? partOfSpeech, string? difficultyLevel, int page, int pageSize);

        Task<Result> SaveSystemWordAsync(string userId, int wordId);

        Task<IEnumerable<Category>> GetAllCategoriesAsync();

        Task<IEnumerable<Category>> GetCategoriesByIdsAsync(IEnumerable<int> ids);

        Task<Category> CreateCategoryAsync(string name);

        Task<Category?> GetCategoryByNameAsync(string name);
    }
}
