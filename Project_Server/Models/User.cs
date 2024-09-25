using System;
using System.Collections.Generic;

namespace Project_Server.Models;

public partial class User
{
    public int Id { get; set; }

    public string Username { get; set; } = null!;

    public string UserPass { get; set; } = null!;

    public string Email { get; set; } = null!;
}
