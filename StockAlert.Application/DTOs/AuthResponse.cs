using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockAlert.Application.DTOs;
public record AuthResponse(bool Success, string Token, string Message);