# 10 PoCs a 10-Year .NET Developer Should Have Built (AI + Azure)

> Curated portfolio of **proof-of-concepts** that prove senior-level command of **.NET 8/9 + Azure + AI**. Each PoC lists what to build, the Azure/AI services involved, the .NET libraries, what it proves, and gotchas. Build them in a single GitHub org — one repo per PoC — and your portfolio speaks for itself.

---

## PoC 1 — Multi-Tenant SaaS Web API on App Service + Entra ID

**Build**: ASP.NET Core 9 Web API with row-level multi-tenancy (TenantId column + global query filter), Entra ID (multi-tenant app registration), per-tenant Key Vault references, deployed via slots.

**Stack**: ASP.NET Core, EF Core 9 (global query filters), Microsoft.Identity.Web, Azure App Service (Linux), Key Vault, SQL Database, Application Insights.

**What it proves**: AuthN/AuthZ at scale, tenant isolation, slots/blue-green, secret management, telemetry.

**Gotchas**: Token caching per tenant, connection string per tenant via Key Vault references, OpenAPI doc filtering per role.

---

## PoC 2 — Event-Driven Microservices on Container Apps + Service Bus + Dapr

**Build**: 3 microservices (Orders → Payments → Notifications) communicating via Service Bus topics; Dapr pub/sub + state store on Cosmos DB; KEDA scale-to-zero; saga via Durable Functions.

**Stack**: .NET 9 Minimal APIs, Azure Container Apps, Dapr, Service Bus, Cosmos DB, Durable Functions, OpenTelemetry → App Insights.

**What it proves**: Distributed systems patterns (saga, outbox, idempotency), eventual consistency, observability across services.

**Gotchas**: Idempotent message handlers, poison queues, distributed tracing context propagation.

---

## PoC 3 — RAG Chatbot over Private Docs (Azure OpenAI + AI Search)

**Build**: Upload PDFs/Docx to Blob → indexer extracts → chunks → embeddings (text-embedding-3-large) → Azure AI Search hybrid index (vector + BM25) → ASP.NET Core API uses Semantic Kernel to answer with citations and tool calling.

**Stack**: Azure OpenAI (gpt-4o + embeddings), Azure AI Search, Blob, Document Intelligence, **Semantic Kernel** or **Microsoft.Extensions.AI**, Blazor or React front end.

**What it proves**: End-to-end RAG, prompt engineering, grounding, citations, content safety filters, token cost control.

**Gotchas**: Chunk size/overlap tuning, re-ranker, query rewriting, prompt injection defenses, eval harness (groundedness/relevance).

---

## PoC 4 — Agentic Workflow with Tool Calling & Function Plugins

**Build**: An "IT helpdesk agent" that can reset AD passwords, open ServiceNow tickets, query SQL, and email users — orchestrated as a multi-step planner. Use **Semantic Kernel agents** or **AutoGen for .NET** with function calling + approval gate.

**Stack**: Azure OpenAI (gpt-4o), Semantic Kernel Agents, Azure Functions (tools), Logic Apps, Graph API, Cosmos DB (memory).

**What it proves**: Function/tool calling, agent orchestration, human-in-the-loop, durable memory, guardrails.

**Gotchas**: Cost runaway loops, tool error handling, deterministic system prompts, **approval workflow before destructive actions**.

---

## PoC 5 — Document Intelligence Pipeline (Forms / Invoices Extraction)

**Build**: Blob trigger Function — when invoice arrives, call Document Intelligence prebuilt-invoice model, validate against business rules, push to Cosmos DB, raise Event Grid event; failures route to human review queue.

**Stack**: Azure Functions (.NET 9 isolated), Document Intelligence, Event Grid, Cosmos DB, Service Bus DLQ, Logic Apps for review UI.

**What it proves**: Event-driven ingestion, OCR/IDP, schema validation, exception flows, DLQ patterns.

**Gotchas**: Confidence thresholds → human review, idempotency on retries, large file streaming.

---

## PoC 6 — Real-Time Speech & Translation App

**Build**: Blazor Server app that captures mic input → Azure Speech-to-Text (streaming) → Translator → Text-to-Speech in target language. Add speaker diarization for meeting transcripts saved to Cosmos DB.

**Stack**: Azure AI Speech SDK, Translator, SignalR, Blazor Server, Cosmos DB.

**What it proves**: Real-time streaming, WebSockets/SignalR, multimodal AI, latency-sensitive UX.

**Gotchas**: WebRTC vs. WebSocket, partial results UI, language auto-detection, cost per minute.

---

## PoC 7 — Computer Vision Quality Inspection (Custom Vision + Edge)

**Build**: Train a custom image classifier on defect samples in Azure AI Custom Vision; export model to ONNX; run on **.NET MAUI** or a **Windows Service** at the edge with ML.NET; results synced to IoT Hub.

**Stack**: Azure AI Vision / Custom Vision, ONNX Runtime, ML.NET, IoT Hub, Azure IoT Edge, .NET MAUI.

**What it proves**: Cloud-train / edge-infer, ML.NET, ONNX, IoT messaging, offline-first.

**Gotchas**: Model drift, edge deployment pipeline, hardware acceleration (DirectML), telemetry batching.

---

## PoC 8 — Secure Zero-Trust API with Managed Identity & Private Endpoints

