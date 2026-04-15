using ElecWasteCollection.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Domain.IRepository
{
	public interface IUserVoucherRepository : IGenericRepository<UserVoucher>
	{
		Task<(List<(UserVoucher UV, double PointsUsed)> Items, int TotalCount)> GetUserVouchersPaginatedAsync(Guid userId, int page, int limit);

	}
}
