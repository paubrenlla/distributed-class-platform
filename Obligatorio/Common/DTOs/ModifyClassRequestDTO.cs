namespace Common.DTOs
{
    public class ModifyClassRequestDTO
    {
        public int ClassId { get; set; }
        public string NewName { get; set; }
        public string NewDescription { get; set; }
        public string NewCapacity { get; set; }
        public string NewDuration { get; set; }
        public string NewDate { get; set; }
    }
}