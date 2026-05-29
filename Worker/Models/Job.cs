using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Worker.Models
{
    public class Job
    {
        public string Id { get; set; } = "";
        public string Code { get; set; } = "";
        public string Language { get; set; } = "python";
        public int Priority { get; set; } = 5;
        public string Status { get; set; } = "pending";
        public string? Result { get; set; }
        public string? AssignedWorker { get; set; }
    }
}
