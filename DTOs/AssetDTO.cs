namespace IT_ASSET.DTOs
{
    public class AssetDTO
    {
    }

    public class AddAssetDto
    {
        public string user_name { get; set; } 
        public string company { get; set; }
        public string department { get; set; }
        public string employee_id { get; set; }
        public string type { get; set; }
        public string date_acquired { get; set; }
        public string asset_barcode { get; set; }
        public string brand { get; set; }
        public string model { get; set; }
        public string ram { get; set; }
        public string storage { get; set; }
        public string gpu { get; set; }
        public string size { get; set; }
        public string color { get; set; }
        public string serial_no { get; set; }
        public string po { get; set; }
        public string warranty { get; set; }
        public decimal cost { get; set; }
        public string remarks { get; set; }
        public List<string> history { get; set; }
        public string li_description { get; set; }
    }

    public class AssignOwnerDto
    {
        public int AssetId { get; set; }  
        public int OwnerId { get; set; }  
    }

    public class CreateAssetDto
    {
        public string type { get; set; }  
        public string asset_barcode { get; set; }  
        public string brand { get; set; }  
        public string model { get; set; } 
        public string ram { get; set; }  
        public string storage { get; set; }  
        public string gpu { get; set; }  
        public string size { get; set; }  
        public string color { get; set; }  
        public string serial_no { get; set; }  
        public string po { get; set; } 
        public string warranty { get; set; }  
        public decimal cost { get; set; }  
        public string remarks { get; set; }  
        public string li_description { get; set; }  
        public string date_acquired { get; set; }  
        public string asset_image { get; set; }  
    }



}
