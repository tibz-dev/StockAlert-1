using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockAlert.Application.DTOs;
public record DashboardDto(
    int TotalProducts,
    decimal TotalInventoryValue,
    int LowStockAlerts,
    decimal TotalSalesRevenue,
    List<ProductDto> TopSellingProducts
);
