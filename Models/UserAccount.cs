using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace CramsRBIApp.Models
{
    public class UserAccount
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Username is required")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;


    }

    public class ApplicationUser : IdentityUser<int>
    {
        // Add your custom user properties here
        public string FirstName { get; set; }
        public string LastName { get; set; }
        // You can keep a reference to your existing UserAccount if needed
        // public int UserAccountId { get; set; }
        // public UserAccount UserAccount { get; set; }
    }

    // Custom ApplicationRole class with int as the key type
    public class ApplicationRole : IdentityRole<int>
    {
        public ApplicationRole() : base() { }

        public ApplicationRole(string roleName) : base(roleName) { }

        // Add your custom role properties here
        public string Description { get; set; }
    }

    public class RolePermission
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string MenuName { get; set; } = string.Empty; // USER, REPORT, ANALYTICS

        [Required]
        [StringLength(20)]
        public string PermissionType { get; set; } = string.Empty; // EDIT, VIEW

        public int RoleId { get; set; }
        public ApplicationRole Role { get; set; }

        public bool IsGranted { get; set; } = false;
    }
}
