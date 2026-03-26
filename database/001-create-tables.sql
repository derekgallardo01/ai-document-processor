-- ============================================
-- AI Document Processor
-- Azure SQL Database Schema
-- ============================================

-- Documents (uploaded files)
CREATE TABLE Documents (
    DocumentID INT IDENTITY(1,1) PRIMARY KEY,
    FileName NVARCHAR(255) NOT NULL,
    FileType NVARCHAR(50) NOT NULL,
    FileSize BIGINT NOT NULL,
    BlobUrl NVARCHAR(1000) NOT NULL,
    Status NVARCHAR(30) NOT NULL DEFAULT 'Uploaded', -- Uploaded, Processing, Completed, Failed
    UploadedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ProcessedDate DATETIME2 NULL,
    PageCount INT NULL,
    ModelId NVARCHAR(100) NULL,
    ErrorMessage NVARCHAR(MAX) NULL,
    CONSTRAINT CK_Documents_Status CHECK (Status IN ('Uploaded', 'Processing', 'Completed', 'Failed'))
);

-- Extracted Fields (key-value pairs from document)
CREATE TABLE ExtractedFields (
    FieldID INT IDENTITY(1,1) PRIMARY KEY,
    DocumentID INT NOT NULL,
    FieldName NVARCHAR(200) NOT NULL,
    FieldValue NVARCHAR(MAX) NULL,
    Confidence DECIMAL(5,4) NULL, -- 0.0000 to 1.0000
    FieldType NVARCHAR(50) NULL, -- string, date, number, currency, etc.
    PageNumber INT NULL,
    IsManuallyEdited BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_ExtractedFields_Document FOREIGN KEY (DocumentID) REFERENCES Documents(DocumentID) ON DELETE CASCADE
);

-- Line Items (for invoices with tables)
CREATE TABLE LineItems (
    LineItemID INT IDENTITY(1,1) PRIMARY KEY,
    DocumentID INT NOT NULL,
    RowIndex INT NOT NULL,
    Description NVARCHAR(500) NULL,
    Quantity DECIMAL(10,2) NULL,
    UnitPrice DECIMAL(10,2) NULL,
    Amount DECIMAL(10,2) NULL,
    Confidence DECIMAL(5,4) NULL,
    CONSTRAINT FK_LineItems_Document FOREIGN KEY (DocumentID) REFERENCES Documents(DocumentID) ON DELETE CASCADE
);

-- Processing Log (audit trail)
CREATE TABLE ProcessingLog (
    LogID INT IDENTITY(1,1) PRIMARY KEY,
    DocumentID INT NOT NULL,
    Action NVARCHAR(100) NOT NULL,
    Details NVARCHAR(MAX) NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_ProcessingLog_Document FOREIGN KEY (DocumentID) REFERENCES Documents(DocumentID) ON DELETE CASCADE
);

-- Indexes
CREATE NONCLUSTERED INDEX IX_Documents_Status ON Documents(Status, UploadedDate DESC);
CREATE NONCLUSTERED INDEX IX_ExtractedFields_Document ON ExtractedFields(DocumentID);
CREATE NONCLUSTERED INDEX IX_LineItems_Document ON LineItems(DocumentID);
CREATE NONCLUSTERED INDEX IX_ProcessingLog_Document ON ProcessingLog(DocumentID, CreatedDate DESC);
