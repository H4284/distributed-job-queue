namespace QueueServer.Models
{
    public class Job
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Code { get; set; } = "";
        public string Language { get; set; } = "python";
        public int Priority { get; set; } = 5;
        public string Status { get; set; } = "pending";
        public string? Result { get; set; }
        public string? AssignedWorker { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
