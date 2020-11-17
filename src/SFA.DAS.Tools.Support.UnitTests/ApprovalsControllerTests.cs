﻿using AutoFixture;
using AutoFixture.Xunit2;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SFA.DAS.Tools.Support.Core.Models;
using SFA.DAS.Tools.Support.Infrastructure.Services;
using SFA.DAS.Tools.Support.UnitTests.AutoFixture;
using SFA.DAS.Tools.Support.Web.Configuration;
using SFA.DAS.Tools.Support.Web.Controllers;
using SFA.DAS.Tools.Support.Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using static SFA.DAS.Tools.Support.Web.Models.StopApprenticeshipRow;

namespace SFA.DAS.Tools.Support.UnitTests
{
    public class ApprovalsControllerTests
    {
        [Theory, AutoMoqData]
        public void SearchApprenticeships_GET_WithNoParameters_ReturnsView(ApprovalsController sut)
        {
            //When
            var result = sut.SearchApprenticeships(null, null, null, null, null, null, null, null);

            //Then
            result.Should().BeOfType<ViewResult>().
                Which.Model.Should().BeOfType<SearchApprenticeshipsViewModel>().
                Which.Should().BeEquivalentTo(new SearchApprenticeshipsViewModel());
        }

        [Theory, AutoMoqData]
        public async Task StopApprenticeship_POST_NoRowsSelected_RedirectsToSearch(ApprovalsController sut, ApprenticeshipSearchResultsViewModel model)
        {
            //Given
            model.SelectedIds = null;

            //When
            var result = await sut.StopApprenticeship(model);

            //Then
            var action = result.Should().BeOfType<RedirectToActionResult>().Which;
            action.ActionName.Should().Be(RouteNames.Approval_SearchApprenticeships);
            action.RouteValues.Values.Should().BeEquivalentTo(new object []
            {
                    model.ApprenticeNameOrUln,
                    model.CourseName,
                    model.ProviderName,
                    model.Ukprn,
                    model.EmployerName,
                    model.Status,
                    model.EndDate.Value.ToString("yyyy-MM-dd"),
                    model.StartDate.Value.ToString("yyyy-MM-dd"),
                });
        }

        [Theory, AutoMoqData]
        public async Task StopApprenticeship_POST_ApiErrorOccurs_ReturnsErrorViewModel([Frozen] Mock<IEmployerCommitmentsService> api, ApprovalsController sut, ApprenticeshipSearchResultsViewModel model)
        {
            //Given
            var fixture = new Fixture();
            var apprenticeResults = fixture.CreateMany<GetApprenticeshipResult>(3).ToList();
            var ids = apprenticeResults.Select(s => s.Apprenticeship.Id);
            apprenticeResults.ForEach(a => a.ErrorMessage = string.Empty);
            apprenticeResults.Last().ErrorMessage = "Api ErrorMessage";
            model.SelectedIds = ids.Aggregate<long, string>("", (current, value) => current + "," + value.ToString());
            api.Setup(a => a.GetApprenticeship(It.Is<long>(a => ids.Contains(a)), It.IsAny<CancellationToken>())).Returns((long id, CancellationToken token) =>
            Task.FromResult(apprenticeResults.Where(s => s.Apprenticeship.Id == id).FirstOrDefault()));

            //When
            var result = await sut.StopApprenticeship(model);

            //Then
            result.Should().BeOfType<ViewResult>().Which
                .Model.Should().BeOfType<StopApprenticeshipViewModel>().Which
                .HasError.Should().BeTrue();
        }

