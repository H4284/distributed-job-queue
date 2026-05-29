namespace QueueServer.Models
{
    public class WorkerInfo
    {
        public string Id { get; set; } = "";
        public string Url { get; set; } = "";
        public int ActiveJobs { get; set; } = 0;
        public DateTime LastHeartbeat { get; set; } = DateTime.UtcNow;
        public bool IsAlive => (DateTime.UtcNow - LastHeartbeat).TotalSeconds < 15;
    }
}
