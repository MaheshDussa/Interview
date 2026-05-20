# Introduction to AI-Powered Information Extraction

> **One-liner**: **Information Extraction (IE) = turn unstructured docs/text/audio into structured data** (fields, tables, entities, relationships) you can query and act on.

---

## 1. What is Information Extraction?

Input: PDFs, scans, emails, web pages, audio transcripts, images.
Output: structured records → DB, BI, workflow.

| Source | Want |
|---|---|
| Invoice PDF | `vendor`, `invoiceDate`, `total`, line items |
| Resume | `name`, `email`, `skills[]`, `years` |
| Contract | parties, clauses, dates, obligations |
| Medical note | conditions, meds, dosages |
| Form image | filled-in field values |
| Receipt photo | merchant, items, tax, total |

---

## 2. Azure Services in the IE Toolkit

| Service | Role |
|---|---|
| **Azure AI Document Intelligence** | Forms, receipts, invoices, IDs, contracts, custom forms, layout, tables |
| **Azure AI Language** | NER, PII, key phrases, custom classification/NER, summarization, health, QA |
| **Azure AI Vision (Read)** | Raw OCR for images |
| **Azure AI Search** | Indexing + skillsets (knowledge mining) + vector |
| **Azure AI Content Understanding** (newer) | Multimodal extraction across docs/images/audio/video |
| **Azure OpenAI (GPT-4o)** | Open-ended extraction via prompt or structured outputs |

---

## 3. Azure AI Document Intelligence (formerly Form Recognizer)

### Models
| Type | What |
|---|---|
| **Prebuilt** | Invoice, Receipt, Business Card, ID, W-2, 1098/1099, Health Insurance Card, US Tax forms, Marriage Cert, Mortgage |
| **Layout** | Text + tables + structure (no key/value semantics) |
| **General Document** | Generic key-value pairs |
| **Read** | OCR text + paragraphs + languages |
| **Custom (Neural)** | Train on your forms (structured / semi-structured / unstructured) |
| **Custom Classification** | Route docs to the right model |
| **Custom Composed** | Combine multiple custom models |

### Output (per analyzed doc)
- Pages → Lines / Words / Selection marks (checkboxes)
- Tables (cells with row/column span)
- Key-value pairs
- Documents (typed fields, e.g., `Items[].Description`, `Total`)
- Polygons (bounding regions) + confidences

---

## 4. .NET SDK Example — Document Intelligence

```csharp
// dotnet add package Azure.AI.DocumentIntelligence

var client = new DocumentIntelligenceClient(
    new Uri("https://<res>.cognitiveservices.azure.com/"),
    new DefaultAzureCredential());

var op = await client.AnalyzeDocumentAsync(
    WaitUntil.Completed,
    "prebuilt-invoice",
    new Uri("https://example.com/invoice.pdf"));

var result = op.Value;
foreach (var doc in result.Documents)
{
    if (doc.Fields.TryGetValue("VendorName", out var v))
        Console.WriteLine($"Vendor: {v.ValueString}");
    if (doc.Fields.TryGetValue("InvoiceTotal", out var t))
        Console.WriteLine($"Total: {t.ValueCurrency.Amount} {t.ValueCurrency.CurrencyCode}");
    if (doc.Fields.TryGetValue("Items", out var items))
        foreach (var item in items.ValueList)
            Console.WriteLine(item.ValueDictionary["Description"].ValueString);
}
```

---

## 5. Custom Models — Build Your Own

Pick the right flavor:

| Flavor | When |
|---|---|
| **Template** | Forms with fixed layout (same template every time) |
| **Neural** | Forms with variable layout, mixed structured + unstructured |
| **Generative (preview)** | LLM-backed extraction for completely unstructured docs |

### Training data
- 5+ samples per form type (label fields with the Document Intelligence Studio).
- Store in Blob (with SAS or MI access).
- Studio generates `.labels.json` + `.ocr.json` per file.

### Composed models
- Combine many custom models behind one endpoint.
- Optional **classifier** decides which sub-model handles each doc.

---

## 6. Document Intelligence Studio

Web UI to:
- Try prebuilt models on your files.
- Label & train custom models.
- Compose models.
- Test classification.
- Export project / JSON for code.

---

