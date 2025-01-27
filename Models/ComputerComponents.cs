namespace IT_ASSET.Models
{
    public class ComputerComponents
    {
        public int id { get; set; }
        public string type { get; set; }
        public string description { get; set; }
        public string asset_barcode { get; set; }
        public string? status { get; set; }
        public List<string>? history { get; set; }
        public int? owner_id { get; set; }
        public User owner { get; set; }


    }
}
