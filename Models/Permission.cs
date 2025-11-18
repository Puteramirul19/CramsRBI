using System.ComponentModel.DataAnnotations;

namespace CramsRBIApp.Models
{
    public class Permission
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string MenuName { get; set; } = string.Empty; // USER, REPORT, ANALYTICS

        [Required]
        public string PermissionType { get; set; } = string.Empty; // EDIT, VIEW

        public int RoleId { get; set; }
        public ApplicationRole Role { get; set; }

        public bool IsGranted { get; set; } = false;
    }

    public class MenuPermission
    {
        public string MenuName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
    }
}
