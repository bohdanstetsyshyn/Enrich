using Enrich.BLL.Common;
using Enrich.BLL.DTOs;
using Enrich.BLL.Interfaces;
using Enrich.Web.Controllers;
using Enrich.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Enrich.UnitTests.Controllers
{
    [TestFixture]
    public class CategoryControllerTests
    {
        private Mock<ICategoryService> _categoryServiceMock = null!;
        private Mock<ILogger<CategoryController>> _loggerMock = null!;
        private CategoryController _controller = null!;

        [SetUp]
        public void SetUp()
        {
            _categoryServiceMock = new Mock<ICategoryService>();
            _loggerMock = new Mock<ILogger<CategoryController>>();
            _controller = new CategoryController(_categoryServiceMock.Object, _loggerMock.Object);
        }

        [Test]
        public async Task Create_Post_WithValidModel_RedirectsToIndex()
        {
            // Arrange
            var model = new CategoryViewModel { Name = "Test Category" };
            _categoryServiceMock.Setup(s => s.CreateCategoryAsync(It.IsAny<CategoryDTO>()))
                .ReturnsAsync(Result.Success());

            // Act
            var result = await _controller.Create(model);

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
            var redirect = (RedirectToActionResult)result;
            Assert.That(redirect.ActionName, Is.EqualTo("Index"));
        }

        [Test]
        public async Task Edit_Get_WhenNotFound_ReturnsNotFound()
        {
            // Arrange
            _categoryServiceMock.Setup(s => s.GetCategoryByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((CategoryDTO?)null);

            // Act
            var result = await _controller.Edit(1);

            // Assert
            Assert.That(result, Is.InstanceOf<NotFoundResult>());
        }
    }
}