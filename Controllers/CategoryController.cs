using Microsoft.AspNetCore.Mvc;
using NewsAggregation.DTO.Category; 
using NewsAggregation.Services.Interfaces;


namespace NewsAggregation.Controllers
{
    [ApiController]
    [Route("category")]
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryService _categoryService;

        public CategoryController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        [HttpPost("create")]
        public async Task<ActionResult<CategoryCreateDto>> CreateCategory(CategoryCreateDto createCategory)
        {
            var createdCategory = await _categoryService.CreateCategory(createCategory);
            return Ok(createdCategory);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            await _categoryService.DeleteCategory(id);
            return NoContent();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CategoryCreateDto>> GetCategoryById(int id)
        {
            var category = await _categoryService.GetCategoryById(id);
            return Ok(category);
        }

        [HttpGet("{name}")]
        public async Task<ActionResult<CategoryCreateDto>> GetCategoryByName(string name)
        {
            var category = await _categoryService.GetCategoryByName(name);
            return Ok(category);
        }

        [HttpGet("all")]
        public async Task<ActionResult<List<CategoryCreateDto>>> GetAllCategories()
        {
            var categories = await _categoryService.GetAllCategories();
            return Ok(categories);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<CategoryCreateDto>> UpdateCategory(int id, [FromBody] CategoryCreateDto updateCategory)
        {
            var updatedCategory = await _categoryService.UpdateCategory(id, updateCategory);
            return Ok(updatedCategory);
        }
    }
}
