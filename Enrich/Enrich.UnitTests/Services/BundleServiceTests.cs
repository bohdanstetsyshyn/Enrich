using Enrich.BLL.Services;
using Enrich.BLL.Settings;
using Enrich.DAL.Entities;
using Enrich.DAL.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;

namespace Enrich.UnitTests.Services
{
    [TestFixture]
    public class BundleServiceTests
    {
        private Mock<IBundleRepository> _bundleRepositoryMock = null!;
        private Mock<ICategoryRepository> _categoryRepositoryMock = null!;
        private Mock<ILogger<BundleService>> _loggerMock = null!;
        private Mock<IOptions<PaginationSettings>> _paginationOptionsMock = null!;
        private BundleService _bundleService = null!;

        [SetUp]
        public void SetUp()
        {
            _bundleRepositoryMock = new Mock<IBundleRepository>();
            _categoryRepositoryMock = new Mock<ICategoryRepository>();
            _loggerMock = new Mock<ILogger<BundleService>>();

            _paginationOptionsMock = new Mock<IOptions<PaginationSettings>>();
            _paginationOptionsMock.Setup(o => o.Value).Returns(new PaginationSettings());

            _bundleService = new BundleService(
                _bundleRepositoryMock.Object,
                _categoryRepositoryMock.Object,
                _paginationOptionsMock.Object,
                _loggerMock.Object);
        }

        [Test]
        public async Task GetBundleByIdAsync_ValidId_ReturnsBundleWithWords()
        {
            // Arrange
            var bundleId = 1;
            var bundle = new Bundle
            {
                Id = bundleId,
                Title = "Test Bundle",
                Words = new List<Word> { new Word { Id = 1, Term = "Apple", Translation = "Яблуко" } }
            };

            _bundleRepositoryMock
                .Setup(r => r.GetBundleByIdWithDetailsAsync(bundleId))
                .ReturnsAsync(bundle);

            // Act
            var result = await _bundleService.GetBundleByIdAsync(bundleId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Words.Count, Is.EqualTo(1));
            Assert.That(result.Words[0].Term, Is.EqualTo("Apple"));
        }

        [Test]
        public async Task GetSystemBundlesAsync_ReturnsPagedResult()
        {
            // Arrange
            var bundles = new List<Bundle>
            {
                new Bundle
                {
                    Id = 1,
                    Title = "Test Bundle",
                    Description = "Test Description",
                    DifficultyLevels = ["A1", "A2"],
                    Words = new List<Word>
                    {
                        new Word { Id = 1, Term = "Word1" },
                        new Word { Id = 2, Term = "Word2" }
                    },
                    Categories = new List<Category>
                    {
                        new Category { Id = 1, Name = "Fruits" }
                    }
                }
            };

            _bundleRepositoryMock
                .Setup(r => r.GetSystemBundlesPageAsync(null, null, null, null, null, 1, 12))
                .ReturnsAsync((bundles.AsEnumerable(), 1));

            // Act
            var result = await _bundleService.GetSystemBundlesAsync(null, null, null, null, null, 1, 12);

            // Assert
            Assert.That(result.TotalCount, Is.EqualTo(1));
            Assert.That(result.Items.Count(), Is.EqualTo(1));
            Assert.That(result.Page, Is.EqualTo(1));
            Assert.That(result.PageSize, Is.EqualTo(12));

            var bundle = result.Items.First();
            Assert.That(bundle.Id, Is.EqualTo(1));
            Assert.That(bundle.Title, Is.EqualTo("Test Bundle"));
            Assert.That(bundle.WordCount, Is.EqualTo(2));
            Assert.That(bundle.Categories.Count, Is.EqualTo(1));
            Assert.That(bundle.Categories[0], Is.EqualTo("Fruits"));

            _bundleRepositoryMock.Verify(
                r => r.GetSystemBundlesPageAsync(null, null, null, null, null, 1, 12),
                Times.Once);
        }

