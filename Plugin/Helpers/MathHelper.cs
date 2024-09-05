using System.Globalization;
using System.Text.RegularExpressions;

namespace FileFlows.Plugin.Helpers;

/// <summary>
/// Helper for math operations
/// </summary>
/// <param name="_logger">the logger to use</param>
public class MathHelper(ILogger _logger)
{
    
    /// <summary>
    /// Checks if the comparison string represents a mathematical operation.
    /// </summary>
    /// <param name="comparison">The comparison string to check.</param>
    /// <returns>True if the comparison is a mathematical operation, otherwise false.</returns>
    public bool IsMathOperation(string comparison)
    {
        if (Regex.IsMatch(comparison, @"^\d+(\.\d+)?><\d+(\.\d+)?$"))
            return true;
        if (Regex.IsMatch(comparison, @"^\d+(\.\d+)?<>\d+(\.\d+)?$"))
            return true;
        
        // Check if the comparison string starts with <=, <, >, >=, ==, or =
        return new[] { "<=", "<", ">", ">=", "==", "=" }.Any(comparison.StartsWith);
    }
    
    /// <summary>
    /// Tests if a math operation is true
    /// </summary>
    /// <param name="operation">The operation string representing the mathematical operation.</param>
    /// <param name="value">The value to apply the operation to.</param>
    /// <returns>True if the mathematical operation is successful, otherwise false.</returns>
    public bool IsTrue(string operation, string value)
        => IsTrue(operation, Convert.ToDouble(value));
    
    /// <summary>
    /// Tests if a math operation is false
    /// </summary>
    /// <param name="operation">The operation string representing the mathematical operation.</param>
    /// <param name="value">The value to apply the operation to.</param>
    /// <returns>True if the mathematical operation is not successful, otherwise false.</returns>
    public bool IsFalse(string operation, string value)
        => IsTrue(operation, Convert.ToDouble(value)) == false;

    /// <summary>
    /// Tests if a math operation is false
    /// </summary>
    /// <param name="operation">The operation string representing the mathematical operation.</param>
    /// <param name="value">The value to apply the operation to.</param>
    /// <returns>True if the mathematical operation is not successful, otherwise false.</returns>
    public bool IsFalse(string operation, double value)
        => IsTrue(operation, value) == false;

