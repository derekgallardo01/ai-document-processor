# AI Document Processor

A full-stack intelligent document processing platform powered by **Azure AI Document Intelligence**. Upload invoices, receipts, W-2 tax forms, and business cards — the system automatically extracts fields, line items, and structured data using AI.

## Architecture

```
┌──────────────┐     ┌──────────────────┐     ┌─────────────────────┐
│  React 19    │────>│  .NET 10 API     │────>│  Azure AI Document  │
│  TypeScript  │<────│  SignalR          │<────│  Intelligence       │
│  Tailwind    │     │  Dapper           │     └─────────────────────┘
└──────────────┘     │  Repository       │
                     │  Pattern          │     ┌─────────────────────┐
                     │                   │────>│  Azure Blob Storage │
                     │                   │     └─────────────────────┘
                     │                   │
                     │                   │────>│  Azure SQL Database │
                     └──────────────────┘     └─────────────────────┘
```

## Features

- **Multi-Model AI Extraction** — Auto-detects document type and routes to the appropriate Azure AI model (Invoice, Receipt, W-2, Business Card, General)
- **Real-Time Processing** — SignalR-powered live progress pipeline showing each processing step
- **Document Comparison** — Side-by-side field comparison with match/mismatch highlighting
- **Export** — Download extracted data as Excel (.xlsx) or CSV
- **Approval Workflow** — Review, approve, or reject processed documents with notes
- **Dark Mode** — Full dark theme support with system preference detection
- **Dashboard** — Live metrics, processing volume charts, and status distribution
- **Editable Fields** — Manually correct extracted values with audit tracking

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Frontend | React 19, TypeScript, Tailwind CSS, Recharts, SignalR |
| Backend | .NET 10, ASP.NET Core, Dapper, SignalR |
| AI | Azure AI Document Intelligence (prebuilt models) |
| Database | Azure SQL Database |
| Storage | Azure Blob Storage |
| Auth | Azure Entra ID (optional, JWT bearer) |
| Export | ClosedXML for Excel generation |
| Testing | xUnit, NSubstitute, FluentAssertions |
| Infra | Bicep (IaC), GitHub Actions CI/CD, Docker |

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/)
- Azure subscription with:
  - Azure SQL Database
  - Azure Blob Storage account
  - Azure AI Document Intelligence resource

### 1. Database Setup

Run the SQL migrations in order against your Azure SQL Database:

```bash
# In Azure Portal Query Editor or SSMS:
database/001-create-tables.sql
database/002-add-document-type.sql
database/003-add-approval-workflow.sql
```

### 2. Configure Secrets

```bash
cd api/DocumentProcessor.Api

# Initialize user secrets
dotnet user-secrets init

# Set your Azure secrets
dotnet user-secrets set "Azure:DocumentIntelligence:Key" "<your-key>"
dotnet user-secrets set "Azure:BlobStorage:ConnectionString" "<your-connection-string>"
```

Update `appsettings.json` with your non-secret config (SQL connection string, Doc Intelligence endpoint).

### 3. Run the API

```bash
cd api/DocumentProcessor.Api
dotnet run
# API runs at http://localhost:5158
# Swagger UI at http://localhost:5158/swagger
```

### 4. Run the Frontend

```bash
cd ui
npm install
npm start
# UI runs at http://localhost:3000
```

### 5. Run Tests

```bash
cd api
dotnet test
# 34 tests covering controllers and services
```

## Docker

```bash
# Set environment variables in .env file, then:
docker-compose up --build
```

## Deploy to Azure

### Infrastructure as Code

```bash
az deployment group create \
  --resource-group <your-rg> \
  --template-file infra/main.bicep \
  --parameters infra/parameters.json \
  --parameters sqlAdminLogin=<user> sqlAdminPassword=<pass>
```

### CI/CD

Push to `main` triggers the GitHub Actions deploy pipeline. Configure these secrets in your repo:

- `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`
- `AZURE_RESOURCE_GROUP`
- `SQL_ADMIN_LOGIN`, `SQL_ADMIN_PASSWORD`
- `AZURE_STATIC_WEB_APPS_TOKEN`

## Project Structure

```
ai-document-processor/
├── api/
│   ├── DocumentProcessor.Api/
│   │   ├── Controllers/        # REST API endpoints
│   │   ├── Hubs/               # SignalR real-time hub
│   │   ├── Middleware/         # Exception handling
│   │   ├── Models/             # Data models & DTOs
│   │   ├── Repositories/      # Data access (Dapper)
│   │   └── Services/
│   │       ├── Extractors/    # Strategy pattern per doc type
│   │       ├── DocumentService.cs
│   │       ├── DocumentTypeDetector.cs
│   │       └── ExportService.cs
│   ├── DocumentProcessor.Tests/
│   │   ├── Controllers/
│   │   └── Services/
│   └── Dockerfile
├── ui/
│   ├── src/
│   │   ├── components/
│   │   │   ├── common/        # Shared components
│   │   │   ├── dashboard/     # Metrics, charts
│   │   │   ├── documents/     # List, detail, compare
│   │   │   └── upload/        # Upload zone, type selector
│   │   ├── hooks/             # useTheme
│   │   ├── api.ts             # Type-safe API client
│   │   └── useSignalR.ts      # Real-time hook
│   ├── Dockerfile
│   └── nginx.conf
├── database/                   # SQL migrations
├── infra/                      # Bicep IaC templates
├── .github/workflows/          # CI/CD pipelines
└── docker-compose.yml
```

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/documents` | List all documents (optional `?status=` filter) |
| GET | `/api/documents/{id}` | Get document by ID |
| GET | `/api/documents/{id}/fields` | Get extracted fields |
| GET | `/api/documents/{id}/lineitems` | Get line items |
| GET | `/api/documents/{id}/log` | Get processing log |
| GET | `/api/documents/metrics` | Dashboard metrics |
| POST | `/api/documents/upload` | Upload document (multipart, optional `documentType`) |
| PUT | `/api/documents/{id}/fields/{fieldId}` | Update extracted field |
| POST | `/api/documents/{id}/review` | Approve/reject document |
| DELETE | `/api/documents/{id}` | Delete document |
| GET | `/api/export/{id}/excel` | Export to Excel |
| GET | `/api/export/{id}/csv` | Export to CSV |
| POST | `/api/export/batch/excel` | Batch export to Excel |