        [Test]
        public async Task GetSystemBundlesAsync_WithSearchTerm_FiltersResults()
        {
            // Arrange
            var bundles = new List<Bundle>
            {
                new Bundle
                {
                    Id = 1,
                    Title = "Fruits Bundle",
                    Description = "Learn fruit vocabulary",
                    DifficultyLevels = ["B1"],
                    Words = new List<Word> { new Word { Id = 1, Term = "Apple" } },
                    Categories = new List<Category>()
                }
            };

            _bundleRepositoryMock
                .Setup(r => r.GetSystemBundlesPageAsync("fruit", null, null, null, null, 1, 12))
                .ReturnsAsync((bundles.AsEnumerable(), 1));

            // Act
            var result = await _bundleService.GetSystemBundlesAsync("fruit", null, null, null, null, 1, 12);

            // Assert
            Assert.That(result.Items.Count(), Is.EqualTo(1));
            Assert.That(result.Items.First().Title, Is.EqualTo("Fruits Bundle"));
            _bundleRepositoryMock.Verify(
                r => r.GetSystemBundlesPageAsync("fruit", null, null, null, null, 1, 12),
                Times.Once);
        }

        [Test]
        public async Task GetSystemBundlesAsync_WithCategoryFilter_FiltersResults()
        {
            // Arrange
            var bundles = new List<Bundle>
            {
                new Bundle
                {
                    Id = 1,
                    Title = "Tech Bundle",
                    DifficultyLevels = [],
                    Words = new List<Word>(),
                    Categories = new List<Category>
                    {
                        new Category { Id = 1, Name = "Technology" }
                    }
                }
            };

            _bundleRepositoryMock
                .Setup(r => r.GetSystemBundlesPageAsync(null, "Technology", null, null, null, 1, 12))
                .ReturnsAsync((bundles.AsEnumerable(), 1));

            // Act
            var result = await _bundleService.GetSystemBundlesAsync(null, "Technology", null, null, null, 1, 12);

            // Assert
            Assert.That(result.Items.Count(), Is.EqualTo(1));
            Assert.That(result.Items.First().Categories[0], Is.EqualTo("Technology"));
        }

        [Test]
        public async Task GetSystemBundlesAsync_WithDifficultyFilter_FiltersResults()
        {
            // Arrange
            var bundles = new List<Bundle>
            {
                new Bundle
                {
                    Id = 1,
                    Title = "Beginner Bundle",
                    DifficultyLevels = ["A1", "A2"],
                    Words = new List<Word>(),
                    Categories = new List<Category>()
                }
            };

            _bundleRepositoryMock
                .Setup(r => r.GetSystemBundlesPageAsync(null, null, "A1,A2", null, null, 1, 12))
                .ReturnsAsync((bundles.AsEnumerable(), 1));

            // Act
            var result = await _bundleService.GetSystemBundlesAsync(null, null, "A1,A2", null, null, 1, 12);

            // Assert
            Assert.That(result.Items.Count(), Is.EqualTo(1));
            Assert.That(result.Items.First().DifficultyLevels, Contains.Item("A1"));
            Assert.That(result.Items.First().DifficultyLevels, Contains.Item("A2"));
        }

        [Test]
        public async Task GetSystemBundlesAsync_WithWordCountRange_FiltersResults()
        {
            // Arrange
            var bundles = new List<Bundle>
            {
                new Bundle
                {
                    Id = 1,
                    Title = "Medium Bundle",
                    DifficultyLevels = [],
                    Words = new List<Word>
                    {
                        new Word { Id = 1, Term = "Word1" },
                        new Word { Id = 2, Term = "Word2" },
                        new Word { Id = 3, Term = "Word3" }
                    },
                    Categories = new List<Category>()
                }
            };

            _bundleRepositoryMock
                .Setup(r => r.GetSystemBundlesPageAsync(null, null, null, 2, 5, 1, 12))
                .ReturnsAsync((bundles.AsEnumerable(), 1));

            // Act
            var result = await _bundleService.GetSystemBundlesAsync(null, null, null, 2, 5, 1, 12);

            // Assert
            Assert.That(result.Items.Count(), Is.EqualTo(1));
            Assert.That(result.Items.First().WordCount, Is.EqualTo(3));
        }

