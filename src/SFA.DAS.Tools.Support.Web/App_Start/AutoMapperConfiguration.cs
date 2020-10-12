﻿using AutoMapper;
using SFA.DAS.CommitmentsV2.Api.Types.Responses;
using SFA.DAS.Tools.Support.Core.Models;
using SFA.DAS.Tools.Support.Web.Models;

namespace SFA.DAS.Tools.Support.Web.App_Start
{
    public static class AutoMapperConfiguration
    {
        public static void ConfigureAutoMapper(this IMapperConfigurationExpression config)
        {
            config.CreateMap<ApprenticeshipDto, StopApprenticeshipViewModel>()
                .ForMember(dest => dest.ApprenticeshipId, m => m.MapFrom(u => u.Id));
            config.CreateMap<GetApprenticeshipsResponse.ApprenticeshipDetailsResponse, ApprenticeshipDto>();
            config.CreateMap<GetApprenticeshipResponse, ApprenticeshipDto>();

        }

    }
}
