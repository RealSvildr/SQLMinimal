namespace System.Runtime.CompilerServices {
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class ExtensionAttribute : Attribute {
    }
}

public static class Int {
    public static bool Between(this int value, int firstValue, int secondValue) {
        if (firstValue > secondValue) {
            int tempValue = firstValue;
            firstValue = secondValue;
            secondValue = tempValue;
        }

        return value >= firstValue && value <= secondValue;
    }

    public static bool In(this int value, params int[] values) {
        foreach (int r in values) {
            if (value == r) {
                return true;
            }
        }
        return false;
    }
}

public static class Decimal {
    public static bool Between(this decimal value, decimal firstValue, decimal secondValue) {
        if (firstValue > secondValue) {
            decimal tempValue = firstValue;
            firstValue = secondValue;
            secondValue = tempValue;
        }

        return value >= firstValue && value <= secondValue;
    }

    public static bool In(this decimal value, params decimal[] values) {
        foreach (int r in values) {
            if (value == r) {
                return true;
            }
        }
        return false;
    }
}

public static class Double {
    public static bool Between(this double value, double firstValue, double secondValue) {
        if (firstValue > secondValue) {
            double tempValue = firstValue;
            firstValue = secondValue;
            secondValue = tempValue;
        }

        return value >= firstValue && value <= secondValue;
    }

    public static bool In(this double value, params double[] values) {
        foreach (int r in values) {
            if (value == r) {
                return true;
            }
        }
        return false;
    }
}

public static class String {
    public static bool In(this string value, params string[] values) {
        foreach (string v in values) {
            if (value == v) {
                return true;
            }
        }
        return false;
    }

    public static bool Like(this string value, string pattern) {
        if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(pattern)) {
            return false;
        }

        string[] patterns = pattern.Split('%');
        string tempPattern = patterns[0],
               worker = value;

        if (!worker.StartsWith(tempPattern)) {
            return false;
        }

        worker = worker.Substring(tempPattern.Length);

        if (patterns.Length > 1) {
            tempPattern = patterns[patterns.Length - 1];

            if (!worker.EndsWith(tempPattern)) {
                return false;
            }

            worker = worker.Substring(0, worker.Length - tempPattern.Length);

            for (int i = 1; i < patterns.Length - 1; i++) {
                tempPattern = patterns[i];

                if (worker.IndexOf(tempPattern) < 0) {
                    return false;
                }

                worker = worker.Substring(worker.IndexOf(tempPattern) + tempPattern.Length);
            }
        }

        return true;
    }
}

public static class Object {
    public static object Convert(this object value, System.Type type) {
        if (type.IsGenericType) {
            return System.Convert.ChangeType(value, type.GetGenericArguments()[0]);
        }

        switch (type.Name) {
            case "String": return value.ToString();
            case "Boolean": return System.Convert.ToBoolean(value);
            case "Int": case "Int32": return System.Convert.ToInt32(value);
            case "DateTime": return System.Convert.ToDateTime(value);
        }

        return value;
    }
}
