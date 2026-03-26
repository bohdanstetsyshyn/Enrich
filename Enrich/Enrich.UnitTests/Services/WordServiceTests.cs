using Enrich.BLL.DTOs;
using Enrich.BLL.Services;
using Enrich.DAL.Data;
using Enrich.DAL.Entities;
using Enrich.DAL.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Enrich.UnitTests.Services
{
    [TestFixture]
    public class WordServiceTests
    {
        private ApplicationDbContext _dbContext = null!;
        private Mock<ILogger<WordService>> _loggerMock = null!;
        private WordService _wordService = null!;

        [SetUp]
        public void SetUp()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _dbContext = new ApplicationDbContext(options);
            _loggerMock = new Mock<ILogger<WordService>>();

            var wordRepository = new WordRepository(_dbContext);
            var categoryRepository = new CategoryRepository(_dbContext);

            _wordService = new WordService(wordRepository, categoryRepository, _loggerMock.Object);
        }

        [Test]
        public async Task GetPersonalWordsAsync_SearchFilterCategoryPagination_Works()
        {
            var userId = "user-1";
            var now = DateTime.UtcNow;

            var catFruits = new Category { Name = "Fruits" };
            var catTravel = new Category { Name = "Travel" };
            _dbContext.Categories.AddRange(catFruits, catTravel);
            await _dbContext.SaveChangesAsync();

            var w1 = new Word { Term = "Apple", PartOfSpeech = "noun", DifficultyLevel = "A1", IsGlobal = false, CreatorId = userId, CreatedAt = now, UpdatedAt = now, Categories = new List<Category> { catFruits } };
            var w2 = new Word { Term = "Apply", PartOfSpeech = "verb", DifficultyLevel = "B1", IsGlobal = false, CreatorId = userId, CreatedAt = now, UpdatedAt = now, Categories = new List<Category> { catTravel } };
            var w3 = new Word { Term = "Application", PartOfSpeech = "noun", DifficultyLevel = "B2", IsGlobal = false, CreatorId = userId, CreatedAt = now, UpdatedAt = now, Categories = new List<Category> { catFruits } };

            _dbContext.Words.AddRange(w1, w2, w3);
            await _dbContext.SaveChangesAsync();

            _dbContext.UserWords.AddRange(
                new UserWord { UserId = userId, WordId = w1.Id, SavedAt = now },
                new UserWord { UserId = userId, WordId = w2.Id, SavedAt = now },
                new UserWord { UserId = userId, WordId = w3.Id, SavedAt = now });
            await _dbContext.SaveChangesAsync();

            var pageResult = await _wordService.GetPersonalWordsAsync(userId, "app", "Fruits", "noun", null, 1, 10);

            Assert.That(pageResult.TotalCount, Is.EqualTo(2));
            Assert.That(pageResult.Items.Count(), Is.EqualTo(2));
            var terms = pageResult.Items.Select(i => i.Term).ToList();
            Assert.That(terms, Does.Contain("Apple"));
            Assert.That(terms, Does.Contain("Application"));

            var page2 = await GetPersonalWords_Page(userId);
            Assert.That(page2.TotalCount, Is.EqualTo(2));
            Assert.That(page2.Items.Count(), Is.EqualTo(1));
        }

        private async Task<PagedResult<PersonalWordDTO>> GetPersonalWords_Page(string userId)
        {
            return await _wordService.GetPersonalWordsAsync(userId, "app", "Fruits", "noun", null, 2, 1);
        }

        [Test]
        public async Task DeleteWordAsync_UserRemovesOwnPersonalWord_DeletesWordAndUserWord()
        {
            var userId = "user-1";
            var dto = new CreatePersonalWordDTO { Term = "Transient" };
            var created = await _wordService.CreatePersonalWordAsync(userId, dto);
            Assert.That(created.IsSuccess, Is.True);

            var word = await _dbContext.Words.FirstOrDefaultAsync(w => w.Term == "Transient");
            Assert.That(word, Is.Not.Null);

            var result = await _wordService.DeleteWordAsync(userId, word!.Id);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.ErrorMessage, Is.Null);

            var dbWord = await _dbContext.Words.FindAsync(word.Id);
            Assert.That(dbWord, Is.Null);

            var userWord = await _dbContext.UserWords.FirstOrDefaultAsync(uw => uw.UserId == userId && uw.WordId == word.Id);
            Assert.That(userWord, Is.Null);
        }

        [Test]
        public async Task DeleteWordAsync_UserRemovesSavedSystemWord_DeletesOnlyUserWord()
        {
            var systemWord = new Word
            {
                Term = "SystemWord",
                IsGlobal = true,
                CreatorId = null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            _dbContext.Words.Add(systemWord);
            await _dbContext.SaveChangesAsync();

            var userId = "user-1";
            var userWord = new UserWord { UserId = userId, WordId = systemWord.Id, SavedAt = DateTime.UtcNow };
            _dbContext.UserWords.Add(userWord);
            await _dbContext.SaveChangesAsync();

            var result = await _wordService.DeleteWordAsync(userId, systemWord.Id);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.ErrorMessage, Is.Null);

            var dbWord = await _dbContext.Words.FindAsync(systemWord.Id);
            Assert.That(dbWord, Is.Not.Null);

            var dbUserWord = await _dbContext.UserWords.FirstOrDefaultAsync(uw => uw.UserId == userId && uw.WordId == systemWord.Id);
            Assert.That(dbUserWord, Is.Null);
        }

        [Test]
        public async Task DeleteWordAsync_UserTriesToDeleteNotSavedWord_ReturnsFailure()
        {
            var userId = "user-1";
            var otherUser = "user-2";
            var dto = new CreatePersonalWordDTO { Term = "OtherUserWord" };
            var created = await _wordService.CreatePersonalWordAsync(otherUser, dto);
            Assert.That(created.IsSuccess, Is.True);

            var word = await _dbContext.Words.FirstOrDefaultAsync(w => w.Term == "OtherUserWord");
            Assert.That(word, Is.Not.Null);

            var result = await _wordService.DeleteWordAsync(userId, word!.Id);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorMessage, Is.Not.Null);
        }

        [TearDown]
        public void TearDown()
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
        }

        [Test]
        public async Task CreatePersonalWordAsync_WithValidData_CreatesWordAndUserWord()
        {
            var userId = "user-1";
            var dto = new CreatePersonalWordDTO
            {
                Term = "Serendipity",
                Translation = "Щасливий випадок",
                DifficultyLevel = "B2",
                PartOfSpeech = "noun",
                Transcription = "/ˌserənˈdɪpɪti/",
                Meaning = "The occurrence of events by chance in a happy way.",
                Example = "It was pure serendipity that we met.",
            };

            var createResult = await _wordService.CreatePersonalWordAsync(userId, dto);

            Assert.That(createResult.IsSuccess, Is.True);
            Assert.That(createResult.ErrorMessage, Is.Null);

            var word = await _dbContext.Words.FirstOrDefaultAsync(w => w.Term == "Serendipity");
            Assert.That(word, Is.Not.Null);
            Assert.That(word!.CreatorId, Is.EqualTo(userId));
            Assert.That(word.IsGlobal, Is.False);
            Assert.That(word.Translation, Is.EqualTo("Щасливий випадок"));

            var userWord = await _dbContext.UserWords.FirstOrDefaultAsync(uw => uw.UserId == userId);
            Assert.That(userWord, Is.Not.Null);
            Assert.That(userWord!.WordId, Is.EqualTo(word.Id));
        }

        [Test]
        public async Task CreatePersonalWordAsync_WithDuplicateTerm_ReturnsFailure()
        {
            var userId = "user-1";
            var dto = new CreatePersonalWordDTO { Term = "Benevolent" };

            await _wordService.CreatePersonalWordAsync(userId, dto);

            var duplicateResult = await _wordService.CreatePersonalWordAsync(userId, dto);

            Assert.That(duplicateResult.IsSuccess, Is.False);
            Assert.That(duplicateResult.ErrorMessage, Is.Not.Null);
            Assert.That(duplicateResult.ErrorMessage, Does.Contain("Benevolent"));
        }

        [Test]
        public async Task CreatePersonalWordAsync_DuplicateCheckIsCaseInsensitive()
        {
            var userId = "user-1";
            await _wordService.CreatePersonalWordAsync(userId, new CreatePersonalWordDTO { Term = "hello" });

            var caseResult = await _wordService.CreatePersonalWordAsync(userId, new CreatePersonalWordDTO { Term = "HELLO" });

            Assert.That(caseResult.IsSuccess, Is.False);
            Assert.That(caseResult.ErrorMessage, Is.Not.Null);
        }

        [Test]
        public async Task CreatePersonalWordAsync_DifferentUser_SameTermAllowed()
        {
            var dto = new CreatePersonalWordDTO { Term = "Ephemeral" };
            await _wordService.CreatePersonalWordAsync("user-1", dto);

            var differentUserResult = await _wordService.CreatePersonalWordAsync("user-2", dto);

            Assert.That(differentUserResult.IsSuccess, Is.True);
            Assert.That(differentUserResult.ErrorMessage, Is.Null);

            var words = await _dbContext.Words.Where(w => w.Term == "Ephemeral").ToListAsync();
            Assert.That(words, Has.Count.EqualTo(2));
        }

        [Test]
        public async Task CreatePersonalWordAsync_TrimsWhitespaceFromTerm()
        {
            var userId = "user-1";
            var dto = new CreatePersonalWordDTO { Term = "  Melancholy  " };

            var trimResult = await _wordService.CreatePersonalWordAsync(userId, dto);

            Assert.That(trimResult.IsSuccess, Is.True);
            var word = await _dbContext.Words.FirstOrDefaultAsync(w => w.Term == "Melancholy");
            Assert.That(word, Is.Not.Null);
        }

        [Test]
        public async Task CreatePersonalWordAsync_WithOnlyRequiredField_Succeeds()
        {
            var userId = "user-1";
            var dto = new CreatePersonalWordDTO { Term = "Ubiquitous" };

            var requiredOnlyResult = await _wordService.CreatePersonalWordAsync(userId, dto);

            Assert.That(requiredOnlyResult.IsSuccess, Is.True);
            Assert.That(requiredOnlyResult.ErrorMessage, Is.Null);

            var word = await _dbContext.Words.FirstOrDefaultAsync(w => w.Term == "Ubiquitous");
            Assert.That(word, Is.Not.Null);
            Assert.That(word!.Translation, Is.Null);
            Assert.That(word.Example, Is.Null);
        }

        [Test]
        public async Task SaveSystemWordAsync_WhenWordIsGlobalAndNotSaved_ReturnsSuccess()
        {
            var userId = "user-1";
            var systemWord = new Word
            {
                Term = "GlobalWord",
                IsGlobal = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _dbContext.Words.Add(systemWord);
            await _dbContext.SaveChangesAsync();

            var saveResult = await _wordService.SaveSystemWordAsync(userId, systemWord.Id);

            Assert.That(saveResult.IsSuccess, Is.True);
            Assert.That(saveResult.ErrorMessage, Is.Null);

            var userWord = await _dbContext.UserWords
                .FirstOrDefaultAsync(uw => uw.UserId == userId && uw.WordId == systemWord.Id);
            Assert.That(userWord, Is.Not.Null);
        }

        [Test]
        public async Task SaveSystemWordAsync_WhenWordAlreadySaved_ReturnsError()
        {
            var userId = "user-1";
            var systemWord = new Word
            {
                Term = "GlobalWord",
                IsGlobal = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _dbContext.Words.Add(systemWord);
            await _dbContext.SaveChangesAsync();

            var existingLink = new UserWord
            {
                UserId = userId,
                WordId = systemWord.Id,
                SavedAt = DateTime.UtcNow
            };
            _dbContext.UserWords.Add(existingLink);
            await _dbContext.SaveChangesAsync();

            var alreadySavedResult = await _wordService.SaveSystemWordAsync(userId, systemWord.Id);

            Assert.That(alreadySavedResult.IsSuccess, Is.False);
            Assert.That(alreadySavedResult.ErrorMessage, Is.EqualTo("You have already saved this word."));

            var userWordCount = await _dbContext.UserWords
                .CountAsync(uw => uw.UserId == userId && uw.WordId == systemWord.Id);
            Assert.That(userWordCount, Is.EqualTo(1));
        }

        [Test]
        public async Task SaveSystemWordAsync_WhenWordIsNotGlobal_ReturnsError()
        {
            var userId = "user-1";
            var personalWord = new Word
            {
                Term = "PrivateWord",
                IsGlobal = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _dbContext.Words.Add(personalWord);
            await _dbContext.SaveChangesAsync();

            var notGlobalResult = await _wordService.SaveSystemWordAsync(userId, personalWord.Id);

            Assert.That(notGlobalResult.IsSuccess, Is.False);
            Assert.That(notGlobalResult.ErrorMessage, Is.EqualTo("Word not found or is not a system word."));
        }
    }
}