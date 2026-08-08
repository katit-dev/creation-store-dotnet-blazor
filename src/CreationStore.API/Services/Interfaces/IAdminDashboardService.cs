using CreationStore.API.DTOs.Admin.Dashboard;
using CreationStore.API.DTOs.ResponseTypes;

namespace CreationStore.API.Services.Interfaces
{
    public interface IAdminDashboardService
    {
        Task<ResponseTypeDTO<AdminDashboardSummaryDTO>>
            GetSummaryAsync();

        Task<ResponseTypeDTO<AdminRevenueStatisticDTO>>
            GetRevenueAsync(DateTime? fromDate, DateTime? toDate);

        Task<ResponseTypeDTO<List<AdminTopProductDTO>>>
            GetTopProductsAsync(int take);

        Task<ResponseTypeDTO<List<AdminRecentOrderDTO>>>
            GetRecentOrdersAsync(int take);
    }
}