using Newtonsoft.Json;

namespace ChromeMessagingServiceHost
{
    internal class HeartbeatCheck
    {
        [JsonProperty("action")]
        public string? Action { get; set; }
    }
}