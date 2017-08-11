﻿using Newtonsoft.Json;

namespace HA4IoT.Extensions.Messaging.KodiMessages
{

    public class Stack
    {
        [JsonProperty(PropertyName = "message")]
        public string Message { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }
    }
    
}
