using System.ComponentModel.DataAnnotations;

namespace Magenta.Registration.Infrastructure.Configuration;

public class RabbitMQConfiguration
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    public string ExchangeName { get; set; } = "magenta.events";
    public string QueueName { get; set; } = "registration.user.events";
    public bool AutomaticRecoveryEnabled { get; set; } = true;
    public int NetworkRecoveryIntervalSeconds { get; set; } = 5;

    public List<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Host))
            errors.Add("Host is required");

        if (Port <= 0 || Port > 65535)
            errors.Add("Port must be between 1 and 65535");

        if (string.IsNullOrWhiteSpace(Username))
            errors.Add("Username is required");

        if (string.IsNullOrWhiteSpace(Password))
            errors.Add("Password is required");

        if (string.IsNullOrWhiteSpace(ExchangeName))
            errors.Add("ExchangeName is required");

        if (string.IsNullOrWhiteSpace(QueueName))
            errors.Add("QueueName is required");

        return errors;
    }
}
