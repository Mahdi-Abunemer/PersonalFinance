using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PersonalFinanceCli.Application.Validation
{
    public static class TransactionValidator
    {
        public static void ValidateAmount(decimal amount)
        {
            if (amount <= 0)
            {
                throw new InvalidOperationException("Amount must be > 0.");
            }
        }

        public static void ValidateCategory(string category)
        {
            if (string.IsNullOrWhiteSpace(category))
            {
                throw new InvalidOperationException("Category cannot be empty.");
            }
        }
    }
}
