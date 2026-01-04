using System.Text.Json.Serialization;

namespace Note_taking_demo.Models
{
    public class Info
    {
        public int Id { get; set; }

        public string Note { get; set; } = string.Empty;

        // Foreign key
        public int UserId { get; set; }

        // Navigation property
        [JsonIgnore]
        public User? User { get; set; }
    }
}
