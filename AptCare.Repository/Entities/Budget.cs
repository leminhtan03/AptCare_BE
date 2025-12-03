using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Repository.Entities
{
    public class Budget
    {
        [Key]
        public int BudgetId { get; set; }
        public decimal Amount { get; set; }
    }
}
