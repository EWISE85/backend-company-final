using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Helper
{
	public class ImaggaCheckResult
	{
		public bool IsMatch { get; set; }
		public string DetectedTagsJson { get; set; } // Chuỗi JSON của các tag
	}

	public class ImaggaResponse
	{
		public ImaggaResult Result { get; set; }
	}
	public class ImaggaResult
	{
		public List<ImaggaTag> Tags { get; set; }
	}
	public class ImaggaTag
	{
		[System.Text.Json.Serialization.JsonPropertyName("confidence")]
		public double Confidence { get; set; }
		[System.Text.Json.Serialization.JsonPropertyName("tag")]
		public Dictionary<string, string> Tag { get; set; }
	}
}