        [Test]
        public async Task GetSystemBundlesAsync_WithInvalidPage_DefaultsToPageOne()
        {
            // Arrange
            _bundleRepositoryMock
                .Setup(r => r.GetSystemBundlesPageAsync(null, null, null, null, null, 1, 12))
                .ReturnsAsync((new List<Bundle>().AsEnumerable(), 0));

            // Act
            var result = await _bundleService.GetSystemBundlesAsync(null, null, null, null, null, 0, 12);

            // Assert
            Assert.That(result.Page, Is.EqualTo(1));
            _bundleRepositoryMock.Verify(
                r => r.GetSystemBundlesPageAsync(null, null, null, null, null, 1, 12),
                Times.Once);
        }

        [Test]
        public async Task GetSystemBundlesAsync_WithInvalidPageSize_DefaultsToTwelve()
        {
            // Arrange
            _bundleRepositoryMock
                .Setup(r => r.GetSystemBundlesPageAsync(null, null, null, null, null, 1, 12))
                .ReturnsAsync((new List<Bundle>().AsEnumerable(), 0));

            // Act
            var result = await _bundleService.GetSystemBundlesAsync(null, null, null, null, null, 1, 0);

            // Assert
            Assert.That(result.PageSize, Is.EqualTo(12));
            _bundleRepositoryMock.Verify(
                r => r.GetSystemBundlesPageAsync(null, null, null, null, null, 1, 12),
                Times.Once);
        }

        [Test]
        public async Task GetSystemBundlesAsync_WithNullWords_ReturnsZeroWordCount()
        {
            // Arrange
            var bundles = new List<Bundle>
            {
                new Bundle
                {
                    Id = 1,
                    Title = "Empty Bundle",
                    DifficultyLevels = [],
                    Words = null!,
                    Categories = new List<Category>()
                }
            };

            _bundleRepositoryMock
                .Setup(r => r.GetSystemBundlesPageAsync(null, null, null, null, null, 1, 12))
                .ReturnsAsync((bundles.AsEnumerable(), 1));

            // Act
            var result = await _bundleService.GetSystemBundlesAsync(null, null, null, null, null, 1, 12);

            // Assert
            Assert.That(result.Items.First().WordCount, Is.EqualTo(0));
        }

        [Test]
        public async Task GetSystemBundlesAsync_WithNullCategories_ReturnsEmptyList()
        {
            // Arrange
            var bundles = new List<Bundle>
            {
                new Bundle
                {
                    Id = 1,
                    Title = "No Categories Bundle",
                    DifficultyLevels = [],
                    Words = new List<Word>(),
                    Categories = null!
                }
            };

            _bundleRepositoryMock
                .Setup(r => r.GetSystemBundlesPageAsync(null, null, null, null, null, 1, 12))
                .ReturnsAsync((bundles.AsEnumerable(), 1));

            // Act
            var result = await _bundleService.GetSystemBundlesAsync(null, null, null, null, null, 1, 12);

            // Assert
            Assert.That(result.Items.First().Categories, Is.Empty);
        }

        [Test]
        public async Task GetAllCategoriesAsync_ReturnsCategories()
        {
            // Arrange
            var categories = new List<Category>
            {
                new Category { Id = 1, Name = "Fruits" },
                new Category { Id = 2, Name = "Technology" },
                new Category { Id = 3, Name = "Travel" }
            };

            _categoryRepositoryMock
                .Setup(r => r.GetAllCategoriesAsync())
                .ReturnsAsync(categories);

            // Act
            var result = await _bundleService.GetAllCategoriesAsync();

            // Assert
            Assert.That(result.Count(), Is.EqualTo(3));
            Assert.That(result.Select(c => c.Name), Contains.Item("Fruits"));
            Assert.That(result.Select(c => c.Name), Contains.Item("Technology"));
            _categoryRepositoryMock.Verify(r => r.GetAllCategoriesAsync(), Times.Once);
        }

