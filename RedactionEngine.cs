using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace LogRedactor
{
    public sealed class RedactionEngine
    {
        private struct RedactionRule
        {
            public Regex Expression;
            public string Replacement;
            public Func<Match, string> DynamicEvaluator;
        }

        private readonly List<RedactionRule> _activeRules = new List<RedactionRule>();
        private readonly RedactionRuleOptions _options;

        private static readonly RegexOptions EngineRegexOptions =
            RegexOptions.Compiled | RegexOptions.CultureInvariant;

        // JWT: Base64/Base64URL header.payload.signature pattern
        private static readonly Regex JwtRegex = new Regex(
            @"\beyJ[a-zA-Z0-9_\-]{10,}\.[a-zA-Z0-9_\-]{10,}\.[a-zA-Z0-9_\-]{10,}\b",
            EngineRegexOptions);

        // API Keys, bearer tokens, AWS/GitHub/Slack credentials, and key-value secret pairs
        private static readonly Regex ApiTokenRegex = new Regex(
            @"(?i)(?:bearer\s+[a-zA-Z0-9_\-\.]{16,}|(?:api[_-]?key|secret|token|auth|password|passwd|client[_-]?secret)\s*[:=]\s*['""]?([a-zA-Z0-9_\-\.]{8,})['""]?|\b(?:ghp|gho|ghu|ghs|ghr)_[a-zA-Z0-9]{36}\b|\bAKIA[0-9A-Z]{16}\b|\bxox[baprs]-[0-9a-zA-Z]{10,48}\b)",
            EngineRegexOptions);

        // Email address pattern
        private static readonly Regex EmailRegex = new Regex(
            @"\b[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}\b",
            EngineRegexOptions);

        // IPv4 address pattern (0-255 octets)
        private static readonly Regex IPv4Regex = new Regex(
            @"\b(?:(?:25[0-5]|2[0-4][0-9]|1[0-9]{2}|[1-9]?[0-9])\.){3}(?:25[0-5]|2[0-4][0-9]|1[0-9]{2}|[1-9]?[0-9])\b",
            EngineRegexOptions);

        // IPv6 address pattern
        private static readonly Regex IPv6Regex = new Regex(
            @"\b(?:[0-9a-fA-F]{1,4}:){7}[0-9a-fA-F]{1,4}\b|\b(?:[0-9a-fA-F]{1,4}:){1,7}:|\b:(?::[0-9a-fA-F]{1,4}){1,7}\b|\b(?:[0-9a-fA-F]{1,4}:){1,6}:[0-9a-fA-F]{1,4}\b",
            EngineRegexOptions);

        // Primary account number / credit card patterns (13 to 19 digits)
        private static readonly Regex CreditCardRegex = new Regex(
            @"\b(?:\d{4}[-\s]?){3}\d{4}\b|\b\d{4}[-\s]?\d{6}[-\s]?\d{5}\b",
            EngineRegexOptions);

        // Aadhaar 12-digit identity pattern
        private static readonly Regex AadhaarRegex = new Regex(
            @"\b[2-9]\d{3}[-\s]?\d{4}[-\s]?\d{4}\b",
            EngineRegexOptions);

        // Phone number pattern (domestic and international representations)
        private static readonly Regex PhoneRegex = new Regex(
            @"(?:\+?\d{1,3}[-.\s]?)?\(?\d{3}\)?[-.\s]?\d{3}[-.\s]?\d{4}\b",
            EngineRegexOptions);

        // Sensitive URL query parameters
        private static readonly Regex UrlSensitiveRegex = new Regex(
            @"(?i)([?&](?:token|access_token|apikey|api_key|key|password|pass|secret|auth|session|session_id|jwt)=)[^&\s]+",
            EngineRegexOptions);

        // Windows user profile directory path pattern
        private static readonly Regex WindowsUserPathRegex = new Regex(
            @"(?i)(?:[A-Z]:\\Users\\)([^\s\\/]+)(\\?)",
            EngineRegexOptions);

        public RedactionEngine(RedactionRuleOptions options)
        {
            _options = options ?? new RedactionRuleOptions();
            CompileActiveRules();
        }

        private void CompileActiveRules()
        {
            _activeRules.Clear();

            if (_options.MaskJwt)
            {
                _activeRules.Add(new RedactionRule
                {
                    Expression = JwtRegex,
                    Replacement = _options.TagJwt
                });
            }

            if (_options.MaskApiToken)
            {
                _activeRules.Add(new RedactionRule
                {
                    Expression = ApiTokenRegex,
                    DynamicEvaluator = delegate(Match m)
                    {
                        string val = m.Value;
                        if (val.IndexOf(':') > 0 || val.IndexOf('=') > 0)
                        {
                            char delimiter = val.IndexOf(':') > 0 ? ':' : '=';
                            int dIdx = val.IndexOf(delimiter);
                            string prefix = val.Substring(0, dIdx + 1);
                            return prefix + " " + _options.TagApiToken;
                        }
                        if (val.StartsWith("bearer ", StringComparison.OrdinalIgnoreCase))
                        {
                            return "Bearer " + _options.TagApiToken;
                        }
                        return _options.TagApiToken;
                    }
                });
            }

            if (_options.MaskUrlSensitive)
            {
                _activeRules.Add(new RedactionRule
                {
                    Expression = UrlSensitiveRegex,
                    DynamicEvaluator = delegate(Match m)
                    {
                        return m.Groups[1].Value + _options.TagUrlSensitive;
                    }
                });
            }

            if (_options.MaskWindowsUserPath)
            {
                _activeRules.Add(new RedactionRule
                {
                    Expression = WindowsUserPathRegex,
                    DynamicEvaluator = delegate(Match m)
                    {
                        string trailingSlash = m.Groups[2].Success ? m.Groups[2].Value : "";
                        return @"C:\Users\[USERNAME]" + trailingSlash;
                    }
                });
            }

            if (_options.MaskEmail)
            {
                _activeRules.Add(new RedactionRule
                {
                    Expression = EmailRegex,
                    Replacement = _options.TagEmail
                });
            }

            if (_options.MaskIPv4)
            {
                _activeRules.Add(new RedactionRule
                {
                    Expression = IPv4Regex,
                    Replacement = _options.TagIPv4
                });
            }

            if (_options.MaskIPv6)
            {
                _activeRules.Add(new RedactionRule
                {
                    Expression = IPv6Regex,
                    Replacement = _options.TagIPv6
                });
            }

            if (_options.MaskCreditCard)
            {
                _activeRules.Add(new RedactionRule
                {
                    Expression = CreditCardRegex,
                    DynamicEvaluator = delegate(Match m)
                    {
                        string digitsOnly = m.Value.Replace("-", "").Replace(" ", "");
                        if (digitsOnly.Length >= 13 && digitsOnly.Length <= 19)
                        {
                            return _options.TagCreditCard;
                        }
                        return m.Value;
                    }
                });
            }

            if (_options.MaskAadhaar)
            {
                _activeRules.Add(new RedactionRule
                {
                    Expression = AadhaarRegex,
                    Replacement = _options.TagAadhaar
                });
            }

            if (_options.MaskPhone)
            {
                _activeRules.Add(new RedactionRule
                {
                    Expression = PhoneRegex,
                    Replacement = _options.TagPhone
                });
            }
        }

        public string ProcessLine(string line, out int redactionCount)
        {
            redactionCount = 0;
            if (string.IsNullOrEmpty(line))
                return line;

            string current = line;
            for (int i = 0; i < _activeRules.Count; i++)
            {
                RedactionRule rule = _activeRules[i];
                if (rule.DynamicEvaluator != null)
                {
                    int matchCount = 0;
                    current = rule.Expression.Replace(current, delegate(Match m)
                    {
                        matchCount++;
                        return rule.DynamicEvaluator(m);
                    });
                    redactionCount += matchCount;
                }
                else
                {
                    MatchCollection matches = rule.Expression.Matches(current);
                    if (matches.Count > 0)
                    {
                        redactionCount += matches.Count;
                        current = rule.Expression.Replace(current, rule.Replacement);
                    }
                }
            }

            return current;
        }
    }
}
