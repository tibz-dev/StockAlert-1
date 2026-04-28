using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockAlert.Application.DTOs;

public record ProductDto(
    Guid Id,
    string Name,
    decimal Price,
    int StockQuantity,
    string CategoryName);