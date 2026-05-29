using QueueServer.Models;

namespace QueueServer.Services
{
    public class JobQueueService
    {
   
        private readonly PriorityQueue<Job, int> _queue =
            new(Comparer<int>.Create((a, b) => b - a));

        private readonly List<WorkerInfo> _workers = new();
        private readonly List<Job> _allJobs = new();
        private readonly object _lock = new();

  

        public void Enqueue(Job job)
        {
            lock (_lock)
            {
                
                if (_allJobs.Any(j => j.Id == job.Id))
                {
                    _queue.Enqueue(job, job.Priority);
                    return;
                }
                _allJobs.Add(job);
                _queue.Enqueue(job, job.Priority);
            }
        }

        public Job? Dequeue()
        {
            lock (_lock)
            {
                return _queue.TryDequeue(out var job, out _) ? job : null;
            }
        }

        public List<Job> GetAllJobs()
        {
            lock (_lock) { return _allJobs.ToList(); }
        }

        public Job? GetJobById(string id)
        {
            lock (_lock) { return _allJobs.FirstOrDefault(j => j.Id == id); }
        }

        public int PendingCount
        {
            get { lock (_lock) { return _queue.Count; } }
        }

        public bool HasPending()
        {
            lock (_lock) { return _queue.Count > 0; }
        }

   

        public void RegisterWorker(WorkerInfo worker)
        {
            lock (_lock)
            {
                var existing = _workers.FirstOrDefault(w => w.Id == worker.Id);
                if (existing == null)
                    _workers.Add(worker);
                else
                    existing.Url = worker.Url; 
            }
        }

        public void UpdateHeartbeat(string workerId)
        {
            lock (_lock)
            {
                var worker = _workers.FirstOrDefault(w => w.Id == workerId);
                if (worker != null)
                    worker.LastHeartbeat = DateTime.UtcNow;
            }
        }

      
        public WorkerInfo? GetLeastLoadedWorker()
        {
            lock (_lock)
            {
                return _workers
                    .Where(w => w.IsAlive)
                    .OrderBy(w => w.ActiveJobs)
                    .FirstOrDefault();
            }
        }

        public List<WorkerInfo> GetWorkers()
        {
            lock (_lock) { return _workers.ToList(); }
        }

        public List<WorkerInfo> GetDeadWorkers()
        {
            lock (_lock) { return _workers.Where(w => !w.IsAlive).ToList(); }
        }

        public void RemoveWorker(string workerId)
        {
            lock (_lock) { _workers.RemoveAll(w => w.Id == workerId); }
        }
    }
}
