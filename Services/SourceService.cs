using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewsAggregation.Data;
using NewsAggregation.DTO.Source;
using NewsAggregation.Helpers;
using NewsAggregation.Models;
using NewsAggregation.Services.Interfaces;

namespace NewsAggregation.Services
{
    public class SourceService : ISourceService
    {
        private readonly DBContext _dBContext;
        private readonly ILogger<AuthService> _logger;

        public SourceService(DBContext dBContext, ILogger<AuthService> logger)
        {
            _dBContext = dBContext;
            _logger = logger;
        }

        public async Task<IActionResult> GetSourceById(Guid id)
        {
            try
            {
                var source = await _dBContext.Sources.FindAsync(id);

                if (source == null)
                {
                    return new NotFoundResult();
                }

                return new OkObjectResult(source);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetSourceById");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> GetAllSources(string? range = null)
        {
            var queryParams = ParameterParser.ParseRangeAndSort(range, "sort");

            try
            {
                var sources = await _dBContext.Sources
                    .Skip((queryParams.Page - 1) * queryParams.PerPage)
                    .Take(queryParams.PerPage)
                    .ToListAsync();

                return new OkObjectResult(sources);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAllSources");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> CreateSource(SourceCreateDto source)
        {
            try
            {
                Source newSource = new()
                {
                    Name = source.Name,
                    Description = source.Description,
                    Url = source.Url,
                    Category = source.Category,
                    Language = source.Language,
                    Country = source.Country
                };

                _dBContext.Sources.Add(newSource);
                await _dBContext.SaveChangesAsync();
                return new OkObjectResult(newSource);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateSource");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> UpdateSource(Guid id, SourceCreateDto source)
        {
            try
            {
                var sourceToUpdate = await _dBContext.Sources.FindAsync(id);

                if (sourceToUpdate == null)
                {
                    return new NotFoundResult();
                }

                sourceToUpdate.Name = source.Name;
                sourceToUpdate.Description = source.Description;
                sourceToUpdate.Url = source.Url;
                sourceToUpdate.Category = source.Category;
                sourceToUpdate.Language = source.Language;
                sourceToUpdate.Country = source.Country;

                await _dBContext.SaveChangesAsync();

                return new OkObjectResult(sourceToUpdate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateSource");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> DeleteSource(Guid id)
        {
            try
            {
                var source = await _dBContext.Sources.FindAsync(id);

                if (source == null)
                {
                    return new NotFoundResult();
                }

                _dBContext.Sources.Remove(source);
                await _dBContext.SaveChangesAsync();

                return new OkResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteSource");
                return new StatusCodeResult(500);
            }
        }

    }
}
