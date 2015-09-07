using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace CommonDataModels
{
    public class Tweet
    {
        [Key]
        public int PK { get; set; }

        [JsonProperty("id")]
        public long Id { get; set; }

        public string UserName { get; set; }
        
        public string UserId { get; set; }

        [JsonProperty("text")]
        public string Message { get; set; }

        [JsonProperty("created_at")]
        public string CreatedAt { get; set; }
    }
}
