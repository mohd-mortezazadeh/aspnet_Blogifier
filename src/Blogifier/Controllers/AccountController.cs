using Blogifier.Blogs;
using Blogifier.Data.Migrations;
using Blogifier.Identity;
using Blogifier.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Blogifier.Controllers;

[Route("account")]
public class AccountController : Controller
{
  protected readonly ILogger _logger;
  protected readonly UserManager _userManager;
  protected readonly SignInManager _signInManager;
  protected readonly BlogManager _blogManager;

  public AccountController(
    ILogger<AccountController> logger,
    UserManager userManager,
    SignInManager signInManager,
    BlogManager blogManager)
  {
    _logger = logger;
    _userManager = userManager;
    _signInManager = signInManager;
    _blogManager = blogManager;
  }

  [HttpGet]
  public IActionResult Index([FromQuery] AccountModel parameter) => RedirectToAction("login", routeValues: parameter);

  [HttpGet("login")]
  public async Task<IActionResult> Login([FromQuery] AccountModel parameter)
  {
    var data = await _blogManager.GetAsync();
    var model = new AccountLoginModel { RedirectUri = parameter.RedirectUri };
    return View($"~/Views/Themes/{data.Theme}/login.cshtml", model);
  }

  [HttpPost("login")]
  public async Task<IActionResult> LoginForm([FromForm] AccountLoginModel model)
  {
    if (ModelState.IsValid)
    {
      var user = await _userManager.FindByEmailAsync(model.Email);
      if (user != null)
      {
        var result = await _signInManager.PasswordSignInAsync(user, model.Password, true, lockoutOnFailure: true);
        if (result.Succeeded)
        {
          _logger.LogInformation("User logged in.");
          model.RedirectUri ??= "/";
          return LocalRedirect(model.RedirectUri);
        }
      }
    }
    model.ShowError = true;
    var data = await _blogManager.GetAsync();
    return View($"~/Views/Themes/{data.Theme}/account/login.cshtml", model);
  }

  [HttpGet("register")]
  public async Task<IActionResult> Register([FromQuery] AccountModel parameter)
  {
    var model = new AccountRegisterModel { RedirectUri = parameter.RedirectUri };
    var data = await _blogManager.GetAsync();
    return View($"~/Views/Themes/{data.Theme}/register.cshtml", model);
  }

  [HttpPost("register")]
  public async Task<IActionResult> RegisterForm([FromForm] AccountRegisterModel model)
  {
    if (ModelState.IsValid)
    {
      var user = new UserInfo(model.UserName)
      {
        NickName = model.NickName,
        Email = model.Email
      };
      var result = await _userManager.CreateAsync(user, model.Password);
      if (result.Succeeded)
      {
        return RedirectToAction("login", routeValues: new AccountModel { RedirectUri = model.RedirectUri });
      }
    }
    model.ShowError = true;
    var data = await _blogManager.GetAsync();
    return View($"~/Views/Themes/{data.Theme}/account/register.cshtml", model);
  }

  [HttpGet("logout")]
  public async Task<IActionResult> Logout()
  {
    await _signInManager.SignOutAsync();
    return Redirect("~/");
  }

  [HttpGet("initialize")]
  public async Task<IActionResult> Initialize([FromQuery] AccountModel parameter)
  {
    if (await _blogManager.AnyAsync())
      return RedirectToAction("login", routeValues: parameter);

    var model = new AccountInitializeModel { RedirectUri = parameter.RedirectUri };
    return View($"~/Views/Themes/{BlogifierConstant.DefaultTheme}/initialize.cshtml", model);
  }

  [HttpPost("initialize")]
  public async Task<IActionResult> InitializeForm([FromForm] AccountInitializeModel model)
  {
    if (await _blogManager.AnyAsync())
      return RedirectToAction("login", routeValues: new AccountModel { RedirectUri = model.RedirectUri });

    if (ModelState.IsValid)
    {
      var user = new UserInfo(model.UserName)
      {
        NickName = model.NickName,
        Email = model.Email
      };
      var result = await _userManager.CreateAsync(user, model.Password);
      if (result.Succeeded)
      {
        await _userManager.AddClaimAsync(user, new Claim(BlogifierClaimTypes.AuthorityAdmin, "y"));
        var blogData = new BlogData
        {
          Title = model.Title,
          Description = model.Description,
          Theme = BlogifierConstant.DefaultTheme,
          ItemsPerPage = BlogifierConstant.DefaultItemsPerPage,
          Version = BlogifierConstant.DefaultVersion,
          Logo = BlogifierConstant.DefaultLogo
        };
        await _blogManager.SetAsync(blogData);
        return Redirect("~/");
      }
    }
    model.ShowError = true;
    return View($"~/Views/Themes/{BlogifierConstant.DefaultTheme}/initialize.cshtml", model);
  }


  [HttpGet("profile")]
  public async Task<IActionResult> Profile([FromQuery] AccountModel parameter)
  {
    var userId = User.FirstUserId();
    var user = (await _userManager.FindByIdAsync(userId))!;
    var model = new AccountProfileEditModel
    {
      RedirectUri = parameter.RedirectUri,
      IsProfile = true,
      Email = user.Email,
      NickName = user.NickName,
      Avatar = user.Avatar,
      Bio = user.Bio,
    };
    var data = await _blogManager.GetAsync();
    return View($"~/Views/Themes/{data.Theme}/profile.cshtml", model);
  }

  [HttpPost("profile")]
  public async Task<IActionResult> ProfileForm([FromForm] AccountProfileEditModel model)
  {
    if (ModelState.IsValid)
    {
      var userId = User.FirstUserId();
      var user = (await _userManager.FindByIdAsync(userId))!;
      user.Email = model.Email;
      user.NickName = model.NickName;
      user.Avatar = model.Avatar;
      user.Bio = model.Bio;
      await _userManager.UpdateAsync(user);
      await _signInManager.SignInAsync(user, isPersistent: true);
    }
    var data = await _blogManager.GetAsync();
    return View($"~/Views/Themes/{data.Theme}/profile.cshtml", model);
  }
}
