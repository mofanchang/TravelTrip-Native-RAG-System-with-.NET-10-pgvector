namespace TravelTrip.Core.DTOs;

public class TripSearchResultDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public int Duration { get; set; }
    public string Keywords { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Image { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int AvailableSeats { get; set; }
    public double SimilarityScore { get; set; }
}

public class TripSearchRequest
{
    public string Query { get; set; } = string.Empty;
    public int TopK { get; set; } = 5;
}

/// <summary>
/// 建立新行程的請求 DTO
/// </summary>
public class CreateTripRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Image { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int AvailableSeats { get; set; }
    public string Location { get; set; } = string.Empty;
    public int Duration { get; set; }
    public string Country { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string? Keywords { get; set; }
}

/// <summary>
/// 更新行程的請求 DTO
/// </summary>
public class UpdateTripRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? Image { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int AvailableSeats { get; set; }
    public string? Location { get; set; }
    public int Duration { get; set; }
    public string? Country { get; set; }
    public string? City { get; set; }
    public string? Keywords { get; set; }
}
