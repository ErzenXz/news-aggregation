using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewsAggregation.Data;
using NewsAggregation.DTO.Plans;
using NewsAggregation.Services.Interfaces;

namespace NewsAggregation.Controllers
{
    [Route("plan")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ApiController]
    public class PlansController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly DBContext _dBContext;
        private readonly IPlansService _plansService;


        public PlansController(IConfiguration configuration, DBContext dBContext,
            IPlansService planService)
        {
            _configuration = configuration;
            _dBContext = dBContext;
            _plansService = planService;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPlanById(Guid id)
        {
            var plans = await _plansService.GetPlanById(id);
            return Ok(plans);
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllPlans(string? range = null)
        {
            var plans = await _plansService.GetAllPlans(range);
            return Ok(plans);
        }

        [HttpGet("allActive")]
        public async Task<IActionResult> GetAllActivePlans(string? range = null)
        {
            var plans = await _plansService.GetAllActivePlans(range);
            return Ok(plans);
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreatePlan([FromBody] PlanCreateDto plan)
        {
            var newPlan = await _plansService.CreatePlan(plan);
            return Ok(newPlan);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePlan(Guid id, [FromBody] PlanCreateDto plan)
        {
            var updatedPlan = await _plansService.UpdatePlan(id, plan);
            return Ok(updatedPlan);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePlan(Guid id)
        {
            await _plansService.DeletePlan(id);
            return Ok();
        }
        
    }
}
