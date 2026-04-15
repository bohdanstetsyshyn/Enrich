namespace Enrich.BLL.DTOs
{
    public class BundleDTO
    {
        public int Id { get; set; }

        public string Title { get; set; } = null!;

        public string? Description { get; set; }

        public string[] DifficultyLevels { get; set; } = [];

        public string? ImageUrl { get; set; }

        public string Status { get; set; } = null!;

        public bool IsPublic { get; set; }

        public bool IsSystem { get; set; }

        public string? OwnerId { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public int WordCount { get; set; }

        public int CategoryCount { get; set; }

        public int TagCount { get; set; }

        public List<string> Categories { get; set; } = [];

        public List<int> CategoryIds { get; set; } = [];

        public List<int> WordIds { get; set; } = [];

        public List<WordItemViewModel> Words { get; set; } = new();
    }
}
