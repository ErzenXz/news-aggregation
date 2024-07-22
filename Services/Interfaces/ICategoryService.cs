using Microsoft.AspNetCore.Mvc;
using NewsAggregation.DTO.Category;

namespace NewsAggregation.Services.Interfaces
{
    public interface ICategoryService
    {
        Task<IActionResult> CreateCategory(CategoryCreateDto createCategory);
        Task<IActionResult> DeleteCategory(int id);
        Task<IActionResult> GetCategoryByName(string name);
        Task<IActionResult> GetAllCategories();
        Task<IActionResult> UpdateCategory(int id, CategoryCreateDto updateCategory);
        Task<IActionResult> GetCategoryById(int id);
    }
}
