// File: src/Magenta.Registration.Application/Services/UserService.cs

using Magenta.Registration.Application.DTOs;
using Magenta.Registration.Application.Interfaces;
using Magenta.Registration.Domain.Entities;
using Magenta.Registration.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Magenta.Registration.Application.Services;

/// <summary>
/// Service implementation for user-related operations.
/// Handles user registration business logic and validation.
/// </summary>
public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher<User> _passwordHasher;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserService"/> class.
    /// </summary>
    /// <param name="userRepository">The user repository.</param>
    /// <param name="passwordHasher">The password hasher.</param>
    public UserService(IUserRepository userRepository, IPasswordHasher<User> passwordHasher)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
    }

    /// <summary>
    /// Registers a new user asynchronously.
    /// </summary>
    /// <param name="request">The registration request containing user details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the registration response.</returns>
    public async Task<RegisterUserResponse> RegisterUserAsync(RegisterUserRequest request, CancellationToken cancellationToken = default)
    {
        // Validate input
        var validationErrors = ValidateRegistrationRequest(request);
        if (validationErrors.Any())
        {
            return RegisterUserResponse.FailureResponse(validationErrors);
        }

        // Check if username already exists
        if (await _userRepository.UsernameExistsAsync(request.Username, cancellationToken))
        {
            return RegisterUserResponse.FailureResponse("Username is already taken.");
        }

        // Check if email already exists
        if (await _userRepository.EmailExistsAsync(request.Email, cancellationToken))
        {
            return RegisterUserResponse.FailureResponse("Email is already registered.");
        }

        // Create new user
        var user = new User
        {
            UserName = request.Username,
            Email = request.Email,
            EmailConfirmed = true, // No email confirmation required
            CreatedAt = DateTime.UtcNow
        };

        // Hash password
        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        // Save user
        var result = await _userRepository.CreateAsync(user, request.Password, cancellationToken);

        if (result.Succeeded)
        {
            return RegisterUserResponse.SuccessResponse(user.Id, user.UserName, user.Email);
        }

        // Convert Identity errors to response
        var errors = result.Errors.Select(e => e.Description).ToList();
        return RegisterUserResponse.FailureResponse(errors);
    }

    /// <summary>
    /// Validates the registration request.
    /// </summary>
    /// <param name="request">The registration request to validate.</param>
    /// <returns>A list of validation errors.</returns>
    private static List<string> ValidateRegistrationRequest(RegisterUserRequest request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.Username))
        {
            errors.Add("Username is required.");
        }
        else if (request.Username.Length < 3 || request.Username.Length > 50)
        {
            errors.Add("Username must be between 3 and 50 characters.");
        }
        else if (!System.Text.RegularExpressions.Regex.IsMatch(request.Username, @"^[a-zA-Z0-9_-]+$"))
        {
            errors.Add("Username can only contain letters, numbers, hyphens, and underscores.");
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            errors.Add("Email is required.");
        }
        else if (!IsValidEmail(request.Email))
        {
            errors.Add("Please provide a valid email address.");
        }
        else if (request.Email.Length > 256)
        {
            errors.Add("Email cannot exceed 256 characters.");
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            errors.Add("Password is required.");
        }
        else if (request.Password.Length < 6 || request.Password.Length > 100)
        {
            errors.Add("Password must be between 6 and 100 characters.");
        }

        if (string.IsNullOrWhiteSpace(request.ConfirmPassword))
        {
            errors.Add("Password confirmation is required.");
        }
        else if (request.Password != request.ConfirmPassword)
        {
            errors.Add("Password and confirmation password do not match.");
        }

        return errors;
    }

    /// <summary>
    /// Validates email format using a simple regex pattern.
    /// </summary>
    /// <param name="email">The email to validate.</param>
    /// <returns>True if the email format is valid, otherwise false.</returns>
    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
