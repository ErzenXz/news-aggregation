namespace NewsAggregation.DTO.UserPreferences;

public class UserPreferencesDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public int CategoryId { get; set; }
    public int TagId { get; set; }
}