## 7. Azure AI Language — Text Extraction

| Feature | Output |
|---|---|
| **NER (general)** | Persons, Orgs, Locations, Quantities |
| **Custom NER** | Your domain entities |
| **PII detection** | SSN, email, phone, redacted version |
| **Key phrase extraction** | Topic phrases |
| **Entity linking** | Wikipedia URIs |
| **Text Analytics for Health** | Diagnoses, meds, dosages, relations |
| **Custom classification** | Single/multi-label |
| **Summarization** | Extractive / abstractive |
| **Custom Question Answering** | Q&A KB from docs |

Best paired with Document Intelligence — DI gives you the text, Language gives you the meaning.

---

## 8. Azure AI Search — Knowledge Mining

The pattern that ties it all together:

```
Blob/SharePoint/SQL
   → Indexer
   → Skillset (enrichment pipeline)
       • OCR
       • Language detection
       • Key phrases / NER / PII
       • Custom skill (Azure Function)
       • Embedding (vector)
   → Index (full-text + vector + facets)
   → Search API → app / RAG
```

**Built-in skills** include OCR, image analysis, language, entities, key phrases, PII, splitter, shaper, custom (Azure Function/Web API), Azure OpenAI embedding skill.

> Memory hint: **Indexer pulls** docs; **Skillset enriches**; **Index** is searchable; **App queries**.

---

## 9. Vector + Hybrid Search

- **Vector search** finds semantically similar text (embedding nearest neighbors).
- **Hybrid** = keyword (BM25) + vector + **semantic ranker** → best.
- Azure AI Search supports HNSW vectors, hybrid queries, semantic captions/answers.

```http
POST /indexes/docs/docs/search?api-version=2024-07-01
{
  "search": "renewal terms",
  "vectorQueries": [{ "kind": "vector", "vector": [...], "fields": "contentVector", "k": 50 }],
  "queryType": "semantic", "semanticConfiguration": "default",
  "top": 10
}
```

---

## 10. Extraction with Azure OpenAI (GPT-4o)

When data is fully unstructured or you need flexibility, ask the LLM directly with **Structured Outputs** / JSON schema.

```csharp
var schema = """
{ "type":"object",
  "properties": {
    "invoiceId": {"type":"string"},
    "total": {"type":"number"},
    "lineItems": { "type":"array", "items":{
       "type":"object",
       "properties":{ "description":{"type":"string"}, "qty":{"type":"number"}, "price":{"type":"number"} }}}
  },
  "required":["invoiceId","total"]
}
""";

var resp = await chat.CompleteChatAsync(
    [ new SystemChatMessage("Extract fields from the invoice."),
      new UserChatMessage(pdfText) ],
    new ChatCompletionOptions
    {
        ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
            "invoice", BinaryData.FromString(schema), strictSchemaEnabled: true)
    });
```

Combine: **Document Intelligence Read → text → GPT-4o structured → JSON**.

---

## 11. Azure AI Content Understanding (newer)

Multimodal IE engine: define a **schema** (fields you want), upload any modality (PDF, image, audio, video), get structured JSON. Hides choosing between Doc Intelligence vs Vision vs Speech vs OpenAI — runs the right pipeline.

---

## 12. End-to-End Patterns

### Invoice Automation
```
Email attachment → Logic App → Blob
   → Event Grid → Function
   → Document Intelligence (prebuilt-invoice)
   → Validation rules (PO match)
   → ERP API (Dynamics)
   → Cosmos DB audit
```

### Contract Analytics
```
Contracts PDFs → AI Search indexer
   → Skillset: OCR + Custom NER (parties, dates, clauses)
   → Index (vector + fields)
   → GPT-4o RAG chat for legal team
```

### Call Center Insights
```
Calls (WAV) → Speech batch transcription (+ diarization)
   → Language: sentiment, key phrases, PII redaction
   → Summarization (issue, resolution)
   → Dashboard
```

---

## 13. Confidence Scores & Human-in-the-Loop

Every extracted field carries a **confidence (0-1)**.

| Confidence | Action |
|---|---|
| ≥ 0.95 | Auto-process |
| 0.7-0.95 | Human review queue |
| < 0.7 | Reject / re-upload |

Use **Logic Apps** + Teams approval, or a simple web UI for verification.

