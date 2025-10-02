using System.ComponentModel.DataAnnotations;

namespace Magenta.Infrastructure.Configuration;

/// <summary>
/// Configuration class for RabbitMQ settings that can be deserialized from JSON.
/// </summary>
public class RabbitMQConfiguration
{
    /// <summary>
    /// RabbitMQ host name or IP address.
    /// </summary>
    [Required]
    public string Host { get; set; } = "localhost";

    /// <summary>
    /// RabbitMQ port number.
    /// </summary>
    [Range(1, 65535)]
    public int Port { get; set; } = 5672;

    /// <summary>
    /// RabbitMQ username for authentication.
    /// </summary>
    [Required]
    public string Username { get; set; } = "guest";

    /// <summary>
    /// RabbitMQ password for authentication.
    /// </summary>
    [Required]
    public string Password { get; set; } = "guest";

    /// <summary>
    /// RabbitMQ virtual host.
    /// </summary>
    public string VirtualHost { get; set; } = "/";

    /// <summary>
    /// RabbitMQ exchange name for publishing events.
    /// </summary>
    [Required]
    public string ExchangeName { get; set; } = "magenta.events";

    /// <summary>
    /// RabbitMQ queue name for consuming events (optional, used by subscribers).
    /// </summary>
    public string? QueueName { get; set; }

    /// <summary>
    /// Enable automatic recovery for RabbitMQ connections.
    /// </summary>
    public bool AutomaticRecoveryEnabled { get; set; } = true;

    /// <summary>
    /// Network recovery interval in seconds.
    /// </summary>
    public int NetworkRecoveryIntervalSeconds { get; set; } = 10;

    /// <summary>
    /// Validates the configuration and returns any validation errors.
    /// </summary>
    /// <returns>A list of validation errors, or empty list if valid.</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Host))
            errors.Add("Host is required");

        if (Port < 1 || Port > 65535)
            errors.Add("Port must be between 1 and 65535");

        if (string.IsNullOrWhiteSpace(Username))
            errors.Add("Username is required");

        if (string.IsNullOrWhiteSpace(Password))
            errors.Add("Password is required");

        if (string.IsNullOrWhiteSpace(ExchangeName))
            errors.Add("ExchangeName is required");

        if (NetworkRecoveryIntervalSeconds < 1)
            errors.Add("NetworkRecoveryIntervalSeconds must be greater than 0");

        return errors;
    }
}

