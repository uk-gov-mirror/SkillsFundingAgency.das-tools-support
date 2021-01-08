using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SFA.DAS.Tools.Support.Core.Models;
using SFA.DAS.Tools.Support.Infrastructure.Services;
using SFA.DAS.Tools.Support.Web.Configuration;
using SFA.DAS.Tools.Support.Web.Extensions;
using SFA.DAS.Tools.Support.Web.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Support.Web.Controllers
{
    [Route("support/approvals")]
    public class PauseApprovalsController : Controller
    {
        private readonly ILogger<ApprovalsController> _logger;
        private readonly IEmployerCommitmentsService _employerCommitmentsService;
        private readonly IMapper _mapper;
        private readonly IOptions<ClaimsConfiguration> _claimConfiguration;

        public PauseApprovalsController(ILogger<ApprovalsController> logger, IEmployerCommitmentsService employerCommitmentsService, IMapper mapper, IOptions<ClaimsConfiguration> claimConfiguration)
        {
            _logger = logger;
            _employerCommitmentsService = employerCommitmentsService;
            _mapper = mapper;
            _claimConfiguration = claimConfiguration;
            _claimConfiguration.Value.ValidateConfiguration();
        }

        [HttpPost("pauseApprenticeship", Name = RouteNames.Approval_PauseApprenticeship)]
        public async Task<IActionResult> PauseApprenticeship(ApprenticeshipSearchResultsViewModel model)
        {
            var tasks = new List<Task<GetApprenticeshipResult>>();
            var ids = model.SelectedIds?.Split(',');

            if (ids == null || ids.Count() == 0)
            {
                return RedirectToAction(RouteNames.Approval_SearchApprenticeships, new
                {
                    model.ApprenticeNameOrUln,
                    model.CourseName,
                    model.ProviderName,
                    model.Ukprn,
                    model.EmployerName,
                    SelectedStatus = model.Status,
                    EndDate = model.EndDate.GetValueOrDefault().ToString("yyyy-MM-dd"),
                    StartDate = model.StartDate.GetValueOrDefault().ToString("yyyy-MM-dd"),
                    act = ActionNames.Pause
                });
            }

            foreach (var id in ids)
            {
                if (int.TryParse(id, out var longId))
                {
                    tasks.Add(_employerCommitmentsService.GetApprenticeship(longId, new CancellationToken()));
                }
            }

            var results = await Task.WhenAll(tasks);

            if (results.Any(a => a.HasError))
            {
                return View(new PauseApprenticeshipViewModel
                {
                    HasError = true
                });
            }

            return View(new PauseApprenticeshipViewModel 
            { 
                Apprenticeships = _mapper.Map<List<PauseApprenticeshipRow>>(results.Select(s => s.Apprenticeship)),
                SearchParams = new SearchParameters
                {
                    ApprenticeNameOrUln = model.ApprenticeNameOrUln,
                    CourseName = model.CourseName,
                    EmployerName = model.EmployerName,
                    ProviderName = model.ProviderName,
                    Ukprn = model.Ukprn,
                    SelectedStatus = model.Status,
                    StartDate = model.StartDate,
                    EndDate = model.EndDate
                } 
            });
        }

        [HttpPost("cancelPauseApprenticeship", Name = RouteNames.Approval_CancelPauseApprenticeship)]
        public IActionResult CancelPauseApprenticeship(PauseApprenticeshipViewModel model, string act)
        {
            return RedirectToAction(RouteNames.Approval_SearchApprenticeships, "Approvals", new
            {
                model.SearchParams.ApprenticeNameOrUln,
                model.SearchParams.CourseName,
                model.SearchParams.ProviderName,
                model.SearchParams.Ukprn,
                model.SearchParams.EmployerName,
                model.SearchParams.SelectedStatus,
                StartDate = model.SearchParams.StartDate.GetUIFormattedDate(),
                EndDate = model.SearchParams.EndDate.GetUIFormattedDate(),
                act = ActionNames.Pause
            });
        }

        [HttpPost("pauseApprenticeshipConfirmation", Name = RouteNames.Approval_PauseApprenticeshipConfirmation)]
        public async Task<IActionResult> PauseApprenticeshipConfirmation(PauseApprenticeshipViewModel model)
        {
            var userEmail = HttpContext.User.Claims.GetClaim(_claimConfiguration.Value.EmailClaim);
            var userId = HttpContext.User.Claims.GetClaim(_claimConfiguration.Value.NameIdentifierClaim);
            var displayName = HttpContext.User.Claims.GetClaim(_claimConfiguration.Value.NameClaim);
            
            displayName = "DBEAVON";
            userId = "DBEAVON";

            if(!IsValid(model, new string[] {userId, displayName}, out List<PauseApprenticeshipRow> apprenticeshipsData))
            {
                return View("PauseApprenticeship", model);
            }

            var tasks = new List<Task<PauseApprenticeshipResult>>();
            // is this where unecessary as its captured in the validation?
            foreach (var apprenticeship in apprenticeshipsData.Where(a => a.ApiSubmissionStatus != SubmissionStatus.Successful))
            {
                tasks.Add(_employerCommitmentsService.PauseApprenticeship(new PauseApprenticeshipRequest
                {
                    ApprenticeshipId = apprenticeship.Id,
                    DisplayName = displayName,
                    EmailAddress = userEmail,
                    UserId = userId
                }, new CancellationToken()));
            }

            var results = await Task.WhenAll(tasks);

            foreach (var apprenticeship in apprenticeshipsData)
            {
                var result = results.Where(s => s.ApprenticeshipId == apprenticeship.Id).FirstOrDefault();
                if (result == null)
                {
                    continue;
                }

                if (!result.HasError)
                {
                    apprenticeship.ApiSubmissionStatus = SubmissionStatus.Successful;
                    apprenticeship.ApiErrorMessage = string.Empty;
                }
                else
                {
                    apprenticeship.ApiSubmissionStatus = SubmissionStatus.Errored;
                    apprenticeship.ApiErrorMessage = result.ErrorMessage;
                }
            }

            model.Apprenticeships = apprenticeshipsData;
            
            return View("PauseApprenticeship", model);
        }

        public bool IsValid(PauseApprenticeshipViewModel model, IEnumerable<string> claims, out List<PauseApprenticeshipRow> apprenticeshipsData)
        {
            if(!model.TryDeserialise(out apprenticeshipsData, _logger))
            {
                ModelState.AddModelError(string.Empty, "Unable to Read apprenticeship information, please return to the search and try again");
                model.ApprenticeshipsData = null;

                return false;
            }
            
            if(claims.Any(c => string.IsNullOrWhiteSpace(c)))
            {
                model.Apprenticeships = apprenticeshipsData;
                ModelState.AddModelError(string.Empty, "Unable to retrieve userId or name from claim for request to Pause Apprenticeship");

                return false;
            }

            // The commitments V2 API does not let you set the Pause date currently, we'll need to verify that manually adding the date is required            
            // if(apprenticeshipsData.Any(s => s.GetPauseDate == null && s.ApiSubmissionStatus != SubmissionStatus.Successful))
            // {
            //     model.Apprenticeships = apprenticeshipsData;
            //     ModelState.AddModelError(string.Empty, "Not all Apprenticeship rows have been supplied with a pause date.");

            //     return false;
            // }

            return true;
        }
    }
}
