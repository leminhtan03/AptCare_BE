using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Dtos.BuildingDtos
{
    public sealed class AptSnap
    {
        public int Limit { get; set; }
        public int ActiveCount { get; set; }
        public bool HasOwner { get; set; }
        public HashSet<int> ActiveUsers { get; } = new();
    }
}
