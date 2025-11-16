namespace Common.DTOs
{
    public class CreateClassRequestDTO
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int MaxCapacity { get; set; }
        public int Duration { get; set; }
        public string StartDate { get; set; }
    }
}