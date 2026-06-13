using ORUApi.Models;

namespace ORUApi.Endpoints.Health;

public static class HealthEndpoints
{
    public static void MapHealthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/health").WithTags("Health");
        group.MapGet("/basic", () =>
            Results.Ok(ApiResponse.Ok(new { Status = "Ok", Message = "Basic System Ok" })));
    }
}
