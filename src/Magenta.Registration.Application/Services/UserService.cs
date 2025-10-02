using Magenta.Registration.Application.DTOs;
using Magenta.Registration.Application.Interfaces;
using Magenta.Registration.Application.Events;
using Magenta.Registration.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Magenta.Registration.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IEventPublisher _eventPublisher;

    public UserService(IUserRepository userRepository, IPasswordHasher<User> passwordHasher, IEventPublisher eventPublisher)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
    }

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
            // Publish user created event
            var userCreatedEvent = new UserCreatedEvent
            {
                UserId = user.Id,
                Username = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                CreatedAt = user.CreatedAt,
                PasswordHash = user.PasswordHash ?? string.Empty,
                SecurityStamp = user.SecurityStamp ?? string.Empty,
                EmailConfirmed = user.EmailConfirmed,
                LockoutEnabled = user.LockoutEnabled,
                LockoutEnd = user.LockoutEnd
            };

            try
            {
                await _eventPublisher.PublishAsync(userCreatedEvent, cancellationToken);
            }
            catch (Exception ex)
            {
                // Log error but don't fail the registration
                // In a production system, you might want to implement retry logic or dead letter queues
                Console.WriteLine($"Failed to publish UserCreatedEvent: {ex.Message}");
            }

            return RegisterUserResponse.SuccessResponse(user.Id, user.UserName, user.Email);
        }

        // Convert Identity errors to response
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
