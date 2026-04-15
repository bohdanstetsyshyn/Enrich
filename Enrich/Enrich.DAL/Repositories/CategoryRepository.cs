using Enrich.DAL.Data;
using Enrich.DAL.Entities;
using Enrich.DAL.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Enrich.DAL.Repositories
{
    public class CategoryRepository(ApplicationDbContext dbContext) : ICategoryRepository
    {
        public async Task<IEnumerable<Category>> GetAllCategoriesAsync()
        {
            return await dbContext.Categories
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<Category> CreateCategoryAsync(Category category)
        {
            dbContext.Categories.Add(category);
            await dbContext.SaveChangesAsync();
            return category;
        }

        public async Task<Category?> GetCategoryByNameAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            var n = name.Trim().ToLower();
            return await dbContext.Categories
                .FirstOrDefaultAsync(c => c.Name.ToLower() == n);
        }

        public async Task<IEnumerable<Category>> GetCategoriesByIdsAsync(IEnumerable<int> ids)
        {
            if (ids == null)
            {
                return [];
            }

            var idArray = ids.Where(i => i > 0).Distinct().ToArray();

            return await dbContext.Categories
                .Where(c => idArray.Contains(c.Id))
                .OrderBy(c => c.Name)
                .ToListAsync();
        }
        public async Task<Category?> GetCategoryByIdAsync(int id)
        {
            return await dbContext.Categories.FindAsync(id);
        }

        public async Task UpdateCategoryAsync(Category category)
        {
            dbContext.Categories.Update(category);
            await dbContext.SaveChangesAsync();
        }

        public async Task DeleteCategoryAsync(int id)
        {
            var category = await dbContext.Categories.FindAsync(id);
            if (category != null)
            {
                dbContext.Categories.Remove(category);
                await dbContext.SaveChangesAsync();
            }
        }
    }
}
