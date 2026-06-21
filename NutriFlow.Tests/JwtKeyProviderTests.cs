using Xunit;
using Microsoft.Extensions.Configuration;
using NutriFlow.Services;
using System.Collections.Generic;
using System;

namespace NutriFlow.Tests
{
    public class JwtKeyProviderTests
    {
        [Fact]
        public void GetKey_WhenEnvironmentVariableIsSet_ReturnsEnvironmentKey()
        {
            // Arrange
            var envKey = "env-secret-key-that-is-at-least-32-chars-long";
            Environment.SetEnvironmentVariable("JWT_SECRET_KEY", envKey);

            var myConfiguration = new Dictionary<string, string?>
            {
                {"Jwt:Key", "config-secret-key-that-is-at-least-32-chars-long"}
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(myConfiguration).Build();

            try
            {
                // Act
                var keyBytes = JwtKeyProvider.GetKey(config);
                var keyStr = System.Text.Encoding.UTF8.GetString(keyBytes);

                // Assert
                Assert.Equal(envKey, keyStr);
            }
            finally
            {
                // Cleanup environment
                Environment.SetEnvironmentVariable("JWT_SECRET_KEY", null);
            }
        }

        [Fact]
        public void GetKey_WhenEnvVarNotSetAndConfigKeyIsSet_ReturnsConfigKey()
        {
            // Arrange
            Environment.SetEnvironmentVariable("JWT_SECRET_KEY", null);

            var configKey = "config-secret-key-that-is-at-least-32-chars-long";
            var myConfiguration = new Dictionary<string, string?>
            {
                {"Jwt:Key", configKey}
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(myConfiguration).Build();

            // Act
            var keyBytes = JwtKeyProvider.GetKey(config);
            var keyStr = System.Text.Encoding.UTF8.GetString(keyBytes);

            // Assert
            Assert.Equal(configKey, keyStr);
        }

        [Fact]
        public void GetKey_WhenBothNotSet_ThrowsInvalidOperationException()
        {
            // Arrange
            Environment.SetEnvironmentVariable("JWT_SECRET_KEY", null);

            var myConfiguration = new Dictionary<string, string?>
            {
                {"Jwt:Key", null}
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(myConfiguration).Build();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => JwtKeyProvider.GetKey(config));
        }
    }
}
