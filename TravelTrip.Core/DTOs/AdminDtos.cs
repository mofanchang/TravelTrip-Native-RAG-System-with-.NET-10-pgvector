namespace TravelTrip.Core.DTOs;

/// <summary>
/// 建立行程請求資料
/// </summary>
public class CreateTripRequest
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal Price { get; set; }
    public string Image { get; set; } = "";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int AvailableSeats { get; set; }
    public string Location { get; set; } = "";
    public int Duration { get; set; }
    public string Country { get; set; } = "";
    public string City { get; set; } = "";
    public string? Keywords { get; set; }
}

/// <summary>
/// 更新行程請求資料
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
