using Microsoft.AspNetCore.Mvc;

public class ProductQueryParams
{
    [FromQuery(Name = "limit")]
    public int Limit { get; set; } = 20;

    [FromQuery(Name = "offset")]
    public int Offset { get; set; }

    [FromQuery(Name = "department")]
    public string? Department { get; set; }
}
