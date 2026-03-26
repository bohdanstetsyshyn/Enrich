using Enrich.BLL.Common;
using Enrich.BLL.DTOs;
using Enrich.BLL.Interfaces;
using Enrich.DAL.Entities;
using Enrich.DAL.Interfaces;
using Microsoft.Extensions.Logging;

namespace Enrich.BLL.Services
{
    public class WordService(
        IWordRepository wordRepository,
        ICategoryRepository categoryRepository,
        ILogger<WordService> logger) : IWordService
    {
        public async Task<Result> CreatePersonalWordAsync(string userId, CreatePersonalWordDTO dto)
        {
            var termLower = dto.Term.Trim().ToLowerInvariant();

            var duplicateExists = await wordRepository.WordExistsForUserAsync(userId, termLower);

            if (duplicateExists)
            {
                logger.LogWarning(
                    "Користувач {UserId} спробував створити дублікат слова: '{Term}'.",
                    userId,
                    dto.Term);

                return Result.Failure($"У вашому особистому словнику вже є слово '{dto.Term}'.");
            }

            var now = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);

            var categories = new List<Category>();
            if (dto.CategoryIds != null && dto.CategoryIds.Any())
            {
                var fetchedCategories = await categoryRepository.GetCategoriesByIdsAsync(dto.CategoryIds);
                if (fetchedCategories != null)
                {
                    categories = [.. fetchedCategories];
                }
            }

            var word = new Word
            {
                Term = dto.Term.Trim(),
                Translation = dto.Translation?.Trim(),
                Transcription = dto.Transcription?.Trim(),
                Meaning = dto.Meaning?.Trim(),
                PartOfSpeech = dto.PartOfSpeech?.Trim(),
                Example = dto.Example?.Trim(),
                DifficultyLevel = dto.DifficultyLevel?.Trim(),
                IsGlobal = false,
                CreatorId = userId,
                CreatedAt = now,
                UpdatedAt = now,
                Categories = categories
            };

            var userWord = new UserWord
            {
                UserId = userId,
                SavedAt = now,
            };

            var createdWord = await wordRepository.CreatePersonalWordAsync(word, userWord);

            logger.LogInformation(
                "Користувач {UserId} успішно створив нове персональне слово '{Term}' (ID: {WordId}) з {CatCount} категоріями.",
                userId,
                createdWord.Term,
                createdWord.Id,
                categories.Count);

            return Result.Success();
        }

        public async Task<IEnumerable<PersonalWordDTO>> GetPersonalWordsAsync(string userId)
        {
            logger.LogInformation("Отримання списку персональних слів для користувача {UserId}", userId);

            var userWords = await wordRepository.GetPersonalWordsWithDetailsAsync(userId);

            var words = userWords.Select(uw => new PersonalWordDTO
            {
                Id = uw.Word.Id,
                Term = uw.Word.Term,
                Translation = uw.Word.Translation,
                Transcription = uw.Word.Transcription,
                Meaning = uw.Word.Meaning,
                PartOfSpeech = uw.Word.PartOfSpeech,
                Example = uw.Word.Example,
                DifficultyLevel = uw.Word.DifficultyLevel,
                AddedAt = uw.SavedAt,
                CategoryName = uw.Word.Categories?.OrderBy(c => c.Id).Select(c => c.Name).FirstOrDefault() ?? "General"
            }).ToList();

            logger.LogInformation("Успішно отримано {Count} слів для користувача {UserId}", words.Count, userId);

            return words;
        }

        public async Task<Result> DeleteWordAsync(string userId, int wordId)
        {
            var userWord = await wordRepository.GetUserWordAsync(userId, wordId);

            if (userWord == null)
            {
                logger.LogWarning("Спроба видалити слово {WordId} користувачем {UserId}, але воно не знайдено.", wordId, userId);
                return Result.Failure("Word not found in your saved words.");
            }

            var word = userWord.Word;
            if (word == null)
            {
                await wordRepository.DeleteUserWordAsync(userWord);
                return Result.Success();
            }

            if (!word.IsGlobal && word.CreatorId == userId)
            {
                await wordRepository.DeleteWordAsync(word);
                logger.LogInformation("Користувач {UserId} як творець видалив персональне слово {WordId}.", userId, wordId);
                return Result.Success();
            }

            await wordRepository.DeleteUserWordAsync(userWord);
            logger.LogInformation("Користувач {UserId} видалив слово {WordId} зі збережених.", userId, wordId);
            return Result.Success();
        }

