namespace ORUApi.Models;

public record ApiResponse<T>(bool Success, string Message, T? Data);

public static class ApiResponse
{
    public static ApiResponse<T> Ok<T>(T data, string message = "Success") =>
        new(true, message, data);

    public static ApiResponse<object?> Error(string message) =>
        new(false, message, null);
}
