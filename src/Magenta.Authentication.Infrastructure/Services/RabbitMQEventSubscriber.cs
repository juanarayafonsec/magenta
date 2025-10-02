using Magenta.Authentication.Application.Interfaces;
using Magenta.Authentication.Domain.Entities;
using Magenta.Authentication.Infrastructure.Events;
using Magenta.Infrastructure.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace Magenta.Authentication.Infrastructure.Services;

public class RabbitMQEventSubscriber : IEventSubscriber, IHostedService, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly ILogger<RabbitMQEventSubscriber> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly string _exchangeName;
    private readonly string _queueName;

    public RabbitMQEventSubscriber(
        IOptions<RabbitMQConfiguration> rabbitMqOptions,
        ILogger<RabbitMQEventSubscriber> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        
        var config = rabbitMqOptions.Value;
        
        // Validate configuration
        var validationErrors = config.Validate();
        if (validationErrors.Any())
        {
            var errorMessage = string.Join(", ", validationErrors);
            throw new InvalidOperationException($"Invalid RabbitMQ configuration: {errorMessage}");
        }

        _exchangeName = config.ExchangeName;
        _queueName = config.QueueName ?? "authentication.user.events";

        var factory = new ConnectionFactory
        {
            HostName = config.Host,
            Port = config.Port,
            UserName = config.Username,
            Password = config.Password,
            VirtualHost = config.VirtualHost,
            AutomaticRecoveryEnabled = config.AutomaticRecoveryEnabled,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(config.NetworkRecoveryIntervalSeconds)
        };

        try
        {
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            
            // Declare the exchange
            _channel.ExchangeDeclare(
                exchange: _exchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false);

            // Declare the queue
            _channel.QueueDeclare(
                queue: _queueName,
                durable: true,
                exclusive: false,
                autoDelete: false);

            // Bind the queue to the exchange with routing patterns
            _channel.QueueBind(
                queue: _queueName,
                exchange: _exchangeName,
                routingKey: "user.created");
            
            _channel.QueueBind(
                queue: _queueName,
                exchange: _exchangeName,
                routingKey: "user.updated");
            
            _channel.QueueBind(
                queue: _queueName,
                exchange: _exchangeName,
                routingKey: "user.deleted");

            // Set QoS to process one message at a time
            _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

            _logger.LogInformation("Connected to RabbitMQ at {Host}:{Port} for event subscription", config.Host, config.Port);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to RabbitMQ at {Host}:{Port}", config.Host, config.Port);
            throw;
        }
    }

    /// <summary>
    /// Starts the event subscription.
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var routingKey = ea.RoutingKey;

                try
                {
                    await ProcessMessageAsync(routingKey, message);
                    _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message with routing key {RoutingKey}: {Message}", routingKey, message);
                    
                    // Reject the message and requeue it for retry
                    _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
                }
            };

            _channel.BasicConsume(
                queue: _queueName,
                autoAck: false,
                consumer: consumer);

            _logger.LogInformation("Started consuming events from queue {QueueName}", _queueName);
            
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start event subscription");
            throw;
        }
    }

    /// <summary>
    /// Stops the event subscription.
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _channel?.Close();
            _logger.LogInformation("Stopped consuming events from queue {QueueName}", _queueName);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping event subscription");
        }
    }

    /// <summary>
    /// Processes incoming messages based on routing key.
    /// </summary>
    private async Task ProcessMessageAsync(string routingKey, string message)
    {
        try
        {
            switch (routingKey)
            {
                case "user.created":
                    await HandleUserCreatedEventAsync(message);
                    break;
                case "user.updated":
                    await HandleUserUpdatedEventAsync(message);
                    break;
                case "user.deleted":
                    await HandleUserDeletedEventAsync(message);
                    break;
                default:
                    _logger.LogWarning("Unknown routing key: {RoutingKey}", routingKey);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message with routing key {RoutingKey}", routingKey);
            throw;
        }
    }

    /// <summary>
    /// Handles user created events by synchronizing user data to the Authentication service.
    /// </summary>
    private async Task HandleUserCreatedEventAsync(string message)
    {
        using var scope = _serviceProvider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AuthenticationUser>>();
        
        try
        {
            var userCreatedEvent = JsonSerializer.Deserialize<UserCreatedEvent>(message, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (userCreatedEvent == null)
            {
                _logger.LogWarning("Failed to deserialize UserCreatedEvent");
                return;
            }

            _logger.LogInformation("Processing UserCreatedEvent for user {UserId}", userCreatedEvent.UserId);

            // Check if user already exists
            var existingUser = await userManager.FindByIdAsync(userCreatedEvent.UserId);
            if (existingUser != null)
            {
                _logger.LogInformation("User {UserId} already exists in Authentication service", userCreatedEvent.UserId);
                return;
            }

                   // Create user in Authentication service
                   var user = new AuthenticationUser
            {
                Id = userCreatedEvent.UserId,
                UserName = userCreatedEvent.Username,
                Email = userCreatedEvent.Email,
                CreatedAt = userCreatedEvent.CreatedAt,
                PasswordHash = userCreatedEvent.PasswordHash,
                SecurityStamp = userCreatedEvent.SecurityStamp,
                EmailConfirmed = userCreatedEvent.EmailConfirmed,
                LockoutEnabled = userCreatedEvent.LockoutEnabled,
                LockoutEnd = userCreatedEvent.LockoutEnd
            };

            var result = await userManager.CreateAsync(user);
            if (result.Succeeded)
            {
                _logger.LogInformation("Successfully synchronized user {UserId} to Authentication service", userCreatedEvent.UserId);
            }
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError("Failed to synchronize user {UserId} to Authentication service: {Errors}", userCreatedEvent.UserId, errors);
                throw new InvalidOperationException($"Failed to create user: {errors}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling UserCreatedEvent: {Message}", message);
            throw;
        }
    }

    /// <summary>
    /// Handles user updated events by updating user data in the Authentication service.
    /// </summary>
    private async Task HandleUserUpdatedEventAsync(string message)
    {
        using var scope = _serviceProvider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AuthenticationUser>>();
        
        try
        {
            var userUpdatedEvent = JsonSerializer.Deserialize<UserUpdatedEvent>(message, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (userUpdatedEvent == null)
            {
                _logger.LogWarning("Failed to deserialize UserUpdatedEvent");
                return;
            }

            _logger.LogInformation("Processing UserUpdatedEvent for user {UserId}", userUpdatedEvent.UserId);

            var user = await userManager.FindByIdAsync(userUpdatedEvent.UserId);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found in Authentication service for update", userUpdatedEvent.UserId);
                return;
            }

            // Update user properties
            bool hasChanges = false;

            if (userUpdatedEvent.Username != null && user.UserName != userUpdatedEvent.Username)
            {
                user.UserName = userUpdatedEvent.Username;
                hasChanges = true;
            }

            if (userUpdatedEvent.Email != null && user.Email != userUpdatedEvent.Email)
            {
                user.Email = userUpdatedEvent.Email;
                hasChanges = true;
            }

            if (userUpdatedEvent.PasswordHash != null && user.PasswordHash != userUpdatedEvent.PasswordHash)
            {
                user.PasswordHash = userUpdatedEvent.PasswordHash;
                hasChanges = true;
            }

            if (userUpdatedEvent.SecurityStamp != null && user.SecurityStamp != userUpdatedEvent.SecurityStamp)
            {
                user.SecurityStamp = userUpdatedEvent.SecurityStamp;
                hasChanges = true;
            }

            if (userUpdatedEvent.EmailConfirmed.HasValue && user.EmailConfirmed != userUpdatedEvent.EmailConfirmed)
            {
                user.EmailConfirmed = userUpdatedEvent.EmailConfirmed.Value;
                hasChanges = true;
            }

            if (userUpdatedEvent.LockoutEnabled.HasValue && user.LockoutEnabled != userUpdatedEvent.LockoutEnabled)
            {
                user.LockoutEnabled = userUpdatedEvent.LockoutEnabled.Value;
                hasChanges = true;
            }

            if (userUpdatedEvent.LockoutEnd != null && user.LockoutEnd != userUpdatedEvent.LockoutEnd)
            {
                user.LockoutEnd = userUpdatedEvent.LockoutEnd;
                hasChanges = true;
            }

            if (userUpdatedEvent.UpdatedAt.HasValue)
            {
                user.UpdatedAt = userUpdatedEvent.UpdatedAt.Value;
                hasChanges = true;
            }

            if (hasChanges)
            {
                var result = await userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    _logger.LogInformation("Successfully updated user {UserId} in Authentication service", userUpdatedEvent.UserId);
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogError("Failed to update user {UserId} in Authentication service: {Errors}", userUpdatedEvent.UserId, errors);
                    throw new InvalidOperationException($"Failed to update user: {errors}");
                }
            }
            else
            {
                _logger.LogInformation("No changes detected for user {UserId}", userUpdatedEvent.UserId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling UserUpdatedEvent: {Message}", message);
            throw;
        }
    }

    /// <summary>
    /// Handles user deleted events by removing user data from the Authentication service.
    /// </summary>
    private async Task HandleUserDeletedEventAsync(string message)
    {
        using var scope = _serviceProvider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AuthenticationUser>>();
        
        try
        {
            var userDeletedEvent = JsonSerializer.Deserialize<UserDeletedEvent>(message, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (userDeletedEvent == null)
            {
                _logger.LogWarning("Failed to deserialize UserDeletedEvent");
                return;
            }

            _logger.LogInformation("Processing UserDeletedEvent for user {UserId}", userDeletedEvent.UserId);

            var user = await userManager.FindByIdAsync(userDeletedEvent.UserId);
            if (user == null)
            {
                _logger.LogInformation("User {UserId} not found in Authentication service for deletion", userDeletedEvent.UserId);
                return;
            }

            var result = await userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                _logger.LogInformation("Successfully deleted user {UserId} from Authentication service", userDeletedEvent.UserId);
            }
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError("Failed to delete user {UserId} from Authentication service: {Errors}", userDeletedEvent.UserId, errors);
                throw new InvalidOperationException($"Failed to delete user: {errors}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling UserDeletedEvent: {Message}", message);
            throw;
        }
    }

    /// <summary>
    /// Disposes the RabbitMQ connection and channel.
    /// </summary>
    public void Dispose()
    {
        try
        {
            _channel?.Close();
            _channel?.Dispose();
            _connection?.Close();
            _connection?.Dispose();
            _logger.LogInformation("Disposed RabbitMQ connection");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing RabbitMQ connection");
        }
    }
}

