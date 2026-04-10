using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model
{
	public class PostImageModel
	{
		public Guid PostId { get; set; }


		public string ImageUrl { get; set; } = string.Empty;
		public List<LabelModel> Labels { get; set; }

	}
}
