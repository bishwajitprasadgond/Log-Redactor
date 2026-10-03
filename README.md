# Enterprise Log Redactor

Log Redactor is a high-performance Windows desktop utility engineered to sanitize sensitive data, credentials, personally identifiable information (PII), and network telemetry from enterprise log files.

The application operates as a standalone, zero-dependency executable (`LogRedactor.exe`). It runs on standard Windows installations without requiring external runtimes such as Python, Node.js, Electron, Java, or third-party dynamic libraries.

---

## Architectural Overview

### Multi-Gigabyte Streaming Pipeline
Traditional utilities load the entire file into memory (e.g. `File.ReadAllText`), resulting in `OutOfMemoryException` failures when processing large enterprise datasets. 

Log Redactor implements a bounded forward-only stream pipeline:

```
[ Disk Source ]
       │
       ▼ (64 KB Sequential Buffer)
[ FileStream (FileOptions.SequentialScan) ]
       │
       ▼
[ StreamReader (UTF-8 Forward-Only Cursor) ]
       │
       ▼
[ Redaction Engine (Compiled Regex Filter Bank) ]
       │
       ▼
[ StreamWriter (UTF-8 Direct Stream Flush) ]
       │
       ▼ (64 KB Output Buffer)
[ Redacted Destination File ]
```

* **Memory Footprint**: Maintained at approximately 4.5 MB to 8 MB of working set RAM regardless of whether the log is 50 MB or 100 GB.
* **Throughput**: Optimized for maximum sequential I/O rates on NVMe, SSD, and enterprise SAN storage.
* **Non-Blocking UI**: Execution runs inside background worker tasks with rate-limited UI message marshaling.

---

## Redaction Rule Specifications

The redaction engine executes a prioritized filter bank using compiled, culture-invariant regular expressions:

| Target Pattern | Scope and Match Criteria | Replacement Tag |
|---|---|---|
| **JWT Tokens** | Base64 and Base64URL header-payload-signature tokens (`eyJ...`) | `[JWT_TOKEN]` |
| **API Keys & Credentials** | Bearer tokens, GitHub personal access tokens (`ghp_`), AWS Access Keys (`AKIA`), Slack tokens (`xoxb-`), and key-value secret pairs (`api_key=...`, `secret:...`) | `[API_KEY]` |
| **Sensitive URL Query Parameters** | Query parameters containing sensitive variables (`?token=...`, `&password=...`, `&session=...`) | `[REDACTED_URL_PARAM]` |
| **Windows User Paths** | Sanitizes user account directories (`C:\Users\<user>\...`) while preserving application directory hierarchy | `C:\Users\[USERNAME]\...` |
| **Email Addresses** | RFC-compliant email address structures | `[EMAIL]` |
| **IPv4 Addresses** | Four-octet IPv4 addresses with 0–255 range validation boundaries | `[IP_V4]` |
| **IPv6 Addresses** | Full and compressed hexadecimal IPv6 notation | `[IP_V6]` |
| **Payment Cards / PAN** | 13-to-19 digit credit/debit card numbers with delimiter normalization | `[CARD_NUM]` |
| **Aadhaar Numbers** | 12-digit Indian national identity numbers | `[AADHAAR]` |
| **Phone Numbers** | Standard domestic and international telephone numbers | `[PHONE]` |

---

## User Interface Design

The user interface is built on native Windows Forms, customized for enterprise ergonomics:
* **Per-Monitor High-DPI Awareness**: Clean, crisp typography without Windows scaling blur.
* **Drag-and-Drop Zone**: Direct drag-and-drop ingestion of `.log`, `.txt`, `.csv`, `.out`, `.json` files.
* **Selective Masking Bank**: Granular checkboxes to enable or disable individual pattern detectors.
* **Interactive Preview**: Scans and displays the first 50 lines with active masking rules applied before initiating full-file processing.
* **Asynchronous Cancellation**: Thread-safe cancellation token integration that stops I/O operations cleanly upon request.

---

## Source Structure

```
LogRedactor/
├── LogRedactor.csproj          # .NET SDK project configuration
├── Program.cs                  # Entry point, DPI awareness initialization
├── MainForm.cs                 # Windows Forms user interface implementation
├── RedactionEngine.cs          # Regular expression filter bank and rule evaluator
├── StreamingLogProcessor.cs    # Multi-GB buffered stream engine
├── Models.cs                   # Configuration parameters and telemetry data models
├── GenerateSampleLogs.cs       # Multi-MB enterprise test log generator
├── build.bat                   # Automation script for native compilation
└── README.md                   # System documentation
```

---

## Build Instructions

### Method 1: Native Windows Compiler (Zero Installation)
Compile directly on any standard Windows machine using the built-in Microsoft .NET Framework C# compiler (`csc.exe`):

```cmd
build.bat
```

### Method 2: .NET 8 / Modern .NET SDK
Compile and publish a self-contained single-file executable using the .NET CLI:

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./publish
```

---

## Generating Test Data

To generate synthetic enterprise server logs for validation and throughput benchmarking:

```cmd
csc /target:exe /out:GenerateSampleLogs.exe GenerateSampleLogs.cs
GenerateSampleLogs.exe
```

This creates a realistic 10 MB test file (`enterprise_test_sample.log`) populated with varied enterprise log formats, database queries, access tokens, API calls, and network events.
