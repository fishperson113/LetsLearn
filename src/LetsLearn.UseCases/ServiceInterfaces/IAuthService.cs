using LetsLearn.UseCases.DTOs;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsLearn.UseCases.ServiceInterfaces
{
    public interface IAuthService
    {
        Task<JwtTokenResponse> RegisterAsync(SignUpRequest request, HttpContext context);
        Task<JwtTokenResponse> LoginAsync(AuthRequest request, HttpContext context);
        Task<JwtTokenResponse> RefreshAsync(HttpContext httpContext);
        Task UpdatePasswordAsync(UpdatePassword request, Guid userId);
        Task LogoutAsync(HttpContext context, Guid userId);
    }
}
