using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventParking.Business.DTOs.Auth;

public class LoginResult
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public LoginResponse? Data { get; set; }
}