using System.Text.Json.Serialization;

namespace Note_taking_demo.Models;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";

    // Navigation collection
    [JsonIgnore]
    public ICollection<Info> Infos { get; set; } = new List<Info>();
}
