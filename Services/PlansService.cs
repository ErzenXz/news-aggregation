using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewsAggregation.Data;
using NewsAggregation.Data.UnitOfWork;
using NewsAggregation.DTO.Plans;
using NewsAggregation.Models;
using NewsAggregation.Services.Interfaces;
using Stripe;

namespace NewsAggregation.Services
{
    public class PlansService : IPlansService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AuthService> _logger;

        public PlansService(IUnitOfWork unitOfWork, ILogger<AuthService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<IActionResult> GetAllPlans(string? range = null)
        {
            var queryParams = ParameterParser.ParseRangeAndSort(range, "sort");
            var page = queryParams.Page;
            var pageSize = queryParams.PerPage;

            try
            {
                var plans = await _unitOfWork.Repository<Plans>().GetAll()
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return new OkObjectResult(new {plans});
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetPlans");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> GetAllActivePlans(string? range = null)
        {
            var queryParams = ParameterParser.ParseRangeAndSort(range, "sort");
            var page = queryParams.Page;
            var pageSize = queryParams.PerPage;

            try
            {
                var now = DateTime.UtcNow;

                var plans = await _unitOfWork.Repository<Plans>().GetAll()
                    .Where(a => a.IsActive)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return new OkObjectResult(new { plans });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetPlans");
                // Return 500 error with message
                _logger.LogError(ex.Message, "Error in GetAllActivePlans");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> GetPlanById(Guid id)
        {
            try
            {
                var plan = await _unitOfWork.Repository<Plans>().GetById(id);

                return new OkObjectResult(plan);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetPlanById");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> CreatePlan(PlanCreateDto plan)
        {
            try
            {
                var productOptions = new ProductCreateOptions
                {
                    Name = plan.Name,
                    Description = plan.Description,
                };
                var product = await new ProductService().CreateAsync(productOptions);

                var priceOptions = new PriceCreateOptions
                {
                    UnitAmount = (long)(plan.Price * 100), // Amount in cents
                    Currency = plan.Currency,
                    Recurring = new PriceRecurringOptions
                    {
                        Interval = "month",
                    },
                    Product = product.Id,
                };
                var price = await new PriceService().CreateAsync(priceOptions);

                Plans pl = new ();
               
                pl.Id = Guid.NewGuid();
                pl.Name = plan.Name;
                pl.Description = plan.Description;
                pl.ImageUrl = plan.ImageUrl;
                pl.Price = plan.Price;
                pl.Duration = plan.Duration;
                pl.Currency = plan.Currency;
                pl.IsActive = true;
                pl.Description = plan.Description;
                pl.CreatedAt = DateTime.UtcNow;

                _unitOfWork.Repository<Plans>().Create(pl);
                await _unitOfWork.CompleteAsync();

                return new OkObjectResult(pl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreatePlan");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> UpdatePlan(Guid id, PlanCreateDto plan)
        {
            try
            {
                var pl = await _unitOfWork.Repository<Plans>().GetById(id);

                if (pl == null)
                {
                    return new NotFoundResult();
                }

                pl.Name = plan.Name;
                pl.Description = plan.Description;
                pl.ImageUrl = plan.ImageUrl;
                pl.Price = plan.Price;
                pl.Duration = plan.Duration;
                pl.Currency = plan.Currency;

                await _unitOfWork.CompleteAsync();

                return new OkObjectResult(pl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdatePlan");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> DeletePlan(Guid id)
        {
            try
            {
                var plan = await _unitOfWork.Repository<Plans>().GetById(id);

                if (plan == null)
                {
                    return new NotFoundResult();
                }

                _unitOfWork.Repository<Plans>().Delete(plan);
                await _unitOfWork.CompleteAsync();

                return new OkObjectResult(plan);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeletePlan");
                return new StatusCodeResult(500);
            }
        }
    }


   
}