---

## 14. Evaluation Metrics

| Task | Metric |
|---|---|
| Field extraction | **Field-level F1**, exact-match accuracy |
| OCR | CER (Character Error Rate), WER |
| Table extraction | Cell-level F1 |
| Classification | Accuracy / F1 |
| RAG retrieval | Recall@k, MRR, NDCG |
| End-to-end | Cost per doc, throughput, straight-through-processing rate (STP%) |

---

## 15. Security & Compliance

| Concern | Approach |
|---|---|
| Sensitive docs | Process within your tenant region; private endpoints |
| PII | Use **Language PII** to redact before storage / LLM call |
| Encryption | At rest (always); customer-managed keys optional |
| Audit | Log every extraction, store input + output + version |
| Access control | RBAC + Managed Identity to data sources |
| Data retention | Configure retention policies; don't keep raw docs forever |

---

## 16. Common Pitfalls

| Pitfall | Fix |
|---|---|
| Bad OCR quality | Higher DPI (≥ 300), deskew, denoise |
| Mixed doc types in one model | Use **classification** + per-type custom model |
| Trained on too few samples | Need 5-50+ per template; varied |
| Tables with merged cells lost | Use **Layout** or Neural Custom model |
| Hallucinated fields from LLM | Constrain with JSON Schema; ground on DI output |
| Multi-page docs split wrong | Use DI page splits or page-range params |
| Field positions shift | Use **Neural** (not Template) custom model |
| PII leaking into LLM | Pre-redact with Language PII before prompt |

---

## 17. Interview / Exam Q&A

**Q1. Document Intelligence vs OCR (Read)?**
OCR returns text + positions. Document Intelligence understands document structure: tables, key-value pairs, prebuilt schemas (invoice, ID, receipt).

**Q2. Prebuilt vs Custom model?**
Prebuilt = common doc types out of the box. Custom = your forms, you train with labeled samples.

**Q3. Template vs Neural custom model?**
Template = fixed layout, low-data, fast train. Neural = variable layout, more samples, higher accuracy on real-world forms.

**Q4. How to extract entities from free-text emails?**
**Azure AI Language NER** (general or **Custom NER** for domain).

**Q5. What's a skillset in Azure AI Search?**
A pipeline of cognitive skills run during indexing to enrich raw content (OCR, NER, embeddings) before storing in the index.

**Q6. Why combine vector + keyword + semantic ranker?**
Keyword finds exact terms, vector finds semantic neighbors, semantic ranker re-orders for relevance — best recall + precision.

**Q7. How do confidence scores help?**
Auto-route low-confidence extractions to humans (HITL); high-confidence go straight through.

**Q8. PII compliance during extraction?**
Run Language **PII detection** to redact before storing or sending to an LLM; encrypt at rest; restrict access via RBAC.

**Q9. When to use GPT-4o instead of Document Intelligence?**
Highly unstructured / variable docs where labeling templates is impractical; or to combine reasoning ("did this contract auto-renew?") with extraction.

**Q10. What does Composed Model do?**
Bundles many custom models under one endpoint, with a classifier choosing the right sub-model per document.

---

## 18. CLI / REST Cheat Sheet

```powershell
# Analyze invoice (prebuilt)
$endpoint = "https://<res>.cognitiveservices.azure.com"
$body = @{ urlSource = "https://example.com/invoice.pdf" } | ConvertTo-Json
$op = Invoke-WebRequest -Method Post `
  -Uri "$endpoint/documentintelligence/documentModels/prebuilt-invoice:analyze?api-version=2024-11-30" `
  -Headers @{ "Ocp-Apim-Subscription-Key" = $key; "Content-Type"="application/json" } `
  -Body $body
$location = $op.Headers.'Operation-Location'

# Poll
Invoke-RestMethod -Uri $location -Headers @{ "Ocp-Apim-Subscription-Key" = $key }
```

---

## 19. Mental Model

> **Information Extraction = OCR (read it) + Structure (parse fields/tables) + Semantics (NER/links) + Search (index it) + LLM (reason on it). Mix Document Intelligence + Language + AI Search + Azure OpenAI to go from raw documents to queryable, actionable data — with confidence scores, PII safety, and human-in-the-loop where it matters.**
