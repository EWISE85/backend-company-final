using ElecWasteCollection.Application.IServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Infrastructure.BackgroundServices
{
	public class VoucherExpirationWorker : BackgroundService
	{
		private readonly IServiceProvider _serviceProvider;
		private readonly ILogger<VoucherExpirationWorker> _logger;

		public VoucherExpirationWorker(IServiceProvider serviceProvider, ILogger<VoucherExpirationWorker> logger)
		{
			_serviceProvider = serviceProvider;
			_logger = logger;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			while (!stoppingToken.IsCancellationRequested)
			{
				try
				{
					using (var scope = _serviceProvider.CreateScope())
					{
						// Gọi service xử lý voucher
						var voucherService = scope.ServiceProvider.GetRequiredService<IVoucherService>();
						await voucherService.UpdateExpiredVouchersAsync();
					}

					_logger.LogInformation("Đã quét và cập nhật các Voucher hết hạn vào lúc: {time}", DateTimeOffset.Now);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Lỗi xảy ra khi tự động cập nhật trạng thái Voucher hết hạn.");
				}

				await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
			}
		}
	}
}
