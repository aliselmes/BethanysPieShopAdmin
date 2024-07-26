using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace BethanysPieShopAdmin.Models.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly BethanysPieShopDbContext _bethanysPieShopDbContext;

        private IMemoryCache _memoryCache;
        private const string AllCategoriesCacheName = "AllCategories";

        public CategoryRepository(BethanysPieShopDbContext bethanysPieShopDbContext, IMemoryCache memoryCache)
        {
            _bethanysPieShopDbContext = bethanysPieShopDbContext;
            _memoryCache = memoryCache;
        }

        public IEnumerable<Category> GetAllCategories()
        {
            return _bethanysPieShopDbContext.Categories.OrderBy(p => p.CategoryId);
        }

        public async Task<IEnumerable<Category>> GetAllCategoriesAsync()
        {
            List<Category> allCategories = null;

            if (!_memoryCache.TryGetValue(AllCategoriesCacheName, out allCategories))
            {
                allCategories = await _bethanysPieShopDbContext.Categories.OrderBy(p => p.CategoryId).AsNoTracking().ToListAsync();

                var cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(60));

                _memoryCache.Set(AllCategoriesCacheName, allCategories, cacheEntryOptions);
            }

            return allCategories;
        }

        public async Task<Category?> GetCategoryByIdAsync(int id)
        {
            return await _bethanysPieShopDbContext.Categories.Include(p => p.Pies).AsNoTracking()
                .FirstOrDefaultAsync(c => c.CategoryId == id);
        }

        public async Task<int> AddCategoryAsync(Category category)
        {
            bool categoryWithSameNameExists =
                await _bethanysPieShopDbContext.Categories.AnyAsync(c => c.Name == category.Name);
            if (categoryWithSameNameExists)
            {
                throw new Exception("A category with the same name already exists");
            }

            _bethanysPieShopDbContext.Categories.Add(category);

            int result = await _bethanysPieShopDbContext.SaveChangesAsync();

            _memoryCache.Remove(AllCategoriesCacheName);

            return result;
        }

        public async Task<int> UpdateCategoryAsync(Category category)
        {
            bool categoryWithSameNameExist = await _bethanysPieShopDbContext.Categories.AnyAsync(c => c.Name == category.Name && c.CategoryId != category.CategoryId);

            if (categoryWithSameNameExist)
            {
                throw new Exception("A category with the same name already exists");
            }

            var categoryToUpdate = await _bethanysPieShopDbContext.Categories.FirstOrDefaultAsync(c => c.CategoryId == category.CategoryId);

            if (categoryToUpdate != null)
            {
                categoryToUpdate.Name = category.Name;
                categoryToUpdate.Description = category.Description;

                _bethanysPieShopDbContext.Categories.Update(categoryToUpdate);

                int result = await _bethanysPieShopDbContext.SaveChangesAsync();

                _memoryCache.Remove(AllCategoriesCacheName);

                return result;
            }
            else
            {
                throw new ArgumentException($"The category to update can't be found.");
            }
        }

        public async Task<int> DeleteCategoryAsync(int id)
        {
            var categoryToDelete = await _bethanysPieShopDbContext.Categories.FirstOrDefaultAsync(c => c.CategoryId == id);

            var piesInCategory = _bethanysPieShopDbContext.Pies.Any(p => p.CategoryId == id);

            if (piesInCategory)
            {
                throw new Exception(
                    "Pies exist in this category. Delete all pies in this category before deleting the category.");
            }

            if (categoryToDelete != null)
            {
                _bethanysPieShopDbContext.Categories.Remove(categoryToDelete);
                int result = await _bethanysPieShopDbContext.SaveChangesAsync();

                _memoryCache.Remove(AllCategoriesCacheName);

                return result;
            }
            else
            {
                throw new ArgumentException($"The category to delete can't be found.");
            }
        }
    }
}