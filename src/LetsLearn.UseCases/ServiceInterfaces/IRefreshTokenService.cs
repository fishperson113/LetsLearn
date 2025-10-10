﻿using LetsLearn.UseCases.DTOs;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsLearn.UseCases.ServiceInterfaces
{
    public interface IRefreshTokenService
    {
        Task<string> CreateAndStoreRefreshTokenAsync(Guid userId, string role);
        Task<JwtTokenResponse> RefreshTokenAsync(HttpContext context);
    }
}
