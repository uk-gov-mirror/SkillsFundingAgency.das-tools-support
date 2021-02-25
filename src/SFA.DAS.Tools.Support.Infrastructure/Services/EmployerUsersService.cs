﻿using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SFA.DAS.Tools.Support.Core.Models;
using AutoMapper;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using SFA.DAS.EmployerUsers.Api.Client;
using SFA.DAS.EmployerUsers.Api.Types;

namespace SFA.DAS.Tools.Support.Infrastructure.Services
{
    public class EmployerUsersService : IEmployerUsersService
    {
        private readonly IEmployerUsersApiClient _employerUsersApi;
        private readonly ILogger _logger;
        private readonly IMapper _mapper;

        public EmployerUsersService(IEmployerUsersApiClient employerUsersApi, IMapper mapper, ILogger<EmployerUsersService> logger)
        {
            _employerUsersApi = employerUsersApi;
            _logger = logger;
            _mapper = mapper;
        }

        public async Task<SuspendUserResult> SuspendUser(Core.Models.SuspendUserRequest request, CancellationToken token)
        {
            var result = await _employerUsersApi.SuspendUser(request.UserId);

            //TODO: Add error handling
            return new SuspendUserResult
            {
                UserId = result.Id,
                ErrorMessage = result.Errors != null && result.Errors.Any(e => !string.IsNullOrEmpty(e.Value)) ? string.Concat(", ", result.Errors.Select(e => $"{e.Key}: {e.Value}")) : null
            };
        }

        public async Task<ResumeUserResult> ResumeUser(Core.Models.ResumeUserRequest request, CancellationToken token)
        {
            var result = await _employerUsersApi.ResumeUser(request.UserId);

            //TODO: Add error handling
            return new ResumeUserResult
            {
                UserId = result.Id,
                ErrorMessage = result.Errors != null && result.Errors.Any(e => !string.IsNullOrEmpty(e.Value)) ? string.Concat(", ", result.Errors.Select(e => $"{e.Key}: {e.Value}")) : null
            };
        }
    }
}