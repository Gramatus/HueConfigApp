using System.Text.Json.Serialization;

namespace ConsoleTestApp.ApiObjects.Lights
{
	public class LightStreamingCapabilities
	{
		[JsonPropertyName("renderer")]
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public bool? IsRenderer { get; set; }
		[JsonPropertyName("proxy")]
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public bool? IsProxy { get; set; }
	}
}