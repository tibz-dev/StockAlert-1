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
    List<ProductDto> TopSellingProducts,
    int DiscrepancyCount, // Number of items where local stock != SmartTrade stock
    List<string> SyncLogs // History of last 5 successful syncs

);
