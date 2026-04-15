using System.Security.Claims;
using Enrich.BLL.Common;
using Enrich.BLL.DTOs;
using Enrich.BLL.Interfaces;
using Enrich.BLL.Settings;
using Enrich.DAL.Entities;
using Enrich.DAL.Interfaces;
using Enrich.Web.Controllers;
using Enrich.Web.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;

namespace Enrich.UnitTests.Controllers
{
    [TestFixture]
    public class BundleControllerTests
    {
        private const string TestUserId = "test-user-1";

        private Mock<ILogger<BundleController>> _loggerMock = null!;
        private Mock<IBundleService> _bundleServiceMock = null!;
        private Mock<ICategoryRepository> _categoryRepositoryMock = null!;
        private Mock<IWordRepository> _wordRepositoryMock = null!;
        private Mock<IOptions<PaginationSettings>> _paginationOptionsMock = null!;

        private BundleController _controller = null!;

        [SetUp]
        public void SetUp()
        {
            _loggerMock = new Mock<ILogger<BundleController>>();
            _bundleServiceMock = new Mock<IBundleService>();
            _categoryRepositoryMock = new Mock<ICategoryRepository>();
            _wordRepositoryMock = new Mock<IWordRepository>();

            _paginationOptionsMock = new Mock<IOptions<PaginationSettings>>();
            _paginationOptionsMock.Setup(o => o.Value).Returns(new PaginationSettings());

            _controller = new BundleController(
                _loggerMock.Object,
                _bundleServiceMock.Object,
                _categoryRepositoryMock.Object,
                _wordRepositoryMock.Object,
                _paginationOptionsMock.Object);

            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, TestUserId) };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);

            var httpContext = new DefaultHttpContext { User = principal };

            var tempDataProvider = new Mock<ITempDataProvider>();
            var tempData = new TempDataDictionary(httpContext, tempDataProvider.Object);

            _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            _controller.TempData = tempData;
        }

        [TearDown]
        public void TearDown()
        {
            _controller?.Dispose();
        }

        [Test]
        public async Task Create_Get_ReturnsViewResultWithPopulatedViewModel()
        {
            var categories = new List<Category>
            {
                new Category { Id = 1, Name = "Fruits" },
                new Category { Id = 2, Name = "Animals" }
            };

            var words = new List<Word>
            {
                new Word { Id = 1, Term = "Apple", Translation = "Яблуко" },
                new Word { Id = 2, Term = "Banana", Translation = "Банан" }
            };

            _categoryRepositoryMock
                .Setup(r => r.GetAllCategoriesAsync())
                .ReturnsAsync(categories);

            _wordRepositoryMock
                .Setup(r => r.GetAllWordsAsync())
                .ReturnsAsync(words);

            var result = await _controller.Create();

            var viewResult = result as ViewResult;
            Assert.That(viewResult, Is.Not.Null);

            var model = viewResult!.Model as CreateBundleViewModel;
            Assert.That(model, Is.Not.Null);
            Assert.That(model!.Categories.Count, Is.EqualTo(2));
            Assert.That(model.Words.Count, Is.EqualTo(2));
            Assert.That(model.AvailableLevels.Count, Is.EqualTo(6));
            Assert.That(model.AvailableLevels, Contains.Item("A1"));
            Assert.That(model.AvailableLevels, Contains.Item("C2"));

            _categoryRepositoryMock.Verify(r => r.GetAllCategoriesAsync(), Times.Once);
            _wordRepositoryMock.Verify(r => r.GetAllWordsAsync(), Times.Once);
        }

        [Test]
        public async Task Create_Post_WithValidModel_RedirectsToIndex()
        {
            // Arrange
            var model = new CreateBundleViewModel
            {
                Title = "My English Bundle",
                Description = "Learning essential English words",
                CategoryIds = new List<int> { 1, 2 },
                WordIds = new List<int> { 1, 2, 3 },
                DifficultyLevels = new List<string> { "A1", "A2", "B1" }
            };

            _bundleServiceMock
                .Setup(s => s.CreateBundleAsync(TestUserId, It.IsAny<CreateBundleDTO>()))
                .ReturnsAsync(Result.Success());

            // Act
            var result = await _controller.Create(model);

            // Assert
            var redirectResult = result as RedirectToActionResult;
            Assert.That(redirectResult, Is.Not.Null);
            Assert.That(redirectResult!.ActionName, Is.EqualTo("Index"));

            _bundleServiceMock.Verify(
                s => s.CreateBundleAsync(
                    TestUserId,
                    It.Is<CreateBundleDTO>(dto =>
                        dto.Title == "My English Bundle" &&
                        dto.Description == "Learning essential English words" &&
                        dto.CategoryIds!.Contains(1) &&
                        dto.WordIds!.Contains(1))),
                Times.Once);
        }

        [Test]
        public async Task Create_Post_WithInvalidModelState_ReturnsViewWithReloadedData()
        {
            // Arrange
            var model = new CreateBundleViewModel
            {
                Title = "", // Invalid - empty title
                Description = "Learning essential English words"
            };

            _controller.ModelState.AddModelError("Title", "Title is required");

            var categories = new List<Category> { new Category { Id = 1, Name = "Fruits" } };
            var words = new List<Word> { new Word { Id = 1, Term = "Apple", Translation = "Яблуко" } };

            _categoryRepositoryMock
                .Setup(r => r.GetAllCategoriesAsync())
                .ReturnsAsync(categories);

            _wordRepositoryMock
                .Setup(r => r.GetAllWordsAsync())
                .ReturnsAsync(words);

            // Act
            var result = await _controller.Create(model);

            // Assert
            var viewResult = result as ViewResult;
            Assert.That(viewResult, Is.Not.Null);
            Assert.That(viewResult!.Model, Is.EqualTo(model));

            var returnedModel = viewResult.Model as CreateBundleViewModel;
            Assert.That(returnedModel!.Categories.Count, Is.EqualTo(1));
            Assert.That(returnedModel.Words.Count, Is.EqualTo(1));

            _bundleServiceMock.Verify(
                s => s.CreateBundleAsync(It.IsAny<string>(), It.IsAny<CreateBundleDTO>()),
                Times.Never);
        }

        [Test]
        public async Task Create_Post_WhenServiceReturnsError_ReturnsViewWithError()
        {
            // Arrange
            var model = new CreateBundleViewModel
            {
                Title = "Duplicate Bundle",
                Description = "This bundle already exists"
            };

            var errorMessage = "You already have a bundle with this name.";
            _bundleServiceMock
                .Setup(s => s.CreateBundleAsync(TestUserId, It.IsAny<CreateBundleDTO>()))
                .ReturnsAsync(Result.Failure(errorMessage));

            var categories = new List<Category> { new Category { Id = 1, Name = "Fruits" } };
            var words = new List<Word> { new Word { Id = 1, Term = "Apple", Translation = "Яблуко" } };

            _categoryRepositoryMock
                .Setup(r => r.GetAllCategoriesAsync())
                .ReturnsAsync(categories);

            _wordRepositoryMock
                .Setup(r => r.GetAllWordsAsync())
                .ReturnsAsync(words);

            // Act
            var result = await _controller.Create(model);

            // Assert
            var viewResult = result as ViewResult;
            Assert.That(viewResult, Is.Not.Null);

            var hasError = _controller.ModelState.Values
                .SelectMany(v => v.Errors)
                .Any(e => e.ErrorMessage.Contains("with this name"));
            Assert.That(hasError, Is.True);
        }

        [Test]
        public async Task Edit_Get_WithValidBundleId_ReturnsViewResultWithPopulatedViewModel()
        {
            // Arrange
            var bundleId = 1;
            var bundleDto = new BundleDTO
            {
                Id = bundleId,
                Title = "Test Bundle",
                Description = "Test Description",
                OwnerId = TestUserId,
                Status = "Draft",
                ImageUrl = "https://example.com/image.jpg",
                WordCount = 5,
                CategoryCount = 2
            };

            _bundleServiceMock
                .Setup(s => s.GetBundleByIdAsync(bundleId))
                .ReturnsAsync(bundleDto);

            var categories = new List<Category>
            {
                new Category { Id = 1, Name = "Fruits" },
                new Category { Id = 2, Name = "Animals" }
            };

            var words = new List<Word>
            {
                new Word { Id = 1, Term = "Apple", Translation = "Яблуко" },
                new Word { Id = 2, Term = "Banana", Translation = "Банан" }
            };

            _categoryRepositoryMock
                .Setup(r => r.GetAllCategoriesAsync())
                .ReturnsAsync(categories);

            _wordRepositoryMock
                .Setup(r => r.GetAllWordsAsync())
                .ReturnsAsync(words);

            // Act
            var result = await _controller.Edit(bundleId);

            // Assert
            var viewResult = result as ViewResult;
            Assert.That(viewResult, Is.Not.Null);

            var model = viewResult!.Model as EditBundleViewModel;
            Assert.That(model, Is.Not.Null);
            Assert.That(model!.Id, Is.EqualTo(bundleId));
            Assert.That(model.Title, Is.EqualTo("Test Bundle"));
            Assert.That(model.Categories.Count, Is.EqualTo(2));
            Assert.That(model.Words.Count, Is.EqualTo(2));
        }

        [Test]
        public async Task Edit_Get_WithNonExistentBundle_ReturnsNotFound()
        {
            // Arrange
            var bundleId = 999;

            _bundleServiceMock
                .Setup(s => s.GetBundleByIdAsync(bundleId))
                .ReturnsAsync((BundleDTO?)null);

            // Act
            var result = await _controller.Edit(bundleId);

            // Assert
            Assert.That(result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public async Task Edit_Get_WhenBundleBelongsToOtherUser_ReturnsForbid()
        {
            // Arrange
            var bundleId = 1;
            var bundleDto = new BundleDTO
            {
                Id = bundleId,
                Title = "Someone Else's Bundle",
                OwnerId = "other-user-id",
                Status = "Draft"
            };

            _bundleServiceMock
                .Setup(s => s.GetBundleByIdAsync(bundleId))
                .ReturnsAsync(bundleDto);

            // Act
            var result = await _controller.Edit(bundleId);

            // Assert
            Assert.That(result, Is.InstanceOf<ForbidResult>());
        }

        [Test]
        public async Task Edit_Post_WithValidModel_RedirectsToIndex()
        {
            // Arrange
            var bundleId = 1;
            var model = new EditBundleViewModel
            {
                Id = bundleId,
                Title = "Updated Bundle",
                Description = "Updated description",
                CategoryIds = new List<int> { 1 },
                WordIds = new List<int> { 1, 2 },
                DifficultyLevels = new List<string> { "B1", "B2" }
            };

            _bundleServiceMock
                .Setup(s => s.UpdateBundleAsync(TestUserId, bundleId, It.IsAny<CreateBundleDTO>()))
                .ReturnsAsync(Result.Success());

            // Act
            var result = await _controller.Edit(bundleId, model);

            // Assert
            var redirectResult = result as RedirectToActionResult;
            Assert.That(redirectResult, Is.Not.Null);
            Assert.That(redirectResult!.ActionName, Is.EqualTo("Index"));

            _bundleServiceMock.Verify(
                s => s.UpdateBundleAsync(TestUserId, bundleId, It.IsAny<CreateBundleDTO>()),
                Times.Once);
        }

        [Test]
        public async Task Edit_Post_WithIdMismatch_ReturnsBadRequest()
        {
            // Arrange
            var bundleId = 1;
            var model = new EditBundleViewModel
            {
                Id = 2,
                Title = "Updated Bundle"
            };

            // Act
            var result = await _controller.Edit(bundleId, model);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestResult>());

            _bundleServiceMock.Verify(
                s => s.UpdateBundleAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CreateBundleDTO>()),
                Times.Never);
        }

        [Test]
        public async Task Edit_Post_WithInvalidModelState_ReturnsViewWithReloadedData()
        {
            // Arrange
            var bundleId = 1;
            var model = new EditBundleViewModel
            {
                Id = bundleId,
                Title = "", // Invalid
                Description = "Updated description"
            };

            _controller.ModelState.AddModelError("Title", "Title is required");

            var categories = new List<Category> { new Category { Id = 1, Name = "Fruits" } };
            var words = new List<Word> { new Word { Id = 1, Term = "Apple", Translation = "Яблуко" } };

            _categoryRepositoryMock
                .Setup(r => r.GetAllCategoriesAsync())
                .ReturnsAsync(categories);

            _wordRepositoryMock
                .Setup(r => r.GetAllWordsAsync())
                .ReturnsAsync(words);

            // Act
            var result = await _controller.Edit(bundleId, model);

            // Assert
            var viewResult = result as ViewResult;
            Assert.That(viewResult, Is.Not.Null);

            var returnedModel = viewResult!.Model as EditBundleViewModel;
            Assert.That(returnedModel!.Categories.Count, Is.EqualTo(1));
            Assert.That(returnedModel.Words.Count, Is.EqualTo(1));

            _bundleServiceMock.Verify(
                s => s.UpdateBundleAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CreateBundleDTO>()),
                Times.Never);
        }

        [Test]
        public async Task Edit_Post_WhenServiceReturnsError_ReturnsViewWithError()
        {
            // Arrange
            var bundleId = 1;
            var model = new EditBundleViewModel
            {
                Id = bundleId,
                Title = "Updated Bundle",
                Description = "Updated description"
            };

            var errorMessage = "Failed to update bundle.";
            _bundleServiceMock
                .Setup(s => s.UpdateBundleAsync(TestUserId, bundleId, It.IsAny<CreateBundleDTO>()))
                .ReturnsAsync(Result.Failure(errorMessage));

            var categories = new List<Category> { new Category { Id = 1, Name = "Fruits" } };
            var words = new List<Word> { new Word { Id = 1, Term = "Apple", Translation = "Яблуко" } };

            _categoryRepositoryMock
                .Setup(r => r.GetAllCategoriesAsync())
                .ReturnsAsync(categories);

            _wordRepositoryMock
                .Setup(r => r.GetAllWordsAsync())
                .ReturnsAsync(words);

            // Act
            var result = await _controller.Edit(bundleId, model);

            // Assert
            var viewResult = result as ViewResult;
            Assert.That(viewResult, Is.Not.Null);

            var hasError = _controller.ModelState.Values
                .SelectMany(v => v.Errors)
                .Any(e => e.ErrorMessage.Contains("update"));
            Assert.That(hasError, Is.True);
        }

        [Test]
        public async Task Delete_Post_WithValidId_RedirectsToIndex()
        {
            // Arrange
            var bundleId = 1;

            _bundleServiceMock
                .Setup(s => s.DeleteBundleAsync(TestUserId, bundleId))
                .ReturnsAsync(Result.Success());

            // Act
            var result = await _controller.Delete(bundleId);

            // Assert
            var redirectResult = result as RedirectToActionResult;
            Assert.That(redirectResult, Is.Not.Null);
            Assert.That(redirectResult!.ActionName, Is.EqualTo("Index"));

            _bundleServiceMock.Verify(
                s => s.DeleteBundleAsync(TestUserId, bundleId),
                Times.Once);
        }

        [Test]
        public async Task Delete_Post_WhenServiceReturnsError_RedirectsToIndexWithTempData()
        {
            // Arrange
            var bundleId = 1;
            var errorMessage = "Failed to delete bundle.";

            _bundleServiceMock
                .Setup(s => s.DeleteBundleAsync(TestUserId, bundleId))
                .ReturnsAsync(Result.Failure(errorMessage));

            // Act
            var result = await _controller.Delete(bundleId);

            // Assert
            var redirectResult = result as RedirectToActionResult;
            Assert.That(redirectResult, Is.Not.Null);
            Assert.That(redirectResult!.ActionName, Is.EqualTo("Index"));
        }

        [Test]
        public async Task Index_Get_ReturnsViewWithPagedBundles()
        {
            // Arrange
            var pagedResult = new PagedResult<BundleDTO>
            {
                Items = new List<BundleDTO>
                {
                    new BundleDTO { Id = 1, Title = "Bundle 1", OwnerId = TestUserId },
                    new BundleDTO { Id = 2, Title = "Bundle 2", OwnerId = TestUserId }
                },
                TotalCount = 2,
                Page = 1,
                PageSize = 6
            };

            _bundleServiceMock
                .Setup(s => s.GetUserBundlesPageAsync(
                    TestUserId,
                    null,
                    null,
                    null,
                    null,
                    null,
                    1,
                    6))
                .ReturnsAsync(pagedResult);

            var mockCategories = new List<Category>
            {
                new() { Id = 1, Name = "Travel" },
                new() { Id = 2, Name = "Fruits" }
            };

            _categoryRepositoryMock
                .Setup(r => r.GetAllCategoriesAsync())
                .ReturnsAsync(mockCategories);

            // Act — pass pageSize=6 explicitly so the controller uses it directly (skips settings fallback)
            var result = await _controller.Index(page: 1, pageSize: 6);

            // Assert
            var viewResult = result as ViewResult;
            Assert.That(viewResult, Is.Not.Null);

            var model = viewResult!.Model as BundleIndexViewModel;
            Assert.That(model, Is.Not.Null);
            Assert.That(model!.Bundles.Items.Count, Is.EqualTo(2));
            Assert.That(model.Bundles.TotalCount, Is.EqualTo(2));
            Assert.That(model.Categories.Count, Is.EqualTo(2));

            _bundleServiceMock.Verify(
                s => s.GetUserBundlesPageAsync(
                    TestUserId,
                    null,
                    null,
                    null,
                    null,
                    null,
                    1,
                    6),
                Times.Once);
        }

        [Test]
        public async Task SubmitForReview_ValidId_ReturnsOk()
        {
            // Arrange
            var bundleId = 1;
            _bundleServiceMock
                .Setup(s => s.SubmitBundleForReviewAsync(TestUserId, bundleId))
                .ReturnsAsync(Result.Success());

            // Act
            var result = await _controller.SubmitForReview(bundleId);

            // Assert
            var okResult = result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            _bundleServiceMock.Verify(s => s.SubmitBundleForReviewAsync(TestUserId, bundleId), Times.Once);
        }
        [Test]
            public async Task Details_ValidIdAndIsOwner_ReturnsViewWithBundle()
            {
                // Arrange
                var bundleId = 1;
                var bundleDto = new BundleDTO
                {
                    Id = bundleId,
                    Title = "Test Bundle",
                    OwnerId = TestUserId,
                    IsSystem = false,
                    IsPublic = false,
                    Words = new List<WordItemViewModel> { new WordItemViewModel { Id = 1, Term = "Word" } }
                };

                _bundleServiceMock
                    .Setup(s => s.GetBundleByIdAsync(bundleId))
                    .ReturnsAsync(bundleDto);

                // Act
                var result = await _controller.Details(bundleId);

                // Assert
                var viewResult = result as ViewResult;
                Assert.That(viewResult, Is.Not.Null);
                Assert.That(viewResult!.Model, Is.EqualTo(bundleDto));
                _bundleServiceMock.Verify(s => s.GetBundleByIdAsync(bundleId), Times.Once);
            }

            [Test]
            public async Task Details_NonExistentId_ReturnsNotFound()
            {
                // Arrange
                var bundleId = 999;
                _bundleServiceMock
                    .Setup(s => s.GetBundleByIdAsync(bundleId))
                    .ReturnsAsync((BundleDTO?)null);

                // Act
                var result = await _controller.Details(bundleId);

                // Assert
                Assert.That(result, Is.InstanceOf<NotFoundResult>());
            }

            [Test]
            public async Task Details_PrivateBundleBelongsToOtherUser_ReturnsForbid()
            {
                // Arrange
                var bundleId = 1;
                var bundleDto = new BundleDTO
                {
                    Id = bundleId,
                    OwnerId = "other-user",
                    IsSystem = false,
                    IsPublic = false
                };

                _bundleServiceMock
                    .Setup(s => s.GetBundleByIdAsync(bundleId))
                    .ReturnsAsync(bundleDto);

                // Act
                var result = await _controller.Details(bundleId);

                // Assert
                Assert.That(result, Is.InstanceOf<ForbidResult>());
            }
    }
}