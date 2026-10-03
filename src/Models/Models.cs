using System;

namespace LogRedactor.Models
{
    [Flags]
    public enum RedactionCategory
    {
        None = 0,
        IPv4 = 1 << 0,
        IPv6 = 1 << 1,
        MacAddress = 1 << 2,
        DomainHostname = 1 << 3,
        PortNumber = 1 << 4,
        Email = 1 << 5,
        Phone = 1 << 6,
        CreditCard = 1 << 7,
        Aadhaar = 1 << 8,
        JWT = 1 << 9,
        ApiToken = 1 << 10,
        UrlQuerySensitive = 1 << 11,
        WindowsPathUser = 1 << 12,
        All = ~0
    }

    public class RedactionRuleOptions
    {
        public bool MaskIPv4 { get; set; }
        public bool MaskIPv6 { get; set; }
        public bool MaskMacAddress { get; set; }
        public bool MaskDomainHostname { get; set; }
        public bool MaskPortNumber { get; set; }
        public bool MaskEmail { get; set; }
        public bool MaskPhone { get; set; }
        public bool MaskCreditCard { get; set; }
        public bool MaskAadhaar { get; set; }
        public bool MaskJwt { get; set; }
        public bool MaskApiToken { get; set; }
        public bool MaskUrlSensitive { get; set; }
        public bool MaskWindowsUserPath { get; set; }

        public string TagIPv4 { get; set; }
        public string TagIPv6 { get; set; }
        public string TagMacAddress { get; set; }
        public string TagDomainHostname { get; set; }
        public string TagPortNumber { get; set; }
        public string TagEmail { get; set; }
        public string TagPhone { get; set; }
        public string TagCreditCard { get; set; }
        public string TagAadhaar { get; set; }
        public string TagJwt { get; set; }
        public string TagApiToken { get; set; }
        public string TagUrlSensitive { get; set; }
        public string TagWindowsUserPath { get; set; }

        public RedactionRuleOptions()
        {
            MaskIPv4 = true;
            MaskIPv6 = true;
            MaskMacAddress = true;
            MaskDomainHostname = true;
            MaskPortNumber = true;
            MaskEmail = true;
            MaskPhone = true;
            MaskCreditCard = true;
            MaskAadhaar = true;
            MaskJwt = true;
            MaskApiToken = true;
            MaskUrlSensitive = true;
            MaskWindowsUserPath = true;

            TagIPv4 = "[IP_V4]";
            TagIPv6 = "[IP_V6]";
            TagMacAddress = "[MAC_ADDR]";
            TagDomainHostname = "[HOST_DOMAIN]";
            TagPortNumber = "[PORT]";
            TagEmail = "[EMAIL]";
            TagPhone = "[PHONE]";
            TagCreditCard = "[CARD_NUM]";
            TagAadhaar = "[AADHAAR]";
            TagJwt = "[JWT_TOKEN]";
            TagApiToken = "[API_KEY]";
            TagUrlSensitive = "[REDACTED_URL_PARAM]";
            TagWindowsUserPath = @"C:\Users\[USERNAME]\";
        }
    }

    public class ProcessingProgressReport
    {
        public long BytesRead { get; set; }
        public long TotalBytes { get; set; }
        public long LinesProcessed { get; set; }
        public long RedactionsApplied { get; set; }
        public double PercentComplete
        {
            get { return TotalBytes > 0 ? (double)BytesRead / TotalBytes * 100.0 : 0.0; }
        }
        public double MegaBytesPerSecond { get; set; }
        public TimeSpan ElapsedTime { get; set; }
    }

    public class ProcessingSummary
    {
        public bool Success { get; set; }
        public bool Cancelled { get; set; }
        public string ErrorMessage { get; set; }
        public long TotalBytesProcessed { get; set; }
        public long TotalLinesProcessed { get; set; }
        public long TotalRedactions { get; set; }
        public TimeSpan Duration { get; set; }
        public string InputFilePath { get; set; }
        public string OutputFilePath { get; set; }

        public ProcessingSummary()
        {
            ErrorMessage = string.Empty;
            InputFilePath = string.Empty;
            OutputFilePath = string.Empty;
        }
    }
}
