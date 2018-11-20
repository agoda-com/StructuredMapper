using System;

namespace StructuredMapper.Test.Api.Helpers
{
    public static class PhoneNumberFormatter
    {
        public static string ToInternational(string localPhoneNumber, int countryId)
        {
            string countryCode;
            switch (countryId)
            {
                case 1: countryCode = "66"; break;
                case 2: countryCode = "44"; break;
                default: throw new NotImplementedException(countryId.ToString());
            }
            
            return $"+{countryCode}{localPhoneNumber.Substring(1)}";
        }
    }
}