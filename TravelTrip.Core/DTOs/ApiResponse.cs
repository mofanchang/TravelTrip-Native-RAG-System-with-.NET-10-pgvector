namespace TravelTrip.Core.DTOs;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public int? Count { get; set; }

    public static ApiResponse<T> SuccessResult(T data, string message = "操作成功", int? count = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data,
            Count = count ?? (data is System.Collections.IEnumerable enumerable ? CountEnumerable(enumerable) : null)
        };
    }

    public static ApiResponse<T> FailureResult(string message)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message
        };
    }

    private static int CountEnumerable(System.Collections.IEnumerable enumerable)
    {
        int count = 0;
        foreach (var _ in enumerable) count++;
        return count;
    }
}
