using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using Azure.Storage.Blobs;
using DocumentProcessor.Api.Hubs;
using DocumentProcessor.Api.Middleware;
using DocumentProcessor.Api.Repositories;
using DocumentProcessor.Api.Services;
using DocumentProcessor.Api.Services.Extractors;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Azure Entra ID Authentication (optional — enabled when AzureAd config is present)
var azureAdSection = builder.Configuration.GetSection("AzureAd");
if (azureAdSection.Exists() && !string.IsNullOrEmpty(azureAdSection["ClientId"]))
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApi(azureAdSection);
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.WithOrigins("http://localhost:3000", "http://localhost:3001")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials());
});

// Azure Blob Storage
var blobConnectionString = builder.Configuration["Azure:BlobStorage:ConnectionString"]!;
var containerName = builder.Configuration["Azure:BlobStorage:ContainerName"] ?? "documents";
var blobContainer = new BlobContainerClient(blobConnectionString, containerName);
builder.Services.AddSingleton(blobContainer);

// Azure AI Document Intelligence
var docEndpoint = builder.Configuration["Azure:DocumentIntelligence:Endpoint"]!;
var docKey = builder.Configuration["Azure:DocumentIntelligence:Key"]!;
var docClient = new DocumentAnalysisClient(new Uri(docEndpoint), new AzureKeyCredential(docKey));
builder.Services.AddSingleton(docClient);

// Document type detection
builder.Services.AddSingleton<DocumentTypeDetector>();

// Document extractors (strategy pattern)
builder.Services.AddSingleton<IDocumentExtractor, InvoiceExtractor>();
builder.Services.AddSingleton<IDocumentExtractor, ReceiptExtractor>();
builder.Services.AddSingleton<IDocumentExtractor, W2Extractor>();
builder.Services.AddSingleton<IDocumentExtractor, BusinessCardExtractor>();
builder.Services.AddSingleton<IDocumentExtractor, GeneralExtractor>();

// Repository
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();

// Services
builder.Services.AddScoped<DocumentService>();
builder.Services.AddScoped<ExportService>();

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();
app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("AllowAll");

// Auth middleware (only active when AzureAd is configured)
if (azureAdSection.Exists() && !string.IsNullOrEmpty(azureAdSection["ClientId"]))
{
    app.UseAuthentication();
    app.UseAuthorization();
}

app.MapControllers();
app.MapHub<DocumentHub>("/hubs/documents");

app.Run();

public partial class Program { }
