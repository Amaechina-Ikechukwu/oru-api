using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using ORUApi.Models;

namespace ORUApi.Services
{
   public class TokenService(IConfiguration config)
{
    public string CreateStudentToken(Student student)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, student.Id.ToString()),
            new(ClaimTypes.Email, student.Email),
            new("matricNumber", student.MatricNumber),
            new(ClaimTypes.Role, "Student"),
        };
        return BuildToken(claims);
    }

    public string CreateAdminToken(Admin admin)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, admin.Id.ToString()),
            new(ClaimTypes.Email, admin.Email),
            new("staffId", admin.StaffId),
            new(ClaimTypes.Role, admin.Role.ToString()),
        };
        return BuildToken(claims);
    }

    private string BuildToken(List<Claim> claims)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(config["Jwt:Secret"]!));

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

}
}