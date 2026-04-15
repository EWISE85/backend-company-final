using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model.UserModel
{
	public class UserProductModel
	{
		public Guid ProductId { get; set; }
		public string Description { get; set; }
		public DateOnly? CreateAt { get; set; }
		public string Status { get; set; }
		public string? CategoryName { get; set; }
		public string? BrandName { get; set; }
		public double FinalPoints { get; set; }
	}
}