    /// <summary>
    /// Tests if a math operation is true
    /// </summary>
    /// <param name="operation">The operation string representing the mathematical operation.</param>
    /// <param name="value">The value to apply the operation to.</param>
    /// <returns>True if the mathematical operation is successful, otherwise false.</returns>
    public bool IsTrue(string operation, double value)
    {
        if (Regex.IsMatch(operation, @"^\d+(\.\d+)?><\d+(\.\d+)?$"))
        {
            // between
            var values = operation.Split(["><"], StringSplitOptions.None);
            var low = double.Parse(values[0]);
            var high = double.Parse(values[0]);
            bool between = value >= low && value <= high;
            _logger.ILog($"Between: {value} is{(between ? "" : " NOT")} between {low} and {high}");
            return between;
        }

        if (Regex.IsMatch(operation, @"^\d+(\.\d+)?<>\d+(\.\d+)?$"))
        {
            // not between
            var values = operation.Split(["<>"], StringSplitOptions.None);
            var low = double.Parse(values[0]);
            var high = double.Parse(values[0]);
            bool notBetween = value < low || value > high;
            _logger.ILog($"NotBetween: {value} is{(notBetween ? " NOT" : "")} between {low} and {high}");
            return notBetween;
        }
        
        const double tolerance = 0.05;

        if (operation.Length > 2)
        {
            switch (operation[..2])
            {
                case "<=":
                {
                    var comparison = Convert.ToDouble(AdjustComparisonValue(operation[2..].Trim()));
                    var result = value <= comparison + tolerance;
                    _logger.ILog(
                        $"LessOrEqual: {value} is{(result ? "" : " NOT")} less or equal to {comparison} with tolerance {tolerance}");
                    return result;
                }
                case ">=":
                {
                    var comparison = Convert.ToDouble(AdjustComparisonValue(operation[2..].Trim()));
                    var result = value >= comparison - tolerance;
                    _logger.ILog(
                        $"GreaterOrEqual: {value} is{(result ? "" : " NOT")} greater or equal to {comparison} with tolerance {tolerance}");
                    return result;
                }
                case "==":
                {
                    var comparison = Convert.ToDouble(AdjustComparisonValue(operation[2..].Trim()));
                    var result = Math.Abs(value - comparison) < tolerance;
                    _logger.ILog(
                        $"Equal: {value} is{(result ? "" : " NOT")} equal to {comparison} with tolerance {tolerance}");
                    return result;
                }
                case "!=":
                {
                    var comparison = Convert.ToDouble(AdjustComparisonValue(operation[2..].Trim()));
                    var result = Math.Abs(value - comparison) > tolerance;
                    _logger.ILog(
                        $"NotEqual: {value} is{(result ? " NOT" : "")} equal to {comparison} with tolerance {tolerance}");
                    return result;
                }
            }
        }

        if (operation.Length > 1)
        {
            switch (operation[..1])
            {
                case "<":
                {
                    var comparison = Convert.ToDouble(AdjustComparisonValue(operation[1..].Trim()));
                    var result = value < comparison + tolerance;
                    _logger.ILog(
                        $"LessThan: {value} is{(result ? "" : " NOT")} less than {comparison} with tolerance {tolerance}");
                    return result;
                }
                case ">":
                {
                    var comparison = Convert.ToDouble(AdjustComparisonValue(operation[1..].Trim()));
                    var result = value > comparison - tolerance;
                    _logger.ILog(
                        $"GreaterThan: {value} is{(result ? "" : " NOT")} greater than {comparison} with tolerance {tolerance}");
                    return result;
                }
                case "=":
                {
                    var comparison = Convert.ToDouble(AdjustComparisonValue(operation[1..].Trim()));
                    var result = Math.Abs(value - comparison) < tolerance;
                    _logger.ILog(
                        $"Equal: {value} is{(result ? "" : " NOT")} equal to {comparison} with tolerance {tolerance}");
                    return result;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Adjusts the comparison string by handling common mistakes in units and converting them into full numbers.
    /// </summary>
    /// <param name="comparisonValue">The original comparison string to be adjusted.</param>
    /// <returns>The adjusted comparison string with corrected units or the original comparison if no adjustments are made.</returns>
    private string AdjustComparisonValue(string comparisonValue)
    {
        if (string.IsNullOrWhiteSpace(comparisonValue))
            return string.Empty;
        
        string adjustedComparison = comparisonValue.ToLower().Trim();

        // Handle common mistakes in units
        if (adjustedComparison.EndsWith("mbps"))
        {
            // Make an educated guess for Mbps to kbps conversion
            return adjustedComparison[..^4] switch
            {
                { } value when double.TryParse(value, out var numericValue) => (numericValue * 1_000_000)
                    .ToString(CultureInfo.InvariantCulture),
                _ => comparisonValue
            };
        }
        if (adjustedComparison.EndsWith("kbps"))
        {
            // Make an educated guess for kbps to bps conversion
            return adjustedComparison[..^4] switch
            {
                { } value when double.TryParse(value, out var numericValue) => (numericValue * 1_000)
                    .ToString(CultureInfo.InvariantCulture),
                _ => comparisonValue
            };
        }
        if (adjustedComparison.EndsWith("kb"))
        {
            return adjustedComparison[..^2] switch
            {
                { } value when double.TryParse(value, out var numericValue) => (numericValue * 1_000 )
                    .ToString(CultureInfo.InvariantCulture),
                _ => comparisonValue
            };
        }
        if (adjustedComparison.EndsWith("mb"))
        {
            return adjustedComparison[..^2] switch
            {
                { } value when double.TryParse(value, out var numericValue) => (numericValue * 1_000_000 )
                    .ToString(CultureInfo.InvariantCulture),
                _ => comparisonValue
            };
        }
        if (adjustedComparison.EndsWith("gb"))
        {
            return adjustedComparison[..^2] switch
            {
                { } value when double.TryParse(value, out var numericValue) => (numericValue * 1_000_000_000 )
                    .ToString(CultureInfo.InvariantCulture),
                _ => comparisonValue
            };
        }
        if (adjustedComparison.EndsWith("tb"))
        {
            return adjustedComparison[..^2] switch
            {
                { } value when double.TryParse(value, out var numericValue) => (numericValue * 1_000_000_000_000)
                    .ToString(CultureInfo.InvariantCulture),
                _ => comparisonValue
            };
        }

        if (adjustedComparison.EndsWith("kib"))
        {
            return adjustedComparison[..^3] switch
            {
                { } value when double.TryParse(value, out var numericValue) => (numericValue * 1_024 )
                    .ToString(CultureInfo.InvariantCulture),
                _ => comparisonValue
            };
        }
        if (adjustedComparison.EndsWith("mib"))
        {
            return adjustedComparison[..^3] switch
            {
                { } value when double.TryParse(value, out var numericValue) => (numericValue * 1_048_576 )
                    .ToString(CultureInfo.InvariantCulture),
                _ => comparisonValue
            };
        }
        if (adjustedComparison.EndsWith("gib"))
        {
            return adjustedComparison[..^3] switch
            {
                { } value when double.TryParse(value, out var numericValue) => (numericValue * 1_073_741_824 )
                    .ToString(CultureInfo.InvariantCulture),
                _ => comparisonValue
            };
        }
        if (adjustedComparison.EndsWith("tib"))
        {
            return adjustedComparison[..^3] switch
            {
                { } value when double.TryParse(value, out var numericValue) => (numericValue * 1_099_511_627_776)
                    .ToString(CultureInfo.InvariantCulture),
                _ => comparisonValue
            };
        }
        return comparisonValue;
    }

}