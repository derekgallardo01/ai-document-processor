# Infrastructure — AI Document Processor

## Monthly Cost: ~$5/month

## Quick Shutdown
```powershell
# Resources are in a SHARED resource group (rg-operations-tracker)
# Delete individual resources instead of the whole group:
az sql db delete --server rg-operations-tracker-server-2 --resource-group rg-operations-tracker --name ai-document-processor-db --yes
az cognitiveservices account delete --name doc-intel-dg --resource-group rg-operations-tracker
```

## Azure Resources

**Resource Group:** `rg-operations-tracker` (eastus) — **SHARED** with business-operations-tracker and docuchat

| Resource | Type | SKU | Monthly Cost |
|----------|------|-----|-------------|
| ai-document-processor-db | SQL Database (on shared server) | Basic | ~$5 |
| doc-intel-dg | Cognitive Services (Form Recognizer) | F0 (Free) | $0 |

**Shared server:** rg-operations-tracker-server-2 (owned by business-operations-tracker)

## Recreation Steps

```powershell
# Prerequisites: rg-operations-tracker resource group and SQL server must exist
# (created by business-operations-tracker project)

# 1. Create SQL Database
az sql db create --server rg-operations-tracker-server-2 --resource-group rg-operations-tracker --name ai-document-processor-db --service-objective Basic

# 2. Create Form Recognizer (free tier)
az cognitiveservices account create --name doc-intel-dg --resource-group rg-operations-tracker --kind FormRecognizer --sku F0 --location eastus --yes

# 3. Add SQL firewall rule if needed
az sql server firewall-rule create --resource-group rg-operations-tracker --server rg-operations-tracker-server-2 --name AllowMyIP --start-ip-address <YOUR-IP> --end-ip-address <YOUR-IP>

# 4. Run SQL schema scripts from the repo
# 5. Update app configuration with connection strings
```
