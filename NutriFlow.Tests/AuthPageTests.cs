using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MudBlazor;
using MudBlazor.Services;
using NutriFlow.Components.Pages;
using NutriFlow.Services;
using NutriFlow.Utils;
using Microsoft.AspNetCore.Components.Web;
using System.Threading.Tasks;
using Xunit;
using System;
using System.Linq;

namespace NutriFlow.Tests
{
    public class AuthPageTests : BunitContext
    {
        public AuthPageTests()
        {
            JSInterop.Mode = JSRuntimeMode.Loose;
            Services.AddMudServices(options =>
            {
                options.PopoverOptions.CheckForPopoverProvider = false;
            });
        }

        [Fact]
        public void Cadastro_RendersAndValidatesEmail()
        {
            // Arrange
            var authServiceMock = new Mock<IAuthService>();
            Services.AddSingleton(authServiceMock.Object);
            Services.AddSingleton(Mock.Of<ISnackbar>());

            // Act
            var cut = Render<Cadastro>();

            // Assert page renders
            Assert.Contains("Criar Conta", cut.Markup);

            // Test email field validation
            var emailInput = cut.Find("input[type='email']");
            Assert.NotNull(emailInput);

            // Set invalid email to trigger validation
            emailInput.Input("invalid-email");
            emailInput.Change("invalid-email");

            // Click the register button to trigger form validation
            var registerButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Cadastrar"));
            Assert.NotNull(registerButton);
            registerButton.Click();

            // Verify error message is in markup
            Assert.Contains("Informe um email válido", cut.Markup);
        }

        [Fact]
        public async Task Cadastro_HandleKeyUp_Enter_TriggersRegistration()
        {
            // Arrange
            var authServiceMock = new Mock<IAuthService>();
            Services.AddSingleton(authServiceMock.Object);
            Services.AddSingleton(Mock.Of<ISnackbar>());

            var cut = Render<Cadastro>();

            // Find name, email, and password input fields and set valid data
            var inputs = cut.FindAll("input");
            var nameInput = inputs[0];
            var emailInput = inputs[1];
            var passwordInput = inputs[2];

            nameInput.Input("Test User");
            nameInput.Change("Test User");
            emailInput.Input("test@example.com");
            emailInput.Change("test@example.com");
            passwordInput.Input("password123");
            passwordInput.Change("password123");

            // Trigger KeyUp with Enter key on password field
            await passwordInput.KeyUpAsync(new KeyboardEventArgs { Key = "Enter" });

            // Verify that AuthService.RegisterAsync was called
            authServiceMock.Verify(x => x.RegisterAsync(It.Is<Models.Usuario>(u => u.Email == "test@example.com")), Times.Once);
        }

        [Fact]
        public void Login_RendersAndValidatesEmail()
        {
            // Arrange
            Services.AddSingleton(Mock.Of<ISnackbar>());

            // Act
            var cut = Render<Login>();

            // Assert page renders
            Assert.Contains("Bem-vindo(a)", cut.Markup);

            // Test email field validation
            var emailInput = cut.Find("input[type='email']");
            Assert.NotNull(emailInput);

            // Set invalid email to trigger validation
            emailInput.Input("invalid-email");
            emailInput.Change("invalid-email");

            // Click the login button to trigger form validation
            var loginButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Entrar"));
            Assert.NotNull(loginButton);
            loginButton.Click();

            // Verify error message is in markup
            Assert.Contains("Informe um email válido", cut.Markup);
        }

        [Fact]
        public async Task Login_HandleKeyUp_Enter_TriggersLogin()
        {
            // Arrange
            Services.AddSingleton(Mock.Of<ISnackbar>());

            // Set up JSInterop mock for submitLoginForm
            var jsMock = JSInterop.SetupVoid("submitLoginForm", "test@example.com", "password123");

            var cut = Render<Login>();

            // Find email and password inputs and fill them
            var emailInput = cut.Find("input[type='email']");
            var passwordInput = cut.Find("input[type='password']");

            emailInput.Input("test@example.com");
            emailInput.Change("test@example.com");
            passwordInput.Input("password123");
            passwordInput.Change("password123");

            // Trigger KeyUp with Enter key on password field
            await passwordInput.KeyUpAsync(new KeyboardEventArgs { Key = "Enter" });

            // Verify JS Interop called for submitLoginForm
            jsMock.VerifyInvoke("submitLoginForm");
        }
    }
}
