using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.User;

public class UserUpdateDto
{
    public string? NewUsername { get; set; }
    public string? NewPassword { get; set; }
}

