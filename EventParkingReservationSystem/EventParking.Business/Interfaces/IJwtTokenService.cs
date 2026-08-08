using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventParking.Business.DTOs.Auth;
using EventParking.Models.Entities;


namespace EventParking.Business.Interfaces;

public interface IJwtTokenService
{
    JwtTokenResult GenerateToken(Customer customer);
}
