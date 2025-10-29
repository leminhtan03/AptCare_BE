using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Helpers
{
    public static class GenerateRoomNumber
    {
        public static string GenerateRoomNumberHelper(string building, int floor, int room)
        {
            return $"{building.ToUpper()}-{floor:D2}{room:D2}";
        }
    }
}
