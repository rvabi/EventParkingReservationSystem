using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventParking.Business.DTOs.Auth;

public class JwtTokenResult
{
    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }
}
