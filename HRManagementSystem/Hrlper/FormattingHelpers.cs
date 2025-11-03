namespace HRManagementSystem.Hrlper
{
    public static class FormattingHelpers
    {

        public static string FormatCurrency(decimal amount)
        {
            if (Math.Abs(amount) >= 10000000) // 1 Crore
                return $"₹{amount / 10000000:N2} Cr";
            if (Math.Abs(amount) >= 100000) // 1 Lakh
                return $"₹{amount / 100000:N2} L";

            // Format with standard Indian grouping below 1 Lakh
            return $"₹{amount.ToString("N2", System.Globalization.CultureInfo.GetCultureInfo("en-IN"))}";
            // Using CultureInfo for correct comma placement below Lakhs
        }
    }
}
