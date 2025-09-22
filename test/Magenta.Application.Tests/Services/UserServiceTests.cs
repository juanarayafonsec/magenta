// File: test/Magenta.Application.Tests/Services/UserServiceTests.cs

using Magenta.Application.DTOs;
using Magenta.Application.Services;
using Magenta.Domain.Entities;
using Magenta.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace Magenta.Application.Tests.Services;

/// <summary>
/// Unit tests for the UserService class.
/// Tests user registration functionality and validation.
/// </summary>
public class UserServiceTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IPasswordHasher<User>> _mockPasswordHasher;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockPasswordHasher = new Mock<IPasswordHasher<User>>();
        _userService = new UserService(_mockUserRepository.Object, _mockPasswordHasher.Object);
    }

    [Fact]
    public async Task RegisterUserAsync_ValidRequest_ReturnsSuccessResponse()
    {
        // Arrange
        var request = new RegisterUserRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "password123",
            ConfirmPassword = "password123"
        };

        var user = new User
        {
            UserName = request.Username,
            Email = request.Email
        };

        _mockUserRepository.Setup(x => x.UsernameExistsAsync(request.Username, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockUserRepository.Setup(x => x.EmailExistsAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockPasswordHasher.Setup(x => x.HashPassword(It.IsAny<User>(), request.Password))
            .Returns("hashed-password");
        _mockUserRepository.Setup(x => x.CreateAsync(It.IsAny<User>(), request.Password, It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _userService.RegisterUserAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.UserId);
        Assert.Equal(request.Username, result.Username);
        Assert.Equal(request.Email, result.Email);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task RegisterUserAsync_UsernameAlreadyExists_ReturnsFailureResponse()
    {
        // Arrange
        var request = new RegisterUserRequest
        {
            Username = "existinguser",
            Email = "test@example.com",
            Password = "password123",
            ConfirmPassword = "password123"
        };

        _mockUserRepository.Setup(x => x.UsernameExistsAsync(request.Username, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _userService.RegisterUserAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Username is already taken.", result.Errors);
        Assert.Null(result.UserId);
    }

    [Fact]
    public async Task RegisterUserAsync_EmailAlreadyExists_ReturnsFailureResponse()
    {
        // Arrange
        var request = new RegisterUserRequest
        {
            Username = "testuser",
            Email = "existing@example.com",
            Password = "password123",
            ConfirmPassword = "password123"
        };

        _mockUserRepository.Setup(x => x.UsernameExistsAsync(request.Username, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockUserRepository.Setup(x => x.EmailExistsAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _userService.RegisterUserAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Email is already registered.", result.Errors);
        Assert.Null(result.UserId);
    }

    [Fact]
    public async Task RegisterUserAsync_InvalidEmail_ReturnsFailureResponse()
    {
        // Arrange
        var request = new RegisterUserRequest
        {
            Username = "testuser",
            Email = "invalid-email",
            Password = "password123",
            ConfirmPassword = "password123"
        };

        // Act
        var result = await _userService.RegisterUserAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Please provide a valid email address.", result.Errors);
    }

    [Fact]
    public async Task RegisterUserAsync_ShortPassword_ReturnsFailureResponse()
    {
        // Arrange
        var request = new RegisterUserRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "123",
            ConfirmPassword = "123"
        };

        // Act
        var result = await _userService.RegisterUserAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Password must be between 6 and 100 characters.", result.Errors);
    }

    [Fact]
    public async Task RegisterUserAsync_PasswordMismatch_ReturnsFailureResponse()
    {
        // Arrange
        var request = new RegisterUserRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "password123",
            ConfirmPassword = "differentpassword"
        };

        // Act
        var result = await _userService.RegisterUserAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Password and confirmation password do not match.", result.Errors);
    }

    [Fact]
    public async Task RegisterUserAsync_InvalidUsername_ReturnsFailureResponse()
    {
        // Arrange
        var request = new RegisterUserRequest
        {
            Username = "ab", // Too short
            Email = "test@example.com",
            Password = "password123",
            ConfirmPassword = "password123"
        };

        // Act
        var result = await _userService.RegisterUserAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Username must be between 3 and 50 characters.", result.Errors);
    }

    [Fact]
    public async Task RegisterUserAsync_UsernameWithInvalidCharacters_ReturnsFailureResponse()
    {
        // Arrange
        var request = new RegisterUserRequest
        {
            Username = "test@user!", // Invalid character
            Email = "test@example.com",
            Password = "password123",
            ConfirmPassword = "password123"
        };

        // Act
        var result = await _userService.RegisterUserAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Username can only contain letters, numbers, hyphens, and underscores.", result.Errors);
    }

    [Fact]
    public async Task RegisterUserAsync_EmptyUsername_ReturnsFailureResponse()
    {
        // Arrange
        var request = new RegisterUserRequest
        {
            Username = "",
            Email = "test@example.com",
            Password = "password123",
            ConfirmPassword = "password123"
        };

        // Act
        var result = await _userService.RegisterUserAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Username is required.", result.Errors);
    }

    [Fact]
    public async Task RegisterUserAsync_EmptyEmail_ReturnsFailureResponse()
    {
        // Arrange
        var request = new RegisterUserRequest
        {
            Username = "testuser",
            Email = "",
            Password = "password123",
            ConfirmPassword = "password123"
        };

        // Act
        var result = await _userService.RegisterUserAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Email is required.", result.Errors);
    }

    [Fact]
    public async Task RegisterUserAsync_EmptyPassword_ReturnsFailureResponse()
    {
        // Arrange
        var request = new RegisterUserRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "",
            ConfirmPassword = ""
        };

        // Act
        var result = await _userService.RegisterUserAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Password is required.", result.Errors);
    }

    [Fact]
    public async Task RegisterUserAsync_EmptyConfirmPassword_ReturnsFailureResponse()
    {
        // Arrange
        var request = new RegisterUserRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "password123",
            ConfirmPassword = ""
        };

        // Act
        var result = await _userService.RegisterUserAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Password confirmation is required.", result.Errors);
    }
}
