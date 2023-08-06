﻿using Newtonsoft.Json;

namespace ChromeMessagingServiceHost
{
    internal class GenericChromeMessage
    {
        [JsonProperty("action")]
        public string? Action { get; set; }
        [JsonProperty("data")]
        public string? Data { get; set; }
    }
}