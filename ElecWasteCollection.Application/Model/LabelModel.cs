using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model
{
	public class LabelModel
	{
		public string Tag { get; set; }
		public double Confidence { get; set; }
		public string Status { get; set; }
	}
}
