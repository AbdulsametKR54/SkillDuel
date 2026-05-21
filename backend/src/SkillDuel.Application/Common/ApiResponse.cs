namespace SkillDuel.Application.Common;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Error { get; set; }

    public static ApiResponse<T> SuccessResult(T data) => new() { Success = true, Data = data };
    public static ApiResponse<T> FailureResult(string error) => new() { Success = false, Error = error };
}

public class ApiResponse
{
    public bool Success { get; set; }
    public string? Error { get; set; }

    public static ApiResponse SuccessResult() => new() { Success = true };
    public static ApiResponse FailureResult(string error) => new() { Success = false, Error = error };
}
