using CramsRBIApp.Data;
using CramsRBIApp.Models;
using CramsRBIApp.ViewModel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace CramsRBIApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<ApplicationRole> roleManager,
            ApplicationDbContext context,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Login(string returnUrl = null)
        {
            // TEMPORARY: Force create admin user for testing
            var adminEmail = "azlia.azizi87@gmail.com";
            var adminUser = await _userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                // Create Admin role if it doesn't exist
                if (!await _roleManager.RoleExistsAsync("Admin"))
                {
                    await _roleManager.CreateAsync(new ApplicationRole("Admin")
                    {
                        Description = "Administrator role"
                    });
                }

                // Create admin user
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    FirstName = "Azlia",
                    LastName = "Azizi"
                };

                var result = await _userManager.CreateAsync(adminUser, "P@ssw0rd!");

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(adminUser, "Admin");
                    TempData["SuccessMessage"] = "Admin user created successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to create admin user: " +
                        string.Join(", ", result.Errors.Select(e => e.Description));
                }
            }

            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                // This doesn't count login failures towards account lockout
                // To enable password failures to trigger account lockout, set lockoutOnFailure: true
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in.");
                    return RedirectToLocal(returnUrl ?? "/Home/Index");
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out.");
                    return RedirectToAction(nameof(Lockout));
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return View(model);
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        [HttpGet]
        public IActionResult Lockout()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            return RedirectToAction("Login");
        }

        [Authorize]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Register()
        {
            try
            {
                _logger.LogInformation("Register GET method called");

                // Get all available roles
                var roles = await _roleManager.Roles.ToListAsync();

                if (roles == null || !roles.Any())
                {
                    _logger.LogWarning("No roles found in the database");
                    TempData["ErrorMessage"] = "No roles found in the database. Please create roles first.";
                }
                else
                {
                    _logger.LogInformation($"Found {roles.Count} roles");
                    foreach (var role in roles)
                    {
                        _logger.LogInformation($"Role: {role.Name}, ID: {role.Id}");
                    }
                }

                var viewModel = new RegisterViewModel
                {
                    AvailableRoles = roles?.Select(r => new SelectListItem
                    {
                        Text = r.Name,
                        Value = r.Id.ToString()
                    }).ToList() ?? new List<SelectListItem>()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in Register GET method");
                TempData["ErrorMessage"] = "An error occurred while loading the registration page.";

                // Still try to show the form, but with an empty roles list
                return View(new RegisterViewModel
                {
                    AvailableRoles = new List<SelectListItem>()
                });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            try
            {
                _logger.LogInformation("Register POST method called");

                // Repopulate the roles dropdown in case we need to return to the form
                var roles = await _roleManager.Roles.ToListAsync();
                model.AvailableRoles = roles.Select(r => new SelectListItem
                {
                    Text = r.Name,
                    Value = r.Id.ToString()
                }).ToList();

                // Log all model values for debugging
                _logger.LogInformation($"Registration attempt - Email: {model.Email}, FirstName: {model.FirstName}, " +
                                    $"LastName: {model.LastName}, PhoneNumber: {model.PhoneNumber}, RoleId: {model.RoleId}");

                // Check if the model is valid
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("ModelState is invalid");
                    foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                    {
                        _logger.LogWarning($"ModelState error: {error.ErrorMessage}");
                    }
                    return View(model);
                }

                _logger.LogInformation("ModelState is valid, creating user");

                // Create the user
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    PhoneNumber = model.PhoneNumber, // Add phone number
                    EmailConfirmed = true // Setting this to true to avoid email confirmation requirement
                };

                // Attempt to create the user with the provided password
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation($"User {user.Email} created successfully with ID: {user.Id}");

                    // Get the role name by ID
                    var role = await _roleManager.FindByIdAsync(model.RoleId);
                    if (role != null)
                    {
                        _logger.LogInformation($"Found role: {role.Name} with ID: {role.Id}");

                        // Add user to the role
                        var roleResult = await _userManager.AddToRoleAsync(user, role.Name);

                        if (roleResult.Succeeded)
                        {
                            _logger.LogInformation($"User {user.Email} added to role {role.Name}");
                            TempData["SuccessMessage"] = $"User {user.Email} has been successfully registered with role {role.Name}.";
                            return RedirectToAction("UserList");
                        }
                        else
                        {
                            _logger.LogError("Failed to add user to role");
                            foreach (var error in roleResult.Errors)
                            {
                                ModelState.AddModelError(string.Empty, error.Description);
                                _logger.LogError($"Role assignment error: {error.Description}");
                            }

                            // If role assignment fails, delete the user to avoid orphaned accounts
                            await _userManager.DeleteAsync(user);
                            TempData["ErrorMessage"] = "Failed to assign role to the user.";
                        }
                    }
                    else
                    {
                        _logger.LogError($"Role with ID {model.RoleId} not found");
                        ModelState.AddModelError(string.Empty, "Selected role was not found.");
                        TempData["ErrorMessage"] = "The selected role does not exist.";

                        // If role not found, delete the user to avoid orphaned accounts
                        await _userManager.DeleteAsync(user);
                    }
                }
                else
                {
                    _logger.LogError("Failed to create user");
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                        _logger.LogError($"User creation error: {error.Description}");
                    }
                    TempData["ErrorMessage"] = "Failed to create user. Please check the validation errors.";
                }

                // If we got this far, something failed, redisplay form
                return View(model);
            }
            catch (Exception ex)
            {
                // Log any unhandled exceptions
                _logger.LogError(ex, "Exception in Register POST method");
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
                TempData["ErrorMessage"] = "An unexpected error occurred. Please try again later.";
                return View(model);
            }
        }

        // Method to test database connection
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> TestDatabase()
        {
            try
            {
                _logger.LogInformation("Testing database connection and Entity Framework setup");

                // Check if roles table has data
                var roles = await _roleManager.Roles.ToListAsync();
                int roleCount = roles.Count;

                // Check if users table has data
                var users = await _userManager.Users.ToListAsync();
                int userCount = users.Count;

                TempData["SuccessMessage"] = $"Database connection successful! Found {roleCount} roles and {userCount} users.";

                // Log the details
                _logger.LogInformation($"Database test: Found {roleCount} roles and {userCount} users");

                if (roles.Any())
                {
                    foreach (var role in roles)
                    {
                        _logger.LogInformation($"Role ID: {role.Id}, Name: {role.Name}");
                    }
                }

                if (users.Any())
                {
                    foreach (var user in users)
                    {
                        _logger.LogInformation($"User ID: {user.Id}, Email: {user.Email}, Name: {user.FirstName} {user.LastName}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database test failed");
                TempData["ErrorMessage"] = $"Database test failed: {ex.Message}";
            }

            return RedirectToAction("Register");
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UserList(string sortField, string sortOrder, string currentFilter, string searchString, int? pageNumber)
        {
            try
            {
                // Set default sort field if none provided
                if (string.IsNullOrEmpty(sortField))
                {
                    sortField = "LastName";
                }

                // Toggle sort order
                ViewData["CurrentSort"] = sortField;
                ViewData["NameSortParam"] = sortField == "LastName" ? (sortOrder == "asc" ? "desc" : "asc") : "asc";
                ViewData["EmailSortParam"] = sortField == "Email" ? (sortOrder == "asc" ? "desc" : "asc") : "asc";
                ViewData["RoleSortParam"] = sortField == "Role" ? (sortOrder == "asc" ? "desc" : "asc") : "asc";

                // Handle search and pagination
                if (searchString != null)
                {
                    pageNumber = 1;
                }
                else
                {
                    searchString = currentFilter;
                }

                ViewData["CurrentFilter"] = searchString;

                // Get all users with their roles
                var usersWithRoles = new List<UserViewModel>();
                var users = _userManager.Users.AsQueryable();

                // Apply search filter if provided
                if (!string.IsNullOrEmpty(searchString))
                {
                    users = users.Where(u =>
                        u.FirstName.Contains(searchString) ||
                        u.LastName.Contains(searchString) ||
                        u.Email.Contains(searchString)
                    );
                }

                // Apply sorting
                users = ApplySorting(users, sortField, sortOrder);

                // Get total count for pagination
                int totalCount = await users.CountAsync();

                // Set page size and get the current page
                int pageSize = 10;
                int currentPage = pageNumber ?? 1;

                // Get the users for the current page
                var pagedUsers = await users
                    .Skip((currentPage - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // Populate the view model with user data and roles
                foreach (var user in pagedUsers)
                {
                    var roles = await _userManager.GetRolesAsync(user);

                    usersWithRoles.Add(new UserViewModel
                    {
                        Id = user.Id,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Email = user.Email,
                        PhoneNumber = user.PhoneNumber,
                        EmailConfirmed = user.EmailConfirmed,
                        Roles = roles.ToList()
                    });
                }

                // Create the view model
                var viewModel = new UserListViewModel
                {
                    Users = usersWithRoles,
                    PaginationInfo = new PaginationInfo
                    {
                        TotalItems = totalCount,
                        ItemsPerPage = pageSize,
                        CurrentPage = currentPage
                    },
                    SearchString = searchString,
                    SortField = sortField,
                    SortOrder = sortOrder,
                    CurrentFilter = currentFilter
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UserList method");
                TempData["ErrorMessage"] = "An error occurred while retrieving the user list.";
                return RedirectToAction("Index", "Home");
            }
        }

        private IQueryable<ApplicationUser> ApplySorting(IQueryable<ApplicationUser> users, string sortField, string sortOrder)
        {
            bool isAscending = sortOrder != "desc";

            switch (sortField)
            {
                case "FirstName":
                    users = isAscending
                        ? users.OrderBy(u => u.FirstName)
                        : users.OrderByDescending(u => u.FirstName);
                    break;
                case "LastName":
                    users = isAscending
                        ? users.OrderBy(u => u.LastName)
                        : users.OrderByDescending(u => u.LastName);
                    break;
                case "Email":
                    users = isAscending
                        ? users.OrderBy(u => u.Email)
                        : users.OrderByDescending(u => u.Email);
                    break;
                default:
                    users = isAscending
                        ? users.OrderBy(u => u.LastName)
                        : users.OrderByDescending(u => u.LastName);
                    break;
            }

            return users;
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> EditUser(int id)
        {
            try
            {
                _logger.LogInformation($"EditUser GET method called for user ID: {id}");

                // Find the user by ID
                var user = await _userManager.FindByIdAsync(id.ToString());
                if (user == null)
                {
                    _logger.LogWarning($"User with ID {id} not found");
                    TempData["ErrorMessage"] = "User not found.";
                    return RedirectToAction("UserList");
                }

                // Get all available roles
                var roles = await _roleManager.Roles.ToListAsync();

                // Get user's current role
                var userRoles = await _userManager.GetRolesAsync(user);
                string currentRoleId = string.Empty;

                if (userRoles.Any())
                {
                    var currentRole = await _roleManager.FindByNameAsync(userRoles.First());
                    if (currentRole != null)
                    {
                        currentRoleId = currentRole.Id.ToString();
                    }
                }

                // Create the view model
                var viewModel = new EditUserViewModel
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    EmailConfirmed = user.EmailConfirmed,
                    RoleId = currentRoleId,
                    AvailableRoles = roles.Select(r => new SelectListItem
                    {
                        Text = r.Name,
                        Value = r.Id.ToString(),
                        Selected = currentRoleId == r.Id.ToString()
                    }).ToList()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in EditUser GET method for user ID: {id}");
                TempData["ErrorMessage"] = "An error occurred while loading the user information.";
                return RedirectToAction("UserList");
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(EditUserViewModel model)
        {
            try
            {
                _logger.LogInformation($"EditUser POST method called for user ID: {model.Id}");

                // Repopulate the roles dropdown in case we need to return to the form
                var roles = await _roleManager.Roles.ToListAsync();
                model.AvailableRoles = roles.Select(r => new SelectListItem
                {
                    Text = r.Name,
                    Value = r.Id.ToString(),
                    Selected = r.Id.ToString() == model.RoleId
                }).ToList();

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("ModelState is invalid for EditUser");
                    foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                    {
                        _logger.LogWarning($"ModelState error: {error.ErrorMessage}");
                    }
                    return View(model);
                }

                // Find the user
                var user = await _userManager.FindByIdAsync(model.Id.ToString());
                if (user == null)
                {
                    _logger.LogWarning($"User with ID {model.Id} not found during POST");
                    TempData["ErrorMessage"] = "User not found.";
                    return RedirectToAction("UserList");
                }

                // Update user properties
                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.Email = model.Email;
                user.UserName = model.Email; // In case email is used as username
                user.PhoneNumber = model.PhoneNumber;
                user.EmailConfirmed = model.EmailConfirmed;

                // Update the user
                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    _logger.LogError("Failed to update user");
                    foreach (var error in updateResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                        _logger.LogError($"User update error: {error.Description}");
                    }
                    return View(model);
                }

                // Only update password if provided
                if (!string.IsNullOrEmpty(model.Password))
                {
                    _logger.LogInformation("Updating user password");

                    // Remove current password
                    var removePasswordResult = await _userManager.RemovePasswordAsync(user);
                    if (!removePasswordResult.Succeeded)
                    {
                        _logger.LogError("Failed to remove current password");
                        foreach (var error in removePasswordResult.Errors)
                        {
                            _logger.LogError($"Password removal error: {error.Description}");
                        }
                        TempData["ErrorMessage"] = "Failed to update password, but other changes were saved.";
                        return RedirectToAction("UserList");
                    }

                    // Add new password
                    var addPasswordResult = await _userManager.AddPasswordAsync(user, model.Password);
                    if (!addPasswordResult.Succeeded)
                    {
                        _logger.LogError("Failed to update password");
                        foreach (var error in addPasswordResult.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                            _logger.LogError($"Password update error: {error.Description}");
                        }
                        TempData["ErrorMessage"] = "Failed to update password, but other changes were saved.";
                        return RedirectToAction("UserList");
                    }

                    _logger.LogInformation("Password updated successfully");
                }
                else
                {
                    _logger.LogInformation("No password change requested, keeping current password");
                }

                // Update role if changed
                if (!string.IsNullOrEmpty(model.RoleId))
                {
                    // Get the selected role
                    var newRole = await _roleManager.FindByIdAsync(model.RoleId);
                    if (newRole != null)
                    {
                        // Get current roles
                        var currentRoles = await _userManager.GetRolesAsync(user);

                        // Remove from current roles
                        if (currentRoles.Any())
                        {
                            await _userManager.RemoveFromRolesAsync(user, currentRoles);
                        }

                        // Add to new role
                        var roleResult = await _userManager.AddToRoleAsync(user, newRole.Name);

                        if (!roleResult.Succeeded)
                        {
                            _logger.LogError("Failed to update user role");
                            foreach (var error in roleResult.Errors)
                            {
                                ModelState.AddModelError(string.Empty, error.Description);
                                _logger.LogError($"Role update error: {error.Description}");
                            }
                            return View(model);
                        }
                    }
                }

                TempData["SuccessMessage"] = $"User {user.Email} has been successfully updated.";
                return RedirectToAction("UserList");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception in EditUser POST method for user ID: {model.Id}");
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
                return View(model);
            }
        }


        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                _logger.LogInformation($"DeleteUser method called for user ID: {id}");

                // Find the user
                var user = await _userManager.FindByIdAsync(id.ToString());
                if (user == null)
                {
                    _logger.LogWarning($"User with ID {id} not found");
                    TempData["ErrorMessage"] = "User not found.";
                    return RedirectToAction("UserList");
                }

                // Store the user's name for the success message
                string userName = $"{user.FirstName} {user.LastName}";

                // Check if attempting to delete the current user
                if (User.Identity.Name == user.Email)
                {
                    _logger.LogWarning("Attempted to delete current user");
                    TempData["ErrorMessage"] = "You cannot delete your own account.";
                    return RedirectToAction("UserList");
                }

                // Delete the user
                var result = await _userManager.DeleteAsync(user);
                if (result.Succeeded)
                {
                    _logger.LogInformation($"User {user.Email} deleted successfully");
                    TempData["SuccessMessage"] = $"User {userName} has been successfully deleted.";
                }
                else
                {
                    _logger.LogError("Failed to delete user");
                    foreach (var error in result.Errors)
                    {
                        _logger.LogError($"User deletion error: {error.Description}");
                    }
                    TempData["ErrorMessage"] = "Failed to delete user. Please try again.";
                }

                return RedirectToAction("UserList");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception in DeleteUser method for user ID: {id}");
                TempData["ErrorMessage"] = "An error occurred while deleting the user.";
                return RedirectToAction("UserList");
            }
        }


        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult RoleList()
        {
            var roles = _roleManager.Roles.ToList();
            return View(roles);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult CreateRole()
        {
            return View(new RoleViewModel());
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRole(RoleViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Check if role with the same name already exists
                    var roleExists = await _roleManager.RoleExistsAsync(model.Name);
                    if (roleExists)
                    {
                        ModelState.AddModelError("Name", "Role with this name already exists");
                        return View(model);
                    }

                    // Create new role
                    var role = new ApplicationRole
                    {
                        Name = model.Name,
                        Description = model.Description
                    };

                    var result = await _roleManager.CreateAsync(role);

                    if (result.Succeeded)
                    {
                        _logger.LogInformation($"Role {role.Name} created successfully");
                        TempData["SuccessMessage"] = $"Role {role.Name} has been successfully created.";
                        return RedirectToAction("RoleList");
                    }
                    else
                    {
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                            _logger.LogError($"Role creation error: {error.Description}");
                        }
                    }
                }

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateRole method");
                TempData["ErrorMessage"] = "An error occurred while creating the role.";
                return View(model);
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> EditRole(int id)
        {
            var role = await _roleManager.FindByIdAsync(id.ToString());
            if (role == null)
            {
                TempData["ErrorMessage"] = "Role not found.";
                return RedirectToAction("RoleList");
            }

            var model = new RoleViewModel
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description
            };

            return View(model);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRole(RoleViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var role = await _roleManager.FindByIdAsync(model.Id.ToString());
                    if (role == null)
                    {
                        TempData["ErrorMessage"] = "Role not found.";
                        return RedirectToAction("RoleList");
                    }

                    // Check if trying to update to an existing role name
                    var existingRole = await _roleManager.FindByNameAsync(model.Name);
                    if (existingRole != null && existingRole.Id != model.Id)
                    {
                        ModelState.AddModelError("Name", "Role with this name already exists");
                        return View(model);
                    }

                    role.Name = model.Name;
                    role.Description = model.Description;

                    var result = await _roleManager.UpdateAsync(role);

                    if (result.Succeeded)
                    {
                        _logger.LogInformation($"Role {role.Name} updated successfully");
                        TempData["SuccessMessage"] = $"Role {role.Name} has been successfully updated.";
                        return RedirectToAction("RoleList");
                    }
                    else
                    {
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                            _logger.LogError($"Role update error: {error.Description}");
                        }
                    }
                }

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in EditRole method");
                TempData["ErrorMessage"] = "An error occurred while updating the role.";
                return View(model);
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRole(int id)
        {
            try
            {
                var role = await _roleManager.FindByIdAsync(id.ToString());
                if (role == null)
                {
                    TempData["ErrorMessage"] = "Role not found.";
                    return RedirectToAction("RoleList");
                }

                // Check if role is in use
                var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name);
                if (usersInRole.Any())
                {
                    TempData["ErrorMessage"] = $"Cannot delete role {role.Name} because it has {usersInRole.Count} users assigned to it.";
                    return RedirectToAction("RoleList");
                }

                var result = await _roleManager.DeleteAsync(role);

                if (result.Succeeded)
                {
                    _logger.LogInformation($"Role {role.Name} deleted successfully");
                    TempData["SuccessMessage"] = $"Role {role.Name} has been successfully deleted.";
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        _logger.LogError($"Role deletion error: {error.Description}");
                    }
                    TempData["ErrorMessage"] = "Failed to delete role. Please try again.";
                }

                return RedirectToAction("RoleList");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteRole method");
                TempData["ErrorMessage"] = "An error occurred while deleting the role.";
                return RedirectToAction("RoleList");
            }
        }

        //add permission

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> RolePermissions(int? roleId)
        {
            try
            {
                // Get all roles for dropdown
                var roles = await _roleManager.Roles.ToListAsync();

                // Define available menus
                var availableMenus = new List<MenuPermissionItem>
        {
            new MenuPermissionItem { MenuName = "USER", DisplayName = "User Management", Icon = "fa-users" },
            new MenuPermissionItem { MenuName = "REPORT", DisplayName = "Reports", Icon = "fa-chart-pie" },
            new MenuPermissionItem { MenuName = "ANALYTICS", DisplayName = "Analytics", Icon = "fa-chart-line" }
        };

                var viewModel = new RolePermissionViewModel
                {
                    AvailableRoles = roles.Select(r => new SelectListItem
                    {
                        Text = r.Name,
                        Value = r.Id.ToString(),
                        Selected = r.Id == roleId
                    }).ToList(),
                    MenuPermissions = availableMenus
                };

                if (roleId.HasValue)
                {
                    var selectedRole = await _roleManager.FindByIdAsync(roleId.Value.ToString());
                    if (selectedRole != null)
                    {
                        viewModel.RoleId = roleId.Value;
                        viewModel.RoleName = selectedRole.Name;

                        // Load existing permissions for this role from database
                        var permissions = await _context.Permissions
                            .Where(p => p.RoleId == roleId.Value)
                            .ToListAsync();

                        // Set permissions for each menu
                        foreach (var menu in viewModel.MenuPermissions)
                        {
                            var editPermission = permissions.FirstOrDefault(p =>
                                p.MenuName == menu.MenuName && p.PermissionType == "EDIT");
                            var viewPermission = permissions.FirstOrDefault(p =>
                                p.MenuName == menu.MenuName && p.PermissionType == "VIEW");

                            menu.CanEdit = editPermission?.IsGranted ?? false;
                            menu.CanView = viewPermission?.IsGranted ?? false;
                        }
                    }
                }

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RolePermissions method");
                TempData["ErrorMessage"] = "An error occurred while loading role permissions.";
                return RedirectToAction("RoleList");
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRolePermissions(RolePermissionViewModel model)
        {
            try
            {
                _logger.LogInformation($"Updating permissions for role ID: {model.RoleId}");

                var role = await _roleManager.FindByIdAsync(model.RoleId.ToString());
                if (role == null)
                {
                    TempData["ErrorMessage"] = "Role not found.";
                    return RedirectToAction("RoleList");
                }

                // Remove existing permissions for this role
                var existingPermissions = await _context.Permissions
                    .Where(p => p.RoleId == model.RoleId)
                    .ToListAsync();

                _context.Permissions.RemoveRange(existingPermissions);

                // Add new permissions based on the form data
                foreach (var menu in model.MenuPermissions)
                {
                    if (menu.CanView)
                    {
                        _context.Permissions.Add(new Permission
                        {
                            RoleId = model.RoleId,
                            MenuName = menu.MenuName,
                            PermissionType = "VIEW",
                            IsGranted = true
                        });
                    }

                    if (menu.CanEdit)
                    {
                        _context.Permissions.Add(new Permission
                        {
                            RoleId = model.RoleId,
                            MenuName = menu.MenuName,
                            PermissionType = "EDIT",
                            IsGranted = true
                        });
                    }
                }

                // Save changes to database
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Successfully updated permissions for role: {role.Name}");
                TempData["SuccessMessage"] = $"Permissions for role '{role.Name}' have been updated successfully.";

                return RedirectToAction("RolePermissions", new { roleId = model.RoleId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating role permissions");
                TempData["ErrorMessage"] = "An error occurred while updating permissions.";
                return RedirectToAction("RolePermissions", new { roleId = model.RoleId });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> PermissionMatrix()
        {
            try
            {
                var roles = await _roleManager.Roles.ToListAsync();

                var menus = new List<MenuPermissionItem>
        {
            new MenuPermissionItem { MenuName = "USER", DisplayName = "User Management", Icon = "fa-users" },
            new MenuPermissionItem { MenuName = "REPORT", DisplayName = "Reports", Icon = "fa-chart-pie" },
            new MenuPermissionItem { MenuName = "ANALYTICS", DisplayName = "Analytics", Icon = "fa-chart-line" }
        };

                var permissionMatrix = new Dictionary<string, Dictionary<string, bool>>();

                // Load actual permissions from database
                var allPermissions = await _context.Permissions
                    .Include(p => p.Role)
                    .Where(p => p.IsGranted)
                    .ToListAsync();

                // Build permission matrix with real data
                foreach (var role in roles)
                {
                    permissionMatrix[role.Name] = new Dictionary<string, bool>();

                    foreach (var menu in menus)
                    {
                        var editPermission = allPermissions.Any(p =>
                            p.RoleId == role.Id &&
                            p.MenuName == menu.MenuName &&
                            p.PermissionType == "EDIT" &&
                            p.IsGranted);

                        var viewPermission = allPermissions.Any(p =>
                            p.RoleId == role.Id &&
                            p.MenuName == menu.MenuName &&
                            p.PermissionType == "VIEW" &&
                            p.IsGranted);

                        permissionMatrix[role.Name][$"{menu.MenuName}_EDIT"] = editPermission;
                        permissionMatrix[role.Name][$"{menu.MenuName}_VIEW"] = viewPermission;
                    }
                }

                var viewModel = new PermissionMatrixViewModel
                {
                    Roles = roles,
                    Menus = menus,
                    PermissionMatrix = permissionMatrix
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in PermissionMatrix method");
                TempData["ErrorMessage"] = "An error occurred while loading the permission matrix.";
                return RedirectToAction("RoleList");
            }
        }

    }
}