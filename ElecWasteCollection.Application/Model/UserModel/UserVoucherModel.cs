using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model.UserModel
{
	public class UserVoucherModel
	{
		public Guid UserVoucherId { get; set; }
		public Guid VoucherId { get; set; }
		public string Code { get; set; }
		public string Name { get; set; }
		public string? ImageUrl { get; set; }
		public DateTime ReceivedAt { get; set; }
		public double PointsToRedeem { get; set; }

	}
}
