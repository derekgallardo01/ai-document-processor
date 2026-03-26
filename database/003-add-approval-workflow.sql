-- ============================================
-- Add approval workflow columns
-- ============================================

ALTER TABLE Documents ADD ReviewStatus NVARCHAR(30) NULL; -- UnderReview, Approved, Rejected
ALTER TABLE Documents ADD ReviewedBy NVARCHAR(200) NULL;
ALTER TABLE Documents ADD ReviewedDate DATETIME2 NULL;
ALTER TABLE Documents ADD ReviewNotes NVARCHAR(MAX) NULL;

-- Update status check to include review statuses
ALTER TABLE Documents DROP CONSTRAINT CK_Documents_Status;
ALTER TABLE Documents ADD CONSTRAINT CK_Documents_Status
    CHECK (Status IN ('Uploaded', 'Processing', 'Completed', 'Failed'));
