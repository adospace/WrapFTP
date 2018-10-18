using System;
using System.Collections.Generic;
using System.Text;

namespace CoreFTP
{
    internal class Validate
    {
        public static void NotNull(object value, string parameterName, string field = null)
        {
            if (value == null) throw new ArgumentNullException(parameterName + (field == null ? string.Empty : "." + field));
        }

        public static void NotNullOrEmptyOrWhiteSpace(string value, string parameterName, string field = null)
        {
            if (value == null) throw new ArgumentNullException(parameterName + (field == null ? string.Empty : "." + field));
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException($"Parameter can't be an empty or whitespace string", parameterName + (field == null ? string.Empty : "." + field));
        }

        public static void Positive(int value, string parameterName, string field = null)
        {
            if (value <= 0) throw new ArgumentException($"Parameter must be greater than 0", parameterName + (field == null ? string.Empty : "." + field));
        }

        public static void PositiveOrZero(int value, string parameterName, string field = null)
        {
            if (value < 0) throw new ArgumentException($"Parameter must be greater than or equal to 0", parameterName + (field == null ? string.Empty : "." + field));
        }


    }
}
