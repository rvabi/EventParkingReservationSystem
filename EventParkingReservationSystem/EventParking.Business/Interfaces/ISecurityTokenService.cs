using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventParking.Business.Interfaces;

public interface ISecurityTokenService
{
    string GenerateToken();

    string HashToken(string token);
}