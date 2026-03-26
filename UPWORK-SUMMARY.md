# AI Document Processor — Portfolio Summary

## Project Title
**AI-Powered Document Processing Platform (Azure + React + .NET)**

---

## Short Description (for Upwork profile)

Built a full-stack intelligent document processing platform using Azure AI Document Intelligence, .NET 10, React 19, and SignalR. The system automatically extracts structured data from invoices, receipts, W-2 tax forms, and business cards with real-time processing visualization, Excel/CSV export, document comparison, and approval workflows.

---

## Detailed Description

### Overview

Designed and built an end-to-end AI document processing platform that transforms unstructured documents into structured, searchable data. Users upload PDFs or images, and the system automatically identifies the document type, routes it to the appropriate AI model, extracts fields and line items, and presents the results in a modern dashboard — all in real time.

### Key Features Delivered

**AI-Powered Extraction**
- Multi-model document intelligence supporting 5 document types: Invoices, Receipts, W-2 Tax Forms, Business Cards, and General Documents
- Automatic document type detection from file name patterns with manual override option
- Strategy pattern architecture allowing easy addition of new document types
- Confidence scoring on every extracted field
- SSN masking for sensitive W-2 data

**Real-Time Processing Pipeline**
- SignalR WebSocket integration for live processing updates
- Animated step-by-step pipeline visualization: Upload → AI Analysis → Extraction → Complete
- Auto-opens document detail panel on processing completion
- Processing queue view with per-document status tracking

**Enterprise Workflow Features**
- Approval workflow with Approve/Reject actions and reviewer notes
- Manual field editing with audit trail tracking
- Export to Excel (.xlsx) with formatted multi-sheet workbooks and CSV
- Batch export across multiple documents
- Side-by-side document comparison with field match/mismatch highlighting

**Modern UI/UX**
- React 19 with TypeScript and Tailwind CSS v4
- Full dark mode with system preference detection and localStorage persistence
- Interactive dashboard with Recharts (processing volume bar chart, status pie chart)
- Responsive sidebar navigation with collapse toggle
- Document type icons and filter badges

**Production-Grade Architecture**
- Repository pattern with Dapper micro-ORM for clean data access
- Azure Entra ID (Azure AD) authentication ready (JWT bearer)
- Secrets management via .NET User Secrets (removed from source control)
- 34 unit tests with xUnit, NSubstitute, and FluentAssertions
- Infrastructure as Code with Azure Bicep templates
- CI/CD pipelines with GitHub Actions (build, test, deploy)
- Docker support with multi-stage builds and docker-compose

### Tech Stack

| Layer | Technology |
|-------|-----------|
| Frontend | React 19, TypeScript, Tailwind CSS v4, Recharts, SignalR |
| Backend | .NET 10, ASP.NET Core Web API, Dapper, SignalR |
| AI/ML | Azure AI Document Intelligence (5 prebuilt models) |
| Database | Azure SQL Database |
| Storage | Azure Blob Storage (SAS token auth) |
| Auth | Azure Entra ID / MSAL (JWT bearer) |
| Export | ClosedXML for Excel generation |
| Testing | xUnit, NSubstitute, FluentAssertions (34 tests) |
| DevOps | Azure Bicep IaC, GitHub Actions CI/CD, Docker |

### Architecture Highlights

- **Strategy Pattern** for document extractors — each document type has its own extractor class implementing `IDocumentExtractor`, making it trivial to add new document types
- **Repository Pattern** separating data access from controllers — enables unit testing and clean separation of concerns
- **SignalR Hub** broadcasting granular processing progress events — not just "changed" but step-by-step updates enabling the animated pipeline UI
- **SAS Token Generation** for secure blob access — Document Intelligence accesses uploaded files via time-limited SAS URLs

### Results

- Processes documents in 3-5 seconds end-to-end
- 85-97% confidence on most extracted fields
- Supports 5 document types out of the box
- 34 passing tests with zero build warnings
- Full dark mode, responsive design, real-time updates

---

## Skills Demonstrated

- Azure AI Services (Document Intelligence / Form Recognizer)
- Azure Cloud Architecture (Blob Storage, SQL Database, Key Vault, App Service)
- .NET 10 / ASP.NET Core Web API
- React 19 + TypeScript
- Tailwind CSS v4
- SignalR (real-time WebSockets)
- Repository Pattern / Strategy Pattern / Clean Architecture
- Unit Testing (xUnit, NSubstitute, FluentAssertions)
- Infrastructure as Code (Bicep)
- CI/CD (GitHub Actions)
- Docker / Docker Compose

---

## Tags (for Upwork)

Azure, Azure AI, Document Intelligence, .NET, C#, ASP.NET Core, React, TypeScript, Tailwind CSS, SignalR, SQL Server, Blob Storage, REST API, Full Stack Development, AI/ML Integration, Infrastructure as Code, CI/CD, Docker