        [Test]
        public async Task SaveSystemBundleAsync_ValidSystemBundle_ReturnsSuccess()
        {
            // Arrange
            var userId = "user123";
            var bundleId = 1;
            var systemBundle = new Bundle { Id = bundleId, IsSystem = true };

            _bundleRepositoryMock
                .Setup(r => r.GetBundleByIdAsync(bundleId))
                .ReturnsAsync(systemBundle);

            _bundleRepositoryMock
                .Setup(r => r.UserHasBundleAsync(userId, bundleId))
                .ReturnsAsync(false);

            _bundleRepositoryMock
                .Setup(r => r.SaveUserBundleAsync(It.IsAny<UserBundle>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _bundleService.SaveSystemBundleAsync(userId, bundleId);

            // Assert
            Assert.That(result.IsSuccess, Is.True);

            _bundleRepositoryMock.Verify(
                r => r.SaveUserBundleAsync(It.Is<UserBundle>(ub => ub.UserId == userId && ub.BundleId == bundleId)),
                Times.Once);
        }

        [Test]
        public async Task SaveSystemBundleAsync_BundleNotSystem_ReturnsError()
        {
            // Arrange
            var userId = "user123";
            var bundleId = 1;
            var nonSystemBundle = new Bundle { Id = bundleId, IsSystem = false };

            _bundleRepositoryMock
                .Setup(r => r.GetBundleByIdAsync(bundleId))
                .ReturnsAsync(nonSystemBundle);

            // Act
            var result = await _bundleService.SaveSystemBundleAsync(userId, bundleId);

            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorMessage, Is.EqualTo("Collection not found or is not a system collection."));

            _bundleRepositoryMock.Verify(r => r.SaveUserBundleAsync(It.IsAny<UserBundle>()), Times.Never);
        }

        [Test]
        public async Task SaveSystemBundleAsync_AlreadySaved_ReturnsError()
        {
            // Arrange
            var userId = "user123";
            var bundleId = 1;
            var systemBundle = new Bundle { Id = bundleId, IsSystem = true };

            _bundleRepositoryMock
                .Setup(r => r.GetBundleByIdAsync(bundleId))
                .ReturnsAsync(systemBundle);

            _bundleRepositoryMock
                .Setup(r => r.UserHasBundleAsync(userId, bundleId))
                .ReturnsAsync(true);

            // Act
            var result = await _bundleService.SaveSystemBundleAsync(userId, bundleId);

            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorMessage, Is.EqualTo("You have already saved this collection."));

            _bundleRepositoryMock.Verify(r => r.SaveUserBundleAsync(It.IsAny<UserBundle>()), Times.Never);
        }

        [Test]
        public async Task UpdateBundleAsync_ValidBundle_ReturnsSuccess()
        {
            // Arrange
            var userId = "user123";
            var bundleId = 1;
            var existingBundle = new Bundle { Id = bundleId, OwnerId = userId, Title = "Old Title" };
            var dto = new BLL.DTOs.CreateBundleDTO
            {
                Title = "New Title",
                Description = "New Description",
                WordIds = new List<int> { 1, 2 },
                CategoryIds = new List<int> { 1 }
            };

            _bundleRepositoryMock
                .Setup(r => r.GetBundleByIdAsync(bundleId))
                .ReturnsAsync(existingBundle);

            _bundleRepositoryMock
                .Setup(r => r.BundleTitleExistsForUserAsync(userId, "new title"))
                .ReturnsAsync(false);

            _bundleRepositoryMock
                .Setup(r => r.UpdateBundleAsync(It.IsAny<Bundle>()))
                .Returns(Task.CompletedTask);

            _bundleRepositoryMock
                .Setup(r => r.SyncBundleRelationsAsync(bundleId, dto.WordIds, dto.CategoryIds))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _bundleService.UpdateBundleAsync(userId, bundleId, dto);

            // Assert
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(existingBundle.Title, Is.EqualTo("New Title"));
            Assert.That(existingBundle.Description, Is.EqualTo("New Description"));
            _bundleRepositoryMock.Verify(r => r.UpdateBundleAsync(existingBundle), Times.Once);
            _bundleRepositoryMock.Verify(r => r.SyncBundleRelationsAsync(bundleId, dto.WordIds, dto.CategoryIds), Times.Once);
        }

