using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace LogRedactor
{
    public class StreamingLogProcessor
    {
        // Enterprise optimized buffer size: 64 KB for high-throughput sequential disk I/O
        private const int BufferSize = 65536;

        public event Action<ProcessingProgressReport> ProgressChanged;

        /// <summary>
        /// Streams and processes multi-gigabyte files with constant, tiny memory footprint.
        /// </summary>
        public ProcessingSummary ProcessFile(
            string inputFilePath,
            string outputFilePath,
            RedactionRuleOptions options,
            CancellationToken cancellationToken)
        {
            var summary = new ProcessingSummary();
            summary.InputFilePath = inputFilePath;
            summary.OutputFilePath = outputFilePath;

            var stopwatch = Stopwatch.StartNew();

            if (!File.Exists(inputFilePath))
            {
                summary.Success = false;
                summary.ErrorMessage = "Input file does not exist: " + inputFilePath;
                return summary;
            }

            var fileInfo = new FileInfo(inputFilePath);
            long totalBytes = fileInfo.Length;
            long bytesRead = 0;
            long linesProcessed = 0;
            long totalRedactions = 0;

            var engine = new RedactionEngine(options);

            // Throttle UI progress notifications to avoid UI message loop saturation
            long lastReportTicks = 0;
            long reportIntervalTicks = Stopwatch.Frequency / 10; // 10 updates per second

            // Ensure directory exists for output
            string outDir = Path.GetDirectoryName(outputFilePath);
            if (!string.IsNullOrEmpty(outDir) && !Directory.Exists(outDir))
            {
                Directory.CreateDirectory(outDir);
            }

            try
            {
                // Open streams with sequential scan optimization and dedicated large buffers
                using (var inStream = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, FileOptions.SequentialScan))
                using (var reader = new StreamReader(inStream, Encoding.UTF8, true, BufferSize))
                using (var outStream = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize))
                using (var writer = new StreamWriter(outStream, new UTF8Encoding(false), BufferSize))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            summary.Cancelled = true;
                            summary.Success = false;
                            summary.ErrorMessage = "Operation cancelled by user.";
                            break;
                        }

                        // Approximate bytes read from underlying stream position
                        bytesRead = inStream.Position;
                        linesProcessed++;

                        int lineRedactionCount;
                        string redactedLine = engine.ProcessLine(line, out lineRedactionCount);
                        totalRedactions += lineRedactionCount;

                        writer.WriteLine(redactedLine);

                        // Report progress throttled
                        long currentTicks = stopwatch.ElapsedTicks;
                        if (currentTicks - lastReportTicks > reportIntervalTicks)
                        {
                            lastReportTicks = currentTicks;
                            double elapsedSec = stopwatch.Elapsed.TotalSeconds;
                            double mbps = elapsedSec > 0 ? (bytesRead / (1024.0 * 1024.0)) / elapsedSec : 0;

                            Action<ProcessingProgressReport> handler = ProgressChanged;
                            if (handler != null)
                            {
                                var report = new ProcessingProgressReport();
                                report.BytesRead = bytesRead;
                                report.TotalBytes = totalBytes;
                                report.LinesProcessed = linesProcessed;
                                report.RedactionsApplied = totalRedactions;
                                report.MegaBytesPerSecond = mbps;
                                report.ElapsedTime = stopwatch.Elapsed;
                                handler(report);
                            }
                        }
                    }

                    // Flush all buffers
                    writer.Flush();
                    outStream.Flush();
                }

                stopwatch.Stop();

                if (!summary.Cancelled)
                {
                    summary.Success = true;
                    summary.TotalBytesProcessed = totalBytes;
                    summary.TotalLinesProcessed = linesProcessed;
                    summary.TotalRedactions = totalRedactions;
                    summary.Duration = stopwatch.Elapsed;

                    // Final progress event at 100%
                    double totalSec = stopwatch.Elapsed.TotalSeconds;
                    double finalMbps = totalSec > 0 ? (totalBytes / (1024.0 * 1024.0)) / totalSec : 0;
                    
                    Action<ProcessingProgressReport> handler = ProgressChanged;
                    if (handler != null)
                    {
                        var finalReport = new ProcessingProgressReport();
                        finalReport.BytesRead = totalBytes;
                        finalReport.TotalBytes = totalBytes;
                        finalReport.LinesProcessed = linesProcessed;
                        finalReport.RedactionsApplied = totalRedactions;
                        finalReport.MegaBytesPerSecond = finalMbps;
                        finalReport.ElapsedTime = stopwatch.Elapsed;
                        handler(finalReport);
                    }
                }
            }
            catch (Exception ex)
            {
                summary.Success = false;
                summary.ErrorMessage = ex.Message;
            }

            return summary;
        }
    }
}
