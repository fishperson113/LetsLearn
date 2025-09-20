using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Claims;


namespace LetsLearn.Core.Entities
{
    public class JwtTokenVo
    {
        public Guid UserID { get; set; }
        public string Role { get; set; } = string.Empty;

        public IEnumerable<Claim> GetClaims()
        {
            return new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, UserID.ToString()),
                new Claim(ClaimTypes.Role, Role)
            };
        }
    }
}
