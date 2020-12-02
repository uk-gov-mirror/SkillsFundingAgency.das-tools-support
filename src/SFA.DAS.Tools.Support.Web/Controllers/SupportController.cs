﻿using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SFA.DAS.Tools.Support.Web.Controllers
{
    public class SupportController : Controller
    {
        private readonly ILogger<SupportController> _logger;
        private readonly string _baseUrl;

        public SupportController(ILogger<SupportController> logger, IConfiguration _configuration)
        {
            _logger = logger;
            var baseUrl =_configuration.GetValue<string>("BaseUrl");
            if (!baseUrl.EndsWith('/'))
            {
                _baseUrl = string.Concat(baseUrl, '/');
            } else
            {
                _baseUrl = baseUrl;
            }
            
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult LogOut()
        {
            return SignOut(new Microsoft.AspNetCore.Authentication.AuthenticationProperties
            {
                RedirectUri = $"{_baseUrl}Support/LoggedOut"
            }, OpenIdConnectDefaults.AuthenticationScheme, CookieAuthenticationDefaults.AuthenticationScheme);
        }

        [AllowAnonymous]
        public IActionResult LoggedOut()
        {
            return View();
        }
    }
}
