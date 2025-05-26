using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Chat
{
    public class UploadResult
    {
        public required string FileName { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
