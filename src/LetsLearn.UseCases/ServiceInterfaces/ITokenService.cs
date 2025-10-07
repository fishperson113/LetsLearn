using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace LetsLearn.UseCases.ServiceInterfaces
{
    public interface ITokenService
    {
        string CreateAccessToken(Guid userId, string role);
        string CreateRefreshToken(Guid userId, string role);
        ClaimsPrincipal ValidateToken(string token, bool isAccessToken);
        int GetRefreshTokenExpireSeconds();
    }
}
