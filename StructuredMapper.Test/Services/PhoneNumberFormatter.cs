namespace StructuredMapper.Test.Services
{
    public static class PhoneNumberFormatter
    {
        public static string ToInternational(string localPhoneNumber)
        {
            return "+66" + localPhoneNumber.Substring(1);
        }
    }
}