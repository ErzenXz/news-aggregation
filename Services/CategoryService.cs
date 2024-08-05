using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using News_aggregation.Entities;
using NewsAggregation.Data.UnitOfWork;
using NewsAggregation.DTO.Category;
using NewsAggregation.Helpers;
using NewsAggregation.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NewsAggregation.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<AuthService> _logger;

        public CategoryService(IMapper mapper, IUnitOfWork unitOfWork, ILogger<AuthService> logger)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<IActionResult> CreateCategory(CategoryCreateDto categoryDto)
        {
            try
            {
                var category = _mapper.Map<Category>(categoryDto);

                _unitOfWork.Repository<Category>().Create(category);

                await _unitOfWork.CompleteAsync();

                var createdCategoryDto = _mapper.Map<CategoryCreateDto>(category);
                return new OkObjectResult(createdCategoryDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Creating Category");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> DeleteCategory(int id)
        {
            try
            {
                var categoryToDelete = await _unitOfWork.Repository<Category>().GetById(id);

                if (categoryToDelete != null)
                {
                    _unitOfWork.Repository<Category>().Delete(categoryToDelete);
                    await _unitOfWork.CompleteAsync();
                    return new OkResult();
                }
                else
                {
                    _logger.LogWarning($"Category with id {id} not found.");
                    return new NotFoundResult();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Deleting Category");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> GetCategoryById(int id)
        {
            try
            {
                var category = await _unitOfWork.Repository<Category>().GetById(id);

                var categoryDto = _mapper.Map<CategoryCreateDto>(category);
                return new OkObjectResult(categoryDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetCategoryById");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> GetCategoryByName(string name)
        {
            try
            {
                var category = await _unitOfWork.Repository<Category>().GetByCondition(c => c.Name == name).FirstOrDefaultAsync();

                if (category == null)
                {
                    _logger.LogWarning($"Category with id {name} not found.");
                    return new NotFoundResult();
                }

                var categoryDto = _mapper.Map<CategoryCreateDto>(category);
                return new OkObjectResult(categoryDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetCategoryById");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> GetAllCategories(string? range = null)
        {
            var queryParams = ParameterParser.ParseRangeAndSort(range, "sort");
            var page = queryParams.Page;
            var pageSize = queryParams.PerPage;


            try
            {
                var categories = await _unitOfWork.Repository<Category>().GetAll().Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

                return new ObjectResult(categories);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAllCategories");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> UpdateCategory(int id, CategoryCreateDto updateCategoryDto)
        {
            try
            {
                var category = await _unitOfWork.Repository<Category>().GetById(id);
                if (category == null)
                {
                    _logger.LogWarning($"Category with id {id} not found.");
                    return new NotFoundResult();
                }

                
                category.Name = updateCategoryDto.Name;
                category.Description = updateCategoryDto.Description;
                

                await _unitOfWork.CompleteAsync();

                var updatedCategoryDto = _mapper.Map<CategoryCreateDto>(category);
                return new OkObjectResult(updatedCategoryDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateCategory");
                return new StatusCodeResult(500);
            }
        }
    }
}
