using Enrich.BLL.Common;
using Enrich.BLL.DTOs;
using Enrich.BLL.Services;
using Enrich.DAL.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Moq;
using NUnit.Framework;

namespace Enrich.UnitTests.Services
{
    [TestFixture]
    public class AuthServiceTests
    {
        private Mock<UserManager<User>> _userManagerMock = null!;
        private Mock<SignInManager<User>> _signInManagerMock = null!;
        private AuthService _authService = null!;

        [SetUp]
        public void SetUp()
        {
            var userStoreMock = new Mock<IUserStore<User>>();
            _userManagerMock = new Mock<UserManager<User>>(
                userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

            var contextAccessorMock = new Mock<IHttpContextAccessor>();
            var claimsFactoryMock = new Mock<IUserClaimsPrincipalFactory<User>>();

            _signInManagerMock = new Mock<SignInManager<User>>(
                _userManagerMock.Object,
                contextAccessorMock.Object,
                claimsFactoryMock.Object,
                null!, null!, null!, null!);

            _authService = new AuthService(_userManagerMock.Object, _signInManagerMock.Object);
        }

        [Test]
        public async Task LoginAsync_WithValidCredentials_ReturnsSuccess()
        {
            // Arrange
            var dto = new LoginDTO { Email = "test@test.com", Password = "Password123!", RememberMe = false };
            var user = new User { UserName = "TestUser", Email = dto.Email };

            _userManagerMock
                .Setup(u => u.FindByEmailAsync(dto.Email))
                .ReturnsAsync(user);

            _signInManagerMock
                .Setup(s => s.PasswordSignInAsync(user.UserName, dto.Password, dto.RememberMe, false))
                .ReturnsAsync(SignInResult.Success);

            // Act
            var result = await _authService.LoginAsync(dto);

            // Assert
            Assert.That(result.IsSuccess, Is.True);
            _signInManagerMock.Verify(s => s.PasswordSignInAsync(user.UserName, dto.Password, dto.RememberMe, false), Times.Once);
        }

        [Test]
        public async Task LoginAsync_WithInvalidEmail_ReturnsFailed()
        {
            // Arrange
            var dto = new LoginDTO { Email = "wrong@test.com", Password = "Password123!", RememberMe = false };

            _userManagerMock
                .Setup(u => u.FindByEmailAsync(dto.Email))
                .ReturnsAsync((User)null!);

            // Act
            var result = await _authService.LoginAsync(dto);

            // Assert
            Assert.That(result.IsSuccess, Is.False);
        }

        [Test]
        public async Task LogoutAsync_WhenCalled_InvokesSignOut()
        {
            // Act
            await _authService.LogoutAsync();

            // Assert
            _signInManagerMock.Verify(s => s.SignOutAsync(), Times.Once);
        }

        [Test]
        public async Task RegisterUserAsync_WhenCalled_CreatesUserWithCorrectProperties()
        {
            // Arrange
            var dto = new UserSignupDTO { Email = "test@example.com", Username = "TestUser", Password = "Password123!" };

            _userManagerMock
                .Setup(u => u.CreateAsync(It.IsAny<User>(), dto.Password))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            await _authService.RegisterUserAsync(dto);

            // Assert
            _userManagerMock.Verify(u => u.CreateAsync(It.Is<User>(user => user.UserName == dto.Username && user.Email == dto.Email), dto.Password), Times.Once);
        }
    }
}