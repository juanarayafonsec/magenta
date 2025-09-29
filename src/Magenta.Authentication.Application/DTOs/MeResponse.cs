namespace Magenta.Authentication.Application.DTOs;

/// <summary>
/// Response DTO for user profile information.
/// Contains the current user's claims and profile data.
/// </summary>
public class MeResponse
{
    /// <summary>
    /// Gets or sets a value indicating whether the request was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the user information.
    /// </summary>
    public UserInfo? User { get; set; }

    /// <summary>
    /// Gets or sets the user's claims.
    /// </summary>
    public List<ClaimInfo> Claims { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of errors if request failed.
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Creates a successful me response.
    /// </summary>
    /// <param name="user">The user information.</param>
    /// <param name="claims">The user's claims.</param>
    /// <returns>A successful me response.</returns>
    public static MeResponse SuccessResponse(UserInfo user, List<ClaimInfo> claims)
    {
        return new MeResponse
        {
            Success = true,
            User = user,
            Claims = claims
        };
    }

    /// <summary>
    /// Creates a failed me response.
    /// </summary>
    /// <param name="errors">The list of errors.</param>
    /// <returns>A failed me response.</returns>
    public static MeResponse FailureResponse(List<string> errors)
    {
        return new MeResponse
        {
            Success = false,
            Errors = errors
        };
    }

    /// <summary>
    /// Creates a failed me response with a single error.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed me response.</returns>
    public static MeResponse FailureResponse(string error)
    {
        return new MeResponse
        {
            Success = false,
            Errors = new List<string> { error }
        };
    }
}

/// <summary>
/// Claim information DTO.
/// Contains claim type and value information.
/// </summary>
public class ClaimInfo
{
    /// <summary>
    /// Gets or sets the claim type.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the claim value.
    /// </summary>
    public string Value { get; set; } = string.Empty;
}
