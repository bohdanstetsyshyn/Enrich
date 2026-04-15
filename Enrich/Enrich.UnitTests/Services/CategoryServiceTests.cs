using Enrich.BLL.DTOs;
using Enrich.BLL.Services;
using Enrich.DAL.Entities;
using Enrich.DAL.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Enrich.UnitTests.Services
{
    [TestFixture]
    public class CategoryServiceTests
    {
        private Mock<ICategoryRepository> _categoryRepositoryMock = null!;
        private Mock<ILogger<CategoryService>> _loggerMock = null!;
        private CategoryService _categoryService = null!;

        [SetUp]
        public void SetUp()
        {
            _categoryRepositoryMock = new Mock<ICategoryRepository>();
            _loggerMock = new Mock<ILogger<CategoryService>>();
            _categoryService = new CategoryService(_categoryRepositoryMock.Object, _loggerMock.Object);
        }

        [Test]
        public async Task CreateCategoryAsync_WithValidName_ReturnsSuccess()
        {
            // Arrange
            var dto = new CategoryDTO { Name = "New Category" };
            _categoryRepositoryMock.Setup(r => r.GetCategoryByNameAsync(It.IsAny<string>()))
                .ReturnsAsync((Category?)null);

            // Act
            var result = await _categoryService.CreateCategoryAsync(dto);

            // Assert
            Assert.That(result.IsSuccess, Is.True);
            _categoryRepositoryMock.Verify(r => r.CreateCategoryAsync(It.Is<Category>(c => c.Name == "New Category")), Times.Once);
        }

        [Test]
        public async Task CreateCategoryAsync_WithDuplicateName_ReturnsFailure()
        {
            // Arrange
            var dto = new CategoryDTO { Name = "Existing" };
            _categoryRepositoryMock.Setup(r => r.GetCategoryByNameAsync("Existing"))
                .ReturnsAsync(new Category { Id = 1, Name = "Existing" });

            // Act
            var result = await _categoryService.CreateCategoryAsync(dto);

            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorMessage, Contains.Substring("already exists"));
        }

        [Test]
        public async Task UpdateCategoryAsync_WhenCategoryNotFound_ReturnsFailure()
        {
            // Arrange
            var dto = new CategoryDTO { Id = 999, Name = "Updated" };
            _categoryRepositoryMock.Setup(r => r.GetCategoryByIdAsync(999)).ReturnsAsync((Category?)null);

            // Act
            var result = await _categoryService.UpdateCategoryAsync(dto);

            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorMessage, Is.EqualTo("Category not found."));
        }

        [Test]
        public async Task DeleteCategoryAsync_WithValidId_ReturnsSuccess()
        {
            // Arrange
            var categoryId = 1;
            var category = new Category { Id = categoryId, Name = "To Delete" };
            _categoryRepositoryMock.Setup(r => r.GetCategoryByIdAsync(categoryId)).ReturnsAsync(category);

            // Act
            var result = await _categoryService.DeleteCategoryAsync(categoryId);

            // Assert
            Assert.That(result.IsSuccess, Is.True);
            _categoryRepositoryMock.Verify(r => r.DeleteCategoryAsync(categoryId), Times.Once);
        }
    }
}