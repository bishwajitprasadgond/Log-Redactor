# Enterprise Log Redactor

Log Redactor is a high-performance Windows desktop utility engineered to sanitize sensitive data, credentials, personally identifiable information (PII), and network telemetry from enterprise log files.

The application operates as a standalone, zero-dependency executable (`LogRedactor.exe`). It runs on standard Windows installations without requiring external runtimes such as Python, Node.js, Electron, Java, or third-party dynamic libraries.

---

## Architecture Diagrams

### 1. System Architecture and Component Layering

```mermaid
graph TD
    subgraph UI_Layer [Presentation Layer - Windows Forms]
        MainForm[MainForm.cs]
        DropZone[Drag & Drop Ingestion]
        OptionsCard[Rule Selection Checkboxes]
        ProgressTelemetry[Progress & Throughput Reporter]
        PreviewConsole[50-Line Preview Console]
    end

    subgraph Core_Engine [Core Processing Pipeline]
        Processor[StreamingLogProcessor.cs]
        Engine[RedactionEngine.cs]
        RulesBank[Compiled Regex Filter Bank]
        Models[Models.cs - Telemetry & DTOs]
    end

    subgraph IO_Layer [Buffered Streaming I/O]
        InStream[FileStream: SequentialScan - 64KB Buffer]
        Reader[StreamReader: UTF-8 Forward Cursor]
        Writer[StreamWriter: UTF-8 Stream Flush]
        OutStream[FileStream: Direct Disk Write - 64KB Buffer]
    end

    DropZone --> MainForm
    OptionsCard --> MainForm
    MainForm -->|Start Async Task| Processor
    Processor --> InStream
    InStream --> Reader
    Reader -->|Line Stream| Engine
    Engine --> RulesBank
    Engine -->|Sanitized Line| Writer
    Writer --> OutStream
    Processor -.->|Throttled Telemetry| ProgressTelemetry
```

### 2. Multi-Gigabyte Constant Memory Streaming Pipeline

```mermaid
flowchart LR
    DiskIn[("Input File (GBs)\n[Disk]")] -->|64 KB Block| FSIn["FileStream\n(SequentialScan)"]
    FSIn -->|Line Cursor| SR["StreamReader\n(Forward Only)"]
    SR -->|Raw Line String| RE["Redaction Engine\n(Regex Filter Bank)"]
    RE -->|Redacted Line String| SW["StreamWriter\n(Direct Flush)"]
    SW -->|64 KB Block| FSOut["FileStream\n(Disk Write)"]
    FSOut --> DiskOut[("Redacted File\n[Disk]")]

    subgraph Memory_Bound [Bounded Working Set: ~4.5 MB - 8 MB RAM]
        FSIn
        SR
        RE
        SW
        FSOut
    end
```

### 3. Masking Rule Execution Order and Priority Filter Bank

```mermaid
flowchart TD
    RawLine([Raw Input Line]) --> R1{JWT Tokens\n eyJ...}
    R1 -->|Match / Replace [JWT_TOKEN]| R2{API Keys & Bearers\n AKIA, ghp, xoxb, secret}
    R1 -->|Next| R2
    
    R2 -->|Match / Replace [API_KEY]| R3{URL Query Secrets\n ?token=..., &apikey=...}
    R2 -->|Next| R3
    
    R3 -->|Match / Replace [REDACTED_URL_PARAM]| R4{Windows User Paths\n C:\\Users\\user\\...}
    R3 -->|Next| R4
    
    R4 -->|Match / Replace C:\\Users\\[USERNAME]\\]| R5{Email Addresses\n RFC Standard}
    R4 -->|Next| R5
    
    R5 -->|Match / Replace [EMAIL]| R6{IPv4 / IPv6 Addresses\n Octet Boundary Checked}
    R5 -->|Next| R6
    
    R6 -->|Match / Replace [IP_V4] / [IP_V6]| R7{Credit Cards & Aadhaar\n 13-19 Digit / 12-Digit IDs}
    R6 -->|Next| R7
    
    R7 -->|Match / Replace [CARD_NUM] / [AADHAAR]| R8{Phone Numbers\n Domestic & Intl}
    R7 -->|Next| R8
    
    R8 -->|Match / Replace [PHONE]| RedactedLine([Sanitized Output Line])
    R8 -->|Next| RedactedLine
```

### 4. Asynchronous Task Execution and Throttled Telemetry Flow

```mermaid
sequenceDiagram
    autonumber
    actor User as Operator
    participant UI as MainForm (UI Thread)
    participant Worker as Background Task (Worker Thread)
    participant Stream as StreamingLogProcessor
    participant Redactor as RedactionEngine
    participant Disk as Disk Storage

    User->>UI: Drop log file & Click "Redact Log"
    UI->>Worker: Task.Factory.StartNew(ProcessFile)
    Worker->>Stream: Initialize FileStreams (64KB buffer)
    
    loop Sequential Line-by-Line Read
        Stream->>Disk: ReadLine()
        Stream->>Redactor: ProcessLine(rawLine)
        Redactor-->>Stream: sanitizedLine, redactionsCount
        Stream->>Disk: WriteLine(sanitizedLine)
        
        opt Stopwatch interval >= 100ms
            Stream-->>UI: BeginInvoke(ProgressChanged: % complete, MB/s, lines)
            UI->>UI: Update progress bar & throughput stats
        end
    end

    Stream->>Disk: Flush() & Close()
    Stream-->>Worker: ProcessingSummary
    Worker-->>UI: InvokeCompletion(Success / Summary dialog)
    UI->>User: Display processing summary and completion notice
```

---

## Architectural Overview

### Multi-Gigabyte Streaming Pipeline
Traditional utilities load the entire file into memory (e.g. `File.ReadAllText`), resulting in `OutOfMemoryException` failures when processing large enterprise datasets. 

Log Redactor implements a bounded forward-only stream pipeline:
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
GenerateSampleLogs.exe 35000
```

This creates a realistic 7.15 MB test file (`enterprise_test_sample.log`) populated with varied enterprise log formats, database queries, access tokens, API calls, and network events.
