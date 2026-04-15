using System.ComponentModel.DataAnnotations;

namespace Enrich.Web.ViewModels
{
    public class CategoryViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Category name is required.")]
        [StringLength(50, ErrorMessage = "Name cannot exceed 50 characters.")]
        [Display(Name = "Category Name")]
        public string Name { get; set; } = string.Empty;
    }
}