        [Test]
        public async Task UpdateBundleAsync_InReview_ReturnsError()
        {
            // Arrange
            var userId = "user123";
            var bundleId = 1;
            var existingBundle = new Bundle
            {
                Id = bundleId,
                OwnerId = userId,
                Status = Enrich.DAL.Entities.Enums.BundleStatus.PendingReview
            };
            var dto = new BLL.DTOs.CreateBundleDTO { Title = "New Title" };

            _bundleRepositoryMock
                .Setup(r => r.GetBundleByIdAsync(bundleId))
                .ReturnsAsync(existingBundle);

            // Act
            var result = await _bundleService.UpdateBundleAsync(userId, bundleId, dto);

            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorMessage, Is.EqualTo("You cannot edit a bundle that is under review or published."));
        }

        [Test]
        public async Task DeleteBundleAsync_InReview_ReturnsError()
        {
            // Arrange
            var userId = "user123";
            var bundleId = 1;
            var existingBundle = new Bundle
            {
                Id = bundleId,
                OwnerId = userId,
                Status = Enrich.DAL.Entities.Enums.BundleStatus.PendingReview
            };

            _bundleRepositoryMock
                .Setup(r => r.GetBundleByIdAsync(bundleId))
                .ReturnsAsync(existingBundle);

            // Act
            var result = await _bundleService.DeleteBundleAsync(userId, bundleId);

            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorMessage, Is.EqualTo("You cannot delete a bundle that is under review or published."));
        }

        [Test]
        public async Task SubmitBundleForReviewAsync_ValidDraft_ReturnsSuccess()
        {
            // Arrange
            var userId = "user123";
            var bundleId = 1;
            var bundle = new Bundle
            {
                Id = bundleId,
                OwnerId = userId,
                Status = Enrich.DAL.Entities.Enums.BundleStatus.Draft
            };

            _bundleRepositoryMock
                .Setup(r => r.GetBundleByIdAsync(bundleId))
                .ReturnsAsync(bundle);

            _bundleRepositoryMock
                .Setup(r => r.UpdateBundleAsync(It.IsAny<Bundle>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _bundleService.SubmitBundleForReviewAsync(userId, bundleId);

            // Assert
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(bundle.Status, Is.EqualTo(Enrich.DAL.Entities.Enums.BundleStatus.PendingReview));
            _bundleRepositoryMock.Verify(r => r.UpdateBundleAsync(bundle), Times.Once);
        }

        [Test]
        public async Task SubmitBundleForReviewAsync_AlreadySubmitted_ReturnsError()
        {
            // Arrange
            var userId = "user123";
            var bundleId = 1;
            var bundle = new Bundle
            {
                Id = bundleId,
                OwnerId = userId,
                Status = Enrich.DAL.Entities.Enums.BundleStatus.PendingReview
            };

            _bundleRepositoryMock
                .Setup(r => r.GetBundleByIdAsync(bundleId))
                .ReturnsAsync(bundle);

            // Act
            var result = await _bundleService.SubmitBundleForReviewAsync(userId, bundleId);

            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorMessage, Is.EqualTo("This bundle is already submitted or published."));
        }
    }
}