using Magenta.Application.DTOs;
using Magenta.Application.Interfaces;
using Magenta.Domain.Entities;
using Magenta.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Magenta.Application.Services;

/// <summary>
/// Service implementation for user-related operations.
/// Handles user registration business logic and validation.
/// </summary>
public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher<User> _passwordHasher;

    public UserService(IUserRepository userRepository, IPasswordHasher<User> passwordHasher)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
    }

    public async Task<RegisterUserResponse> RegisterUserAsync(RegisterUserRequest request, CancellationToken cancellationToken = default)
    {
        var validationErrors = ValidateRegistrationRequest(request);
        if (validationErrors.Any())
        {
            return RegisterUserResponse.FailureResponse(validationErrors);
        }

        if (await _userRepository.UsernameExistsAsync(request.Username, cancellationToken))
        {
            return RegisterUserResponse.FailureResponse("Username is already taken.");
        }

        if (await _userRepository.EmailExistsAsync(request.Email, cancellationToken))
        {
            return RegisterUserResponse.FailureResponse("Email is already registered.");
        }

        var user = new User
        {
            UserName = request.Username,
            Email = request.Email,
            EmailConfirmed = false, // Email confirmation can be implemented later
            CreatedAt = DateTime.UtcNow
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        var result = await _userRepository.CreateAsync(user, request.Password, cancellationToken);

        if (result.Succeeded)
        {
            return RegisterUserResponse.SuccessResponse(user.Id, user.UserName, user.Email);
        }

        var errors = result.Errors.Select(e => e.Description).ToList();
        return RegisterUserResponse.FailureResponse(errors);
    }

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