        [Theory, AutoMoqData]
        public async Task StopApprenticeship_POST_ApiReturnsResults_ReturnsViewModel([Frozen] Mock<IEmployerCommitmentsService> api, ApprovalsController sut, ApprenticeshipSearchResultsViewModel model)
        {
            //Given
            var fixture = new Fixture();
            var apprenticeResults = fixture.CreateMany<GetApprenticeshipResult>(3).ToList();
            var ids = apprenticeResults.Select(s => s.Apprenticeship.Id);
            apprenticeResults.ForEach(a => a.ErrorMessage = string.Empty);
            model.SelectedIds = ids.Aggregate<long, string>("", (current, value) => current + "," + value.ToString());
            api.Setup(a => a.GetApprenticeship(It.Is<long>(a => ids.Contains(a)), It.IsAny<CancellationToken>()))
                .Returns((long id, CancellationToken token) => Task.FromResult(apprenticeResults.Where(s => s.Apprenticeship.Id == id).FirstOrDefault()));

            //When
            var result = await sut.StopApprenticeship(model);

            //Then
            var resultModel = result.Should().BeOfType<ViewResult>().Which
                .Model.Should().BeOfType<StopApprenticeshipViewModel>().Which;

            resultModel.SearchParams.Should().BeEquivalentTo(new
            {
                model.ApprenticeNameOrUln,
                model.CourseName,
                model.EmployerName,
                model.ProviderName,
                model.Ukprn,
                SelectedStatus = model.Status,
                model.StartDate,
                model.EndDate
            });

            resultModel.Apprenticeships.Should().BeEquivalentTo(new object[]
            {
                new {
                    apprenticeResults[0].Apprenticeship.Id,
                    AccountId = apprenticeResults[0].Apprenticeship.EmployerAccountId,
                    apprenticeResults[0].Apprenticeship.FirstName,
                    apprenticeResults[0].Apprenticeship.LastName,
                    ULN = apprenticeResults[0].Apprenticeship.Uln,
                    apprenticeResults[0].Apprenticeship.EmployerName,
                    apprenticeResults[0].Apprenticeship.ProviderName,
                    apprenticeResults[0].Apprenticeship.Ukprn,
                    apprenticeResults[0].Apprenticeship.CourseName,
                    Status = apprenticeResults[0].Apprenticeship.ApprenticeshipStatus.ToString(),
                    PaymentStatus = apprenticeResults[0].Apprenticeship.PaymentStatus.ToString(),
                    apprenticeResults[0].Apprenticeship.StartDate,
                    apprenticeResults[0].Apprenticeship.EndDate,
                    ApiSubmissionStatus = SubmissionStatus.NotSent
                },
                new {
                    apprenticeResults[1].Apprenticeship.Id,
                    AccountId = apprenticeResults[1].Apprenticeship.EmployerAccountId,
                    apprenticeResults[1].Apprenticeship.FirstName,
                    apprenticeResults[1].Apprenticeship.LastName,
                    ULN = apprenticeResults[1].Apprenticeship.Uln,
                    apprenticeResults[1].Apprenticeship.EmployerName,
                    apprenticeResults[1].Apprenticeship.ProviderName,
                    apprenticeResults[1].Apprenticeship.Ukprn,
                    apprenticeResults[1].Apprenticeship.CourseName,
                    Status = apprenticeResults[1].Apprenticeship.ApprenticeshipStatus.ToString(),
                    PaymentStatus = apprenticeResults[1].Apprenticeship.PaymentStatus.ToString(),
                    apprenticeResults[1].Apprenticeship.StartDate,
                    apprenticeResults[1].Apprenticeship.EndDate,
                    ApiSubmissionStatus = SubmissionStatus.NotSent
                },
                new {
                    apprenticeResults[2].Apprenticeship.Id,
                    AccountId = apprenticeResults[2].Apprenticeship.EmployerAccountId,
                    apprenticeResults[2].Apprenticeship.FirstName,
                    apprenticeResults[2].Apprenticeship.LastName,
                    ULN = apprenticeResults[2].Apprenticeship.Uln,
                    apprenticeResults[2].Apprenticeship.EmployerName,
                    apprenticeResults[2].Apprenticeship.ProviderName,
                    apprenticeResults[2].Apprenticeship.Ukprn,
                    apprenticeResults[2].Apprenticeship.CourseName,
                    Status = apprenticeResults[2].Apprenticeship.ApprenticeshipStatus.ToString(),
                    PaymentStatus = apprenticeResults[2].Apprenticeship.PaymentStatus.ToString(),
                    apprenticeResults[2].Apprenticeship.StartDate,
                    apprenticeResults[2].Apprenticeship.EndDate,
                    ApiSubmissionStatus = SubmissionStatus.NotSent
                }
            });
        }


