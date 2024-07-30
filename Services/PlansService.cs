using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewsAggregation.Data;
using NewsAggregation.DTO.Plans;
using NewsAggregation.Models;
using NewsAggregation.Services.Interfaces;

namespace NewsAggregation.Services
{
    public class PlansService : IPlansService
    {
        private readonly DBContext _dBContext;
        private readonly ILogger<AuthService> _logger;

        public PlansService(DBContext dBContext, ILogger<AuthService> logger)
        {
            _dBContext = dBContext;
            _logger = logger;
        }

        public async Task<IActionResult> GetAllPlans(string? range = null)
        {
            var queryParams = ParameterParser.ParseRangeAndSort(range, "sort");

            try
            {
                var plans = await _dBContext.Plans
                    .Skip((queryParams.Page - 1) * queryParams.PerPage)
                    .Take(queryParams.PerPage)
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

            try
            {
                var now = DateTime.UtcNow;

                var plans = await _dBContext.Plans
                    .Where(a => a.IsActive)
                    .Skip((queryParams.Page - 1) * queryParams.PerPage)
                    .Take(queryParams.PerPage)
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
                var plan = await _dBContext.Plans
                    .Where(a => a.Id == id)
                    .FirstOrDefaultAsync();

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


                _dBContext.Plans.Add(pl);
                await _dBContext.SaveChangesAsync();

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
                var pl = await _dBContext.Plans
                    .Where(a => a.Id == id)
                    .FirstOrDefaultAsync();

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

                await _dBContext.SaveChangesAsync();

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
                var plan = await _dBContext.Plans
                    .Where(a => a.Id == id)
                    .FirstOrDefaultAsync();

                if (plan == null)
                {
                    return new NotFoundResult();
                }

                _dBContext.Plans.Remove(plan);
                await _dBContext.SaveChangesAsync();

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
