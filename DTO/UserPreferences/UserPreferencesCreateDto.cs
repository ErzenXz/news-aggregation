namespace NewsAggregation.DTO.UserPreferences;

public class UserPreferencesCreateDto
{
    public Guid UserId { get; set; }
    public int CategoryId { get; set; }
    public string Tags { get; set; }
}