-- ============================================
-- Add DocumentType column for multi-model support
-- ============================================

ALTER TABLE Documents ADD DocumentType NVARCHAR(50) NOT NULL DEFAULT 'Invoice';

-- Update index to include DocumentType
CREATE NONCLUSTERED INDEX IX_Documents_Type ON Documents(DocumentType, UploadedDate DESC);
