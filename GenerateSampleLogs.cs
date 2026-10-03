using System;
using System.IO;
using System.Text;

namespace LogRedactor
{
    class GenerateSampleLogs
    {
        private static readonly string[] Users = { "john.doe", "sarah.connor", "michael.scott", "alice.wong", "david.miller" };
        private static readonly string[] Domains = { "company.com", "enterprise.org", "internal.corp", "cloudinfra.io", "prod-api.internal" };
        private static readonly string[] Hostnames = { "srv-app01.prod.lan", "db-cluster-node2.internal.corp", "k8s-ingress-master.local", "auth-gateway-primary.internal" };
        private static readonly string[] Endpoints = { "/api/v2/auth/login", "/api/v1/users/profile", "/api/v3/payments/charge", "/api/v1/tokens/refresh", "/admin/system/export" };
        private static readonly string[] LogLevels = { "INFO", "INFO", "WARN", "ERROR", "DEBUG" };

        static int Main(string[] args)
        {
            int targetLines = 35000;
            string outputFile = "enterprise_test_sample.log";

            if (args.Length > 0)
            {
                int customLines;
                if (int.TryParse(args[0], out customLines))
                {
                    targetLines = customLines;
                }
            }

            Console.WriteLine("==============================================================");
            Console.WriteLine("  Generating Enterprise Test Log: " + outputFile);
            Console.WriteLine("  Target line count: " + targetLines.ToString("N0"));
            Console.WriteLine("==============================================================");

            var random = new Random(42);
            DateTime baseTimestamp = new DateTime(2026, 10, 3, 8, 0, 0);

            using (var fs = new FileStream(outputFile, FileMode.Create, FileAccess.Write, FileShare.None, 65536))
            using (var writer = new StreamWriter(fs, Encoding.UTF8, 65536))
            {
                for (int i = 0; i < targetLines; i++)
                {
                    DateTime timestamp = baseTimestamp.AddMilliseconds(i * 125);
                    string level = LogLevels[random.Next(LogLevels.Length)];
                    string user = Users[random.Next(Users.Length)];
                    string domain = Domains[random.Next(Domains.Length)];
                    string hostname = Hostnames[random.Next(Hostnames.Length)];
                    string email = user + "@" + domain;
                    string endpoint = Endpoints[random.Next(Endpoints.Length)];

                    string ipv4 = string.Format("{0}.{1}.{2}.{3}", random.Next(10, 220), random.Next(1, 254), random.Next(1, 254), random.Next(1, 254));
                    string ipv6 = string.Format("2001:0db8:{0:x4}:{1:x4}:0000:8a2e:0370:{2:x4}", random.Next(0, 65535), random.Next(0, 65535), random.Next(0, 65535));
                    string mac = string.Format("{0:X2}:{1:X2}:{2:X2}:{3:X2}:{4:X2}:{5:X2}", random.Next(0, 256), random.Next(0, 256), random.Next(0, 256), random.Next(0, 256), random.Next(0, 256), random.Next(0, 256));
                    int port = random.Next(1024, 65535);

                    string phone = string.Format("+1 ({0}) {1}-{2}", random.Next(200, 999), random.Next(100, 999), random.Next(1000, 9999));
                    string cardNum = string.Format("{0}-{1}-{2}-{3}", random.Next(4000, 4999), random.Next(1000, 9999), random.Next(1000, 9999), random.Next(1000, 9999));
                    string aadhaar = string.Format("{0} {1} {2}", random.Next(2000, 9999), random.Next(1000, 9999), random.Next(1000, 9999));
                    string jwt = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9." + Convert.ToBase64String(Encoding.UTF8.GetBytes("sub:" + user + ",iat:1516239022")) + ".SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";
                    string apiKey = "AKIA" + Guid.NewGuid().ToString("N").Substring(0, 16).ToUpper();
                    string winPath = @"C:\Users\" + user + @"\AppData\Roaming\EnterpriseApp\session.dat";

                    int patternVariant = i % 6;
                    string logLine;

                    switch (patternVariant)
                    {
                        case 0:
                            logLine = string.Format("{0:yyyy-MM-dd HH:mm:ss.fff} [{1}] [AuthService] User '{2}' authenticated from {3}:{4} (IPv6: {5}, MAC: {6}) with bearer {7}",
                                timestamp, level, email, ipv4, port, ipv6, mac, jwt);
                            break;
                        case 1:
                            logLine = string.Format("{0:yyyy-MM-dd HH:mm:ss.fff} [{1}] [PaymentGateway] Processing card payment for account holder Phone: {2}, Card: {3}, Aadhaar: {4} on host: {5}",
                                timestamp, level, phone, cardNum, aadhaar, hostname);
                            break;
                        case 2:
                            logLine = string.Format("{0:yyyy-MM-dd HH:mm:ss.fff} [{1}] [HttpClient] Outgoing request to https://{2}{3}?apikey={4}&session_id=sess_{5:x8} from host {6}:{7}",
                                timestamp, level, domain, endpoint, apiKey, random.Next(), ipv4, port);
                            break;
                        case 3:
                            logLine = string.Format("{0:yyyy-MM-dd HH:mm:ss.fff} [{1}] [StorageService] Backing up configuration cache from user workspace: {2} [Host: {3}, Secret: ghp_{4}]",
                                timestamp, level, winPath, hostname, Guid.NewGuid().ToString("N"));
                            break;
                        case 4:
                            logLine = string.Format("{0:yyyy-MM-dd HH:mm:ss.fff} [{1}] [SecurityFilter] Intercepted token refresh request for {2} containing query param url: https://internal.auth.corp/v1?token={3}&user={4} (Src MAC: {5})",
                                timestamp, level, email, jwt, user, mac);
                            break;
                        default:
                            logLine = string.Format("{0:yyyy-MM-dd HH:mm:ss.fff} [{1}] [DatabaseCluster] Node sync initiated from {2}:{3} to node: {4} with client_secret: cs_{5:x12}",
                                timestamp, level, ipv4, port, hostname, random.Next());
                            break;
                    }

                    writer.WriteLine(logLine);
                }

                writer.Flush();
            }

            var fileInfo = new FileInfo(outputFile);
            Console.WriteLine("Done. Generated file size: " + (fileInfo.Length / (1024.0 * 1024.0)).ToString("F2") + " MB");
            Console.WriteLine("Path: " + fileInfo.FullName);
            return 0;
        }
    }
}
