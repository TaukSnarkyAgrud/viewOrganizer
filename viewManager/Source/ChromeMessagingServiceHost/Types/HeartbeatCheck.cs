using Newtonsoft.Json;

namespace ChromeMessagingServiceHost.Types
{
    internal class HeartbeatCheck
    {
        [JsonProperty("action")]
        public string? Action { get; set; }
    }
}