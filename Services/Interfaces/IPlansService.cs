using Microsoft.AspNetCore.Mvc;
using NewsAggregation.DTO.Plans;
using ServiceStack;

namespace NewsAggregation.Services.Interfaces
{
    public interface IPlansService : IService
    {
        public Task<IActionResult> GetPlanById(Guid id);
        public Task<IActionResult> GetAllPlans(string? range = null);
        public Task<IActionResult> GetAllActivePlans(string? range = null);
        public Task<IActionResult> CreatePlan(PlanCreateDto plan);
        public Task<IActionResult> UpdatePlan(Guid id, PlanCreateDto plan);
        public Task<IActionResult> DeletePlan(Guid id);
    }
}
