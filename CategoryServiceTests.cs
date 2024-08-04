using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NewsAggregation.Data.UnitOfWork;
using NewsAggregation.DTO.Category;
using NewsAggregation.Services;
using News_aggregation.Entities;
using System.Linq.Expressions;

namespace NewsAggregation.Tests
{
    public class CategoryServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
        private readonly Mock<IMapper> _mockMapper = new();
        private readonly Mock<ILogger<AuthService>> _mockLogger = new();

        private readonly CategoryService _categoryService;

        public CategoryServiceTests()
        {
            _categoryService = new CategoryService(_mockMapper.Object, _mockUnitOfWork.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task CreateCategory_Should_Return_OkObjectResult_With_CategoryCreateDto()
        {

            var categoryDto = new CategoryCreateDto { Name = "TestCategory", Description = "Test Description" };
            var category = new Category { Id = 1, Name = "TestCategory", Description = "Test Description" };

            _mockMapper.Setup(m => m.Map<Category>(It.IsAny<CategoryCreateDto>())).Returns(category);
            _mockMapper.Setup(m => m.Map<CategoryCreateDto>(It.IsAny<Category>())).Returns(categoryDto);

            _mockUnitOfWork.Setup(uow => uow.Repository<Category>().Create(It.IsAny<Category>()));
            _mockUnitOfWork.Setup(uow => uow.CompleteAsync()).ReturnsAsync(true);


            var result = await _categoryService.CreateCategory(categoryDto);

            
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnValue = Assert.IsType<CategoryCreateDto>(okResult.Value);
            Assert.Equal(categoryDto.Name, returnValue.Name);
        }

        [Fact]
        public async Task DeleteCategory_Should_Return_OkResult_When_Category_Exists()
        {
            
            var categoryId = 1;
            var category = new Category { Id = categoryId, Name = "TestCategory", Description = "Test Description" };

            _mockUnitOfWork.Setup(uow => uow.Repository<Category>().GetById(categoryId)).ReturnsAsync(category);
            _mockUnitOfWork.Setup(uow => uow.Repository<Category>().Delete(category));
            _mockUnitOfWork.Setup(uow => uow.CompleteAsync()).ReturnsAsync(true);

            
            var result = await _categoryService.DeleteCategory(categoryId);

            
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task DeleteCategory_Should_Return_NotFoundResult_When_Category_Does_Not_Exist()
        {

            var categoryId = 1;

            _mockUnitOfWork.Setup(uow => uow.Repository<Category>().GetById(categoryId)).ReturnsAsync((Category)null);


            var result = await _categoryService.DeleteCategory(categoryId);


            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task GetCategoryById_Should_Return_CategoryCreateDto()
        {
            
            var categoryId = 1;
            var category = new Category { Id = categoryId, Name = "TestCategory", Description = "Test Description" };
            var categoryDto = new CategoryCreateDto { Name = "TestCategory", Description = "Test Description" };

            _mockUnitOfWork.Setup(uow => uow.Repository<Category>().GetById(categoryId)).ReturnsAsync(category);
            _mockMapper.Setup(m => m.Map<CategoryCreateDto>(category)).Returns(categoryDto);

            
            var result = await _categoryService.GetCategoryById(categoryId);

            
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnValue = Assert.IsType<CategoryCreateDto>(okResult.Value);
            Assert.Equal(categoryDto.Name, returnValue.Name);
        }


        [Fact]
        public async Task GetCategoryByName_Should_Return_InternalServerErrorResult_When_Exception_Occurs()
        {
            
            var categoryName = "NonExistentCategory";

            _mockUnitOfWork.Setup(uow => uow.Repository<Category>().GetByCondition(It.IsAny<Expression<Func<Category, bool>>>()))
                .Throws(new Exception("Database error"));

            
            var result = await _categoryService.GetCategoryByName(categoryName);

            
            var statusCodeResult = Assert.IsType<StatusCodeResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }


    }
}
       