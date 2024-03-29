﻿using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ConsoleTestApp.ApiObjects.Lights
{
	public class LightControlCapabilities
	{
		[JsonPropertyName("mindimlevel")]
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public int? MinimumDimLevel { get; set; }
		[JsonPropertyName("maxlumen")]
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public int? MaximumLumen { get; set; }
		[JsonPropertyName("colorgamuttype")]
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public string ColorGamutType { get; set; }
		[JsonPropertyName("colorgamut")]
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public List<double[]> ColorGamut { get; set; }
		[JsonPropertyName("ct")]
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public Dictionary<string, int> ColorTemperatureRange { get; set; }
	}
}
/*
			 "mindimlevel": 200,
			 "maxlumen": 800,
			 "colorgamuttype": "C",
			 "colorgamut": [
				 [
					 0.6915,
					 0.3083
				 ],
				 [
					 0.17,
					 0.7
				 ],
				 [
					 0.1532,
					 0.0475
				 ]
			 ],
			 "ct": {
				 "min": 153,
				 "max": 500
			 }

*/
