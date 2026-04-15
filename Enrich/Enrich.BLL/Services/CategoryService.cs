using Enrich.BLL.Common;
using Enrich.BLL.DTOs;
using Enrich.BLL.Interfaces;
using Enrich.DAL.Entities;
using Enrich.DAL.Interfaces;
using Microsoft.Extensions.Logging;

namespace Enrich.BLL.Services
{
    public class CategoryService(
        ICategoryRepository categoryRepository,
        ILogger<CategoryService> logger) : ICategoryService
    {
        public async Task<IEnumerable<CategoryDTO>> GetAllCategoriesAsync()
        {
            var categories = await categoryRepository.GetAllCategoriesAsync();
            return categories.Select(c => new CategoryDTO { Id = c.Id, Name = c.Name });
        }

        public async Task<CategoryDTO?> GetCategoryByIdAsync(int id)
        {
            var category = await categoryRepository.GetCategoryByIdAsync(id);
            if (category == null) return null;

            return new CategoryDTO { Id = category.Id, Name = category.Name };
        }

        public async Task<Result> CreateCategoryAsync(CategoryDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name)) return "Category name cannot be empty.";

            var existing = await categoryRepository.GetCategoryByNameAsync(dto.Name);
            if (existing != null)
            {
                logger.LogWarning("Спроба створити категорію з дублікатом імені: {Name}", dto.Name);
                return $"Category with name '{dto.Name}' already exists.";
            }

            var category = new Category { Name = dto.Name.Trim() };
            await categoryRepository.CreateCategoryAsync(category);

            logger.LogInformation("Адміністратор створив нову категорію: {Name} (ID: {Id})", category.Name, category.Id);
            return true;
        }

        public async Task<Result> UpdateCategoryAsync(CategoryDTO dto)
        {
            var category = await categoryRepository.GetCategoryByIdAsync(dto.Id);
            if (category == null) return "Category not found.";

            var existingWithName = await categoryRepository.GetCategoryByNameAsync(dto.Name);
            if (existingWithName != null && existingWithName.Id != dto.Id)
            {
                return $"Another category with name '{dto.Name}' already exists.";
            }

            category.Name = dto.Name.Trim();
            await categoryRepository.UpdateCategoryAsync(category);

            logger.LogInformation("Категорію ID: {Id} оновлено на: {Name}", dto.Id, dto.Name);
            return true;
        }

        public async Task<Result> DeleteCategoryAsync(int id)
        {
            var category = await categoryRepository.GetCategoryByIdAsync(id);
            if (category == null) return "Category not found.";

            await categoryRepository.DeleteCategoryAsync(id);

            logger.LogInformation("Категорію '{Name}' (ID: {Id}) було видалено.", category.Name, id);
            return true;
        }
    }
}