        [Theory, AutoMoqData]
        public void CancelStopApprenticeship_POST_RedirectsToSearch(ApprovalsController sut, StopApprenticeshipViewModel model)
        {
            //Given

            //When
            var result = sut.CancelStopApprenticeship(model);

            //Then
            var action = result.Should().BeOfType<RedirectToActionResult>().Which;
            action.ActionName.Should().Be(RouteNames.Approval_SearchApprenticeships);
            action.RouteValues.Values.Should().BeEquivalentTo(new object []
            {
                    model.SearchParams.ApprenticeNameOrUln,
                    model.SearchParams.CourseName,
                    model.SearchParams.ProviderName,
                    model.SearchParams.Ukprn,
                    model.SearchParams.EmployerName,
                    model.SearchParams.SelectedStatus,
                    model.SearchParams.EndDate.Value.ToString("yyyy-MM-dd"),
                    model.SearchParams.StartDate.Value.ToString("yyyy-MM-dd"),
                });
        }

        [Theory, AutoMoqData]
        public async Task StopApprenticeshipConfirmation_POST_JsonDataError_ReturnsErrorViewModel(ApprovalsController sut, StopApprenticeshipViewModel model)
        {
            //Given
            model.ApprenticeshipsData = "RandomData";

            //When
            var result = await sut.StopApprenticeshipConfirmation(model);

            //Then
            var resultModel = result.Should().BeOfType<ViewResult>().Which
                .Model.Should().BeOfType<StopApprenticeshipViewModel>().Which;
            resultModel.ApprenticeshipsData.Should().BeSameAs(null);
            sut.ModelState.IsValid.Should().BeFalse();
            sut.ModelState.Values.First().Errors.First().ErrorMessage.Should().Be("Unable to Read apprenticeship information, please return to the search and try again");
        }

        [Theory, AutoMoqData]
        public async Task StopApprenticeshipConfirmation_POST_IdentityError_ReturnsErrorViewModel(ApprovalsController sut, StopApprenticeshipViewModel model, List<StopApprenticeshipRow> apprenticeshipData)
        {
            //Given
            var jsonData = JsonSerializer.Serialize(apprenticeshipData, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }).ToString();
            model.ApprenticeshipsData = jsonData;
            sut.ControllerContext.HttpContext = new DefaultHttpContext();

            //When
            var result = await sut.StopApprenticeshipConfirmation(model);

            //Then
            var resultModel = result.Should().BeOfType<ViewResult>().Which
                .Model.Should().BeOfType<StopApprenticeshipViewModel>().Which;
            resultModel.ApprenticeshipsData.Should().BeSameAs(model.ApprenticeshipsData);
            sut.ModelState.IsValid.Should().BeFalse();
            sut.ModelState.Values.First().Errors.First().ErrorMessage.Should().Be("Unable to retrieve userId or name from claim for request to Stop Apprenticeship");
        }

        [Theory, AutoMoqData]
        public async Task StopApprenticeshipConfirmation_POST_NotAllStopDatesEntered_ReturnsErrorViewModel(ApprovalsController sut, StopApprenticeshipViewModel model, List<StopApprenticeshipRow> apprenticeshipData)
        {
            //Given
            var jsonData = JsonSerializer.Serialize(apprenticeshipData, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }).ToString();
            model.ApprenticeshipsData = jsonData;

            //When
            var result = await sut.StopApprenticeshipConfirmation(model);

            //Then
            var resultModel = result.Should().BeOfType<ViewResult>().Which
                .Model.Should().BeOfType<StopApprenticeshipViewModel>().Which;
            resultModel.ApprenticeshipsData.Should().BeSameAs(model.ApprenticeshipsData);
            sut.ModelState.IsValid.Should().BeFalse();
            sut.ModelState.Values.First().Errors.First().ErrorMessage.Should().Be("Not all Apprenticeship rows have been supplied with a stop date.");
        }

        [Theory, AutoMoqData]
        public async Task StopApprenticeshipConfirmation_POST_DataEnteredCorrectly_SubmitsStopToApiAndFails([Frozen] Mock<IEmployerCommitmentsService> api, ApprovalsController sut, StopApprenticeshipViewModel model, List<StopApprenticeshipRow> apprenticeshipData)
        {
            //Given
            apprenticeshipData.ForEach(s => s.EnteredStopDate = DateTime.Today.ToString("yyyy-MM-dd"));
            var apprenticeshipIds = apprenticeshipData.Select(s => s.Id);
            var jsonData = JsonSerializer.Serialize(apprenticeshipData, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }).ToString();
            model.ApprenticeshipsData = jsonData;

            api.Setup(s => s.StopApprenticeship(
                It.Is<StopApprenticeshipRequest>(r => apprenticeshipIds.Contains(r.ApprenticeshipId)), It.IsAny<CancellationToken>()))
                .Returns((StopApprenticeshipRequest request, CancellationToken token) =>
                {
                    return Task.FromResult(new StopApprenticeshipResult
                    {
                        ApprenticeshipId = request.ApprenticeshipId,
                        ErrorMessage = $"Errored For {request.ApprenticeshipId}"
                    }); ;
                });

            //When
            var result = await sut.StopApprenticeshipConfirmation(model);

            //Then

            var resultModel = result.Should().BeOfType<ViewResult>().Which
                .Model.Should().BeOfType<StopApprenticeshipViewModel>().Which;
            resultModel.Apprenticeships.All(s => s.ApiSubmissionStatus == SubmissionStatus.Errored && s.ApiErrorMessage.Equals($"Errored For {s.Id}"));
            resultModel.HasError.Should().BeFalse();
        }

        [Theory, AutoMoqData]
        public async Task StopApprenticeshipConfirmation_POST_DataEnteredCorrectly_SubmitsStopToApiAndSucceeds([Frozen] Mock<IEmployerCommitmentsService> api, ApprovalsController sut, StopApprenticeshipViewModel model, List<StopApprenticeshipRow> apprenticeshipData)
        {
            //Given
            apprenticeshipData.ForEach(s => s.EnteredStopDate = DateTime.Today.ToString("yyyy-MM-dd"));
            var apprenticeshipIds = apprenticeshipData.Select(s => s.Id);
            var jsonData = JsonSerializer.Serialize(apprenticeshipData, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }).ToString();
            model.ApprenticeshipsData = jsonData;

            api.Setup(s => s.StopApprenticeship(
                It.Is<StopApprenticeshipRequest>(r => apprenticeshipIds.Contains(r.ApprenticeshipId)), It.IsAny<CancellationToken>()))
                .Returns((StopApprenticeshipRequest request, CancellationToken token) =>
                {
                    return Task.FromResult(new StopApprenticeshipResult
                    {
                        ApprenticeshipId = request.ApprenticeshipId
                    });
                });

            //When
            var result = await sut.StopApprenticeshipConfirmation(model);

            //Then

            var resultModel = result.Should().BeOfType<ViewResult>().Which
                .Model.Should().BeOfType<StopApprenticeshipViewModel>().Which;
            resultModel.Apprenticeships.All(s => s.ApiSubmissionStatus == SubmissionStatus.Successful);
            resultModel.HasError.Should().BeFalse();
        }
    }
}