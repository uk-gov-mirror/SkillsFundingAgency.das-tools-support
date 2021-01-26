using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SFA.DAS.Tools.Support.Infrastructure.Services;
using SFA.DAS.Tools.Support.Web.Configuration;
using SFA.DAS.Tools.Support.Web.Models;

namespace SFA.DAS.Tools.Support.Web.Controllers
{
    [Route("support/usersearch")]
    public class UserSearchController : Controller
    {
        private readonly IEmployerAccountsService _accountsService;
        private readonly ILogger<UserSearchController> _logger;

        public UserSearchController(ILogger<UserSearchController> logger, IEmployerAccountsService accountsService)
        {
            _accountsService = accountsService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index(string accountId, string act)
        {
            switch(act)
            {
                case ActionNames.Suspend:
                    ViewData.Add("FormActionRoute", RouteNames.SuspendUsers);
                    ViewData.Add("FormActionText", "Suspend user(s)");
                break;
                case ActionNames.Resume:
                    ViewData.Add("FormActionRoute", RouteNames.ResumeUsers);
                    ViewData.Add("FormActionText", "Resume user(s)");
                break;
                default:
                    return BadRequest();
            }
            
            return View(new UserViewModel() { AccountId = accountId });
        }
    }
}