// RoleViewModel.cs - Add this to your ViewModel folder
using System.ComponentModel.DataAnnotations;

namespace CramsRBIApp.ViewModel
{
    public class RoleViewModel
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "Role name is required")]
        [Display(Name = "Role Name")]
        [StringLength(50, ErrorMessage = "Role name cannot exceed 50 characters")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Description is required")]
        [Display(Name = "Description")]
        [StringLength(255, ErrorMessage = "Description cannot exceed 255 characters")]
        public string Description { get; set; }
    }
}