using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Mvc;
using LeadHub.Api.Models;

namespace LeadHub.Api;

public static class ControllerResultExtensions
{
    public static IActionResult ToActionResult<T>(
        this ControllerBase controller, Result<T, Error> result, int successStatusCode = StatusCodes.Status200OK)
    {
        if (result.IsSuccess)
            return new ObjectResult(ApiResponse<T>.Ok(result.Value)) { StatusCode = successStatusCode };

        var (statusCode, code) = ErrorCodeMapper.Map(result.Error.Code);
        return new ObjectResult(ApiResponse<T>.Fail(new ApiError(code, result.Error.Message))) { StatusCode = statusCode };
    }

    public static IActionResult ToActionResult(
        this ControllerBase controller, UnitResult<Error> result, int successStatusCode = StatusCodes.Status200OK)
    {
        if (result.IsSuccess)
            return new ObjectResult(ApiResponse<object?>.Ok(null)) { StatusCode = successStatusCode };

        var (statusCode, code) = ErrorCodeMapper.Map(result.Error.Code);
        return new ObjectResult(ApiResponse<object?>.Fail(new ApiError(code, result.Error.Message))) { StatusCode = statusCode };
    }
}
