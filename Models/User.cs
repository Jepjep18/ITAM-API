using System.Text.Json.Serialization;
using IT_ASSET.Models;

public class User
{
    public int id { get; set; }
    public string name { get; set; }
    public string company { get; set; }
    public string department { get; set; }
    public string? employee_id { get; set; }
    public string? password { get; set; }
    public string? e_signature { get; set; }
    public DateTime? date_created { get; set; }
    public bool is_active { get; set; }

    [JsonIgnore]
    public ICollection<Asset>? assets { get; set; }

    [JsonIgnore]
    public ICollection<Computer>? computer { get; set; }

    [JsonIgnore]
    public ICollection<ComputerComponents>? computer_components { get; set; }
}