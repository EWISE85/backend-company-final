using ElecWasteCollection.Domain.Entities;
using ElecWasteCollection.Domain.IRepository;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Infrastructure.Repository
{
	public class UserVoucherRepository : GenericRepository<UserVoucher>, IUserVoucherRepository
	{
		public UserVoucherRepository(DbContext context) : base(context)
		{
		}
		public async Task<(List<(UserVoucher UV, double PointsUsed)> Items, int TotalCount)> GetUserVouchersPaginatedAsync(Guid userId, int page, int limit)
		{
			var query = _dbSet.AsNoTracking()
				.Include(uv => uv.Voucher)
				.Where(uv => uv.UserId == userId);

			var totalCount = await query.CountAsync();

			var itemsWithPoints = await query
				.OrderByDescending(uv => uv.ReceivedAt)
				.Skip((page - 1) * limit)
				.Take(limit)
				.Select(uv => new
				{
					UserVoucher = uv,
					PointsUsed = _context.Set<PointTransactions>()
						.Where(pt => pt.UserId == uv.UserId
								  && pt.VoucherId == uv.VoucherId
								  && pt.TransactionType == PointTransactionType.DOI_DIEM.ToString())
						.Select(pt => Math.Abs(pt.Point))
						.FirstOrDefault()
				})
				.ToListAsync();

			var resultItems = itemsWithPoints.Select(x => (x.UserVoucher, x.PointsUsed)).ToList();

			return (resultItems, totalCount);
		}
	}
}
