using Enrich.BLL.Common;
using Enrich.BLL.DTOs;

namespace Enrich.BLL.Interfaces
{
    public interface ICategoryService
    {
        Task<IEnumerable<CategoryDTO>> GetAllCategoriesAsync();
        Task<CategoryDTO?> GetCategoryByIdAsync(int id);
        Task<Result> CreateCategoryAsync(CategoryDTO dto);
        Task<Result> UpdateCategoryAsync(CategoryDTO dto);
        Task<Result> DeleteCategoryAsync(int id);
    }
}