using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Dtos.Account
{
    public class ImportResultDto
    {
        public int TotalRows { get; set; }
        public int SuccessfulRows { get; set; }
        public int FailedRows => Errors.Count;
        public bool IsSuccess => FailedRows == 0;
        public List<string> Errors { get; set; } = new List<string>();
    }
}