        public async Task<PagedResult<PersonalWordDTO>> GetPersonalWordsAsync(string userId, string? searchTerm, string? category, string? partOfSpeech, string? difficultyLevel, int page, int pageSize)
        {
            logger.LogInformation("Отримання сторінки слів для {UserId}: page={Page}, pageSize={PageSize}", userId, page, pageSize);

            if (page <= 0)
            {
                page = 1;
            }

            if (pageSize <= 0)
            {
                pageSize = 20;
            }

            var (itemsUw, total) = await wordRepository.GetPersonalWordsPageAsync(userId, searchTerm, category, partOfSpeech, difficultyLevel, page, pageSize);

            var items = itemsUw.Select(uw => new PersonalWordDTO
            {
                Id = uw.Word.Id,
                Term = uw.Word.Term,
                Translation = uw.Word.Translation,
                Transcription = uw.Word.Transcription,
                Meaning = uw.Word.Meaning,
                PartOfSpeech = uw.Word.PartOfSpeech,
                Example = uw.Word.Example,
                DifficultyLevel = uw.Word.DifficultyLevel,
                AddedAt = uw.SavedAt,
                CategoryName = uw.Word.Categories?.OrderBy(c => c.Id).Select(c => c.Name).FirstOrDefault() ?? "General"
            }).ToList();

            return new PagedResult<PersonalWordDTO>
            {
                Items = items,
                TotalCount = total,
                Page = page,
                PageSize = pageSize,
            };
        }

        public async Task<PagedResult<SystemWordDTO>> GetSystemWordsAsync(string userId, string? searchTerm, string? category, string? partOfSpeech, string? difficultyLevel, int page, int pageSize)
        {
            logger.LogInformation("Getting system words page for {UserId}: page={Page}, pageSize={PageSize}", userId, page, pageSize);

            if (page <= 0)
            {
                page = 1;
            }

            if (pageSize <= 0)
            {
                pageSize = 20;
            }

            var (itemsResult, total) = await wordRepository.GetSystemWordsPageAsync(userId, searchTerm, category, partOfSpeech, difficultyLevel, page, pageSize);

            var items = itemsResult.Select(result => new SystemWordDTO
            {
                Id = result.word.Id,
                Term = result.word.Term,
                Translation = result.word.Translation,
                Transcription = result.word.Transcription,
                Meaning = result.word.Meaning,
                PartOfSpeech = result.word.PartOfSpeech,
                Example = result.word.Example,
                DifficultyLevel = result.word.DifficultyLevel,
                CategoryName = result.word.Categories?.OrderBy(c => c.Id).Select(c => c.Name).FirstOrDefault() ?? "General",
                IsSaved = result.isSaved
            }).ToList();

            return new PagedResult<SystemWordDTO>
            {
                Items = items,
                TotalCount = total,
                Page = page,
                PageSize = pageSize,
            };
        }

        public async Task<Result> SaveSystemWordAsync(string userId, int wordId)
        {
            var word = await wordRepository.GetWordAsync(wordId);

            if (word == null || !word.IsGlobal)
            {
                return Result.Failure("Word not found or is not a system word.");
            }

            var alreadySaved = await wordRepository.UserHasWordAsync(userId, wordId);
            if (alreadySaved)
            {
                return Result.Failure("You have already saved this word.");
            }

            var userWord = new UserWord
            {
                UserId = userId,
                WordId = wordId,
                SavedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified)
            };

            await wordRepository.SaveUserWordAsync(userWord);

            logger.LogInformation("User {UserId} saved global word {WordId}.", userId, wordId);

            return Result.Success();
        }

        public async Task<IEnumerable<Category>> GetAllCategoriesAsync()
        {
            return await categoryRepository.GetAllCategoriesAsync();
        }

        public async Task<IEnumerable<Category>> GetCategoriesByIdsAsync(IEnumerable<int> ids)
        {
            return await categoryRepository.GetCategoriesByIdsAsync(ids);
        }

        public async Task<Category> CreateCategoryAsync(string name)
        {
            var existing = await categoryRepository.GetCategoryByNameAsync(name);
            if (existing != null)
            {
                return existing;
            }

            var cat = new Category { Name = name.Trim() };
            return await categoryRepository.CreateCategoryAsync(cat);
        }

        public async Task<Category?> GetCategoryByNameAsync(string name)
        {
            return await categoryRepository.GetCategoryByNameAsync(name);
        }
    }
}