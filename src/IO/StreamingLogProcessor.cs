using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using LogRedactor.Engine;
using LogRedactor.Models;

namespace LogRedactor.IO
{
    public class StreamingLogProcessor
    {
        private const int BufferSize = 65536;

        public event Action<ProcessingProgressReport> ProgressChanged;

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

            long lastReportTicks = 0;
            long reportIntervalTicks = Stopwatch.Frequency / 10;

            string outDir = Path.GetDirectoryName(outputFilePath);
            if (!string.IsNullOrEmpty(outDir) && !Directory.Exists(outDir))
            {
                Directory.CreateDirectory(outDir);
            }

            try
            {
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

                        bytesRead = inStream.Position;
                        linesProcessed++;

                        int lineRedactionCount;
                        string redactedLine = engine.ProcessLine(line, out lineRedactionCount);
                        totalRedactions += lineRedactionCount;

                        writer.WriteLine(redactedLine);

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