**Build**: ASP.NET Core API on Container Apps in a VNet; **no secrets anywhere**. Managed Identity → Key Vault, SQL, Service Bus, Storage, Azure OpenAI. All PaaS over Private Endpoints; Front Door + WAF in front; Defender for Cloud + Microsoft Sentinel alerts.

**Stack**: Container Apps + VNet, Managed Identity, Key Vault, Private Endpoints, Front Door + WAF, Defender for Cloud, Sentinel, Bicep IaC.

**What it proves**: Zero-trust networking, IaC, defense in depth, compliance baseline.

**Gotchas**: DNS for private endpoints, egress control, MI role assignments, cold start with VNet integration.

---

## PoC 9 — Cost-Aware AI Gateway (Multi-Model Router + Caching + Quotas)

**Build**: A reverse-proxy ASP.NET Core API in front of Azure OpenAI / OpenAI / Anthropic that: routes by intent + cost, semantic-caches responses in Redis, enforces per-tenant token quotas, logs every call to App Insights with cost tagging, retries with exponential backoff and circuit breaker.

**Stack**: ASP.NET Core, **YARP** or Azure API Management (with AI Gateway policies), Azure Cache for Redis, Azure OpenAI, App Insights, **Polly**.

**What it proves**: Cross-cutting concerns for LLMs (cost, latency, reliability), API gateway patterns, FinOps.

**Gotchas**: Embedding-based semantic cache hits, streaming response forwarding, rate-limit headers, model fallback on 429.

---

## PoC 10 — Full DevOps: IaC + CI/CD + Observability for an AI App

**Build**: A reference repo deploying any of the above with **Bicep** modules + **GitHub Actions OIDC** to Azure; environments (dev/test/prod) via slots/revisions; load tests with Azure Load Testing; dashboards in Azure Monitor; automated rollback on SLO breach.

**Stack**: Bicep / Terraform, GitHub Actions (OIDC, environments, approvals), Azure Load Testing, Azure Monitor Workbooks, Action Groups, ADO/GitHub Issues.

**What it proves**: Senior-level DevOps, SRE thinking (SLI/SLO/error budget), reproducible infra, secret-less pipelines.

**Gotchas**: Bicep what-if before deploy, drift detection, secret rotation, **golden signals** dashboards.

---

## Bonus PoCs (Pick if Time Permits)

| # | PoC | Why |
|---|-----|-----|
| 11 | **Fine-tuning a small model** with Azure ML + LoRA on domain data | Shows you can go beyond prompt engineering |
| 12 | **Vector DB benchmark** — AI Search vs. Cosmos DB for NoSQL vs. PostgreSQL pgvector | Architecture decisioning |
| 13 | **Responsible AI** — content safety, PII redaction, bias eval, prompt-shield | Compliance/governance maturity |
| 14 | **Hybrid search with re-ranker** (Cohere Rerank / Azure AI Search semantic ranker) | Retrieval quality |
| 15 | **AKS-hosted .NET microservices** with Istio + KEDA + GitOps (Flux/Argo) | Heavy ops chops |
| 16 | **Power Platform + Custom Connector** wrapping your .NET API | Citizen-dev / enterprise reach |
| 17 | **Blazor + WebAssembly + WebGPU** running ONNX locally | Edge AI in the browser |
| 18 | **Durable Functions fan-out/fan-in** doing parallel LLM eval over 10k prompts | Long-running workflows |

---

## Suggested Build Order (90-day plan)

| Weeks | PoCs | Theme |
|---|---|---|
| 1–2 | PoC 1, PoC 10 | Strong foundation: API + DevOps |
| 3–4 | PoC 8 | Lock down security |
| 5–6 | PoC 5, PoC 6 | Cognitive Services breadth |
| 7–8 | PoC 3 | RAG (the headline) |
| 9–10 | PoC 4, PoC 9 | Agents + gateway |
| 11–12 | PoC 2, PoC 7 | Distributed + edge |

---

## What Each PoC Should Include in the Repo

1. **README.md** — architecture diagram (Mermaid or excalidraw), runbook, "what this proves"
2. **`infra/`** — Bicep / Terraform, idempotent
3. **`src/`** — clean .NET solution, layered or vertical-slice
4. **`tests/`** — unit + integration + (where relevant) load tests
5. **`.github/workflows/`** — OIDC CI/CD, lint, SAST (CodeQL), container scan
6. **`docs/`** — ADRs (Architecture Decision Records), cost estimate, eval results for AI PoCs
7. **Demo script** — 5-minute walkthrough recording
8. **Cleanup script** — `azd down` or `az group delete` to control cost

---

## Cross-Links

- Compute foundations → [../01_Compute/](../01_Compute/)
- Containers/ACR → [../02_Containers/](../02_Containers/)
- Storage & Cosmos → [../03_Storage_Data/](../03_Storage_Data/)
- Security/Identity → [../04_Security_Identity/](../04_Security_Identity/)
- AI fundamentals → [../06_AI_Fundamentals/](../06_AI_Fundamentals/)

---

## Mental Model

> **As a 10-year .NET dev in 2026, you should be able to demo: (1) a secure multi-tenant API, (2) event-driven microservices, (3) RAG, (4) an agent with tool calling, (5) document/speech/vision pipelines, (6) zero-trust networking, (7) an AI gateway with cost controls, and (8) end-to-end IaC + CI/CD. These 10 PoCs cover all of that — and each one is a portfolio piece a hiring manager can poke at.**
