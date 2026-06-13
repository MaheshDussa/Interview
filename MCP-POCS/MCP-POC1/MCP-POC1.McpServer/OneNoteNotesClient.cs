using MCP_POC1.Shared;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace MCP_POC1.McpServer;

internal sealed class OneNoteNotesClient
{
    public static bool TryCreate(out OneNoteNotesClient? client, out string configurationMessage)
    {
        var accessToken = EnvironmentSettings.ReadFirst("ONENOTE_ACCESS_TOKEN", "MSGRAPH_ACCESS_TOKEN");
        var sectionId = EnvironmentSettings.ReadFirst("ONENOTE_SECTION_ID");

        if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(sectionId))
        {
            client = null;
            configurationMessage = "OneNote save is not configured. Set ONENOTE_ACCESS_TOKEN (or MSGRAPH_ACCESS_TOKEN) and ONENOTE_SECTION_ID to save notes automatically.";
            return false;
        }

        client = new OneNoteNotesClient(accessToken, sectionId);
        configurationMessage = string.Empty;
        return true;
    }

    private OneNoteNotesClient(string accessToken, string sectionId)
    {
        AccessToken = accessToken;
        SectionId = sectionId;
    }

    private string AccessToken { get; }

    private string SectionId { get; }

    public async Task<string> SaveNotesAsync(GeneratedNotes notes)
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);

        var endpoint = $"https://graph.microsoft.com/v1.0/me/onenote/sections/{SectionId}/pages";
        using var content = new StringContent(BuildOneNoteHtml(notes), Encoding.UTF8, "text/html");
        using var response = await client.PostAsync(endpoint, content);

        var responseBody = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"OneNote save failed with HTTP {(int)response.StatusCode}: {responseBody}");
        }

        return "Notes saved to OneNote successfully.";
    }

    public static GeneratedNotes GenerateNotes(string topic)
    {
        var normalizedTopic = NormalizeTopic(topic);

        if (normalizedTopic.Contains("azure app service") || normalizedTopic.Contains("app service") || normalizedTopic.Contains("app services"))
        {
            return BuildAzureAppServiceNotes();
        }

        if (normalizedTopic.Equals("iis") || normalizedTopic.Contains("internet information services"))
        {
            return BuildIisNotes();
        }

        if (normalizedTopic.Contains("azure devops pipeline") || normalizedTopic.Contains("azure pipelines") || normalizedTopic.Contains("devops pipeline"))
        {
            return BuildAzureDevOpsPipelineNotes();
        }

        if (normalizedTopic.Contains("dp-800") || normalizedTopic.Contains("administering sql") || normalizedTopic.Contains("sql azure") || normalizedTopic.Contains("azure sql"))
        {
            return BuildDp800Notes();
        }

        return BuildAdvancedGenericNotes(topic);
    }

    private static GeneratedNotes BuildAzureAppServiceNotes()
    {
        var markdown = """
# Technology Mastery Notes Template

## 1. Overview

### Service / Technology Name

Name: Azure App Service  
Category: Cloud / PaaS / Hosting

### Definition

> Azure App Service is a managed Azure platform service for hosting web applications, APIs, and background workloads without directly managing Windows or Linux virtual machines.

### Purpose

- It was created to let teams deploy web workloads quickly without spending time on OS patching, IIS or Nginx maintenance, or manual load balancer setup.
- It solves the business problem of slow delivery and high operational overhead for common web and API hosting scenarios.
- It should be used when the application mainly needs managed web hosting, strong integration with Azure services, and predictable operational patterns.

### Thumb Rule

> "Use this when you want managed web hosting with strong Azure integration and you do not need Kubernetes-level control."

### Real-World Example

- Streaming and media platforms use it for internal admin portals, partner APIs, and operational dashboards.
- Banking applications use it for customer portals, secure APIs, and internal approval workflows where identity, audit, and controlled deployment matter.
- E-commerce applications use it for storefront APIs, campaign microsites, order status services, and back-office web tools.

---

## 2. Business Need

### Problems Solved

| Problem | How This Technology Solves It |
| --- | --- |
| Slow application deployment | Provides managed runtime hosting with built-in deployment support, reducing infrastructure setup time. |
| High operations effort | Offloads OS patching, worker management, built-in TLS support, and platform maintenance to Azure. |
| Inconsistent release safety | Supports deployment slots, swap-based rollouts, and configuration isolation for safer production releases. |
| Complex Azure integration | Integrates directly with managed identity, Key Vault, VNet integration, Application Insights, and CI/CD tooling. |

### Benefits

#### Technical Benefits

- Faster web and API deployment with fewer infrastructure decisions.
- Strong integration with Azure identity, observability, and secret management.
- Built-in scaling, deployment slots, and diagnostics for production operations.

#### Business Benefits

- Cost reduction through reduced infrastructure management time.
- Scalability through managed scale up and scale out options.
- Reliability through platform-managed availability features and safer deployments.
- Faster development because teams focus on application logic instead of server administration.

---

## 3. Architecture

### High-Level Architecture

```text
User
  ↓
Azure Front Door / App Gateway / Public Endpoint
  ↓
Azure App Service
  ↓
Application Code / API Runtime
  ↓
Database / Cache / Messaging / External Services
```

### Detailed Request Flow

#### Step 1

The user sends an HTTP or HTTPS request through a public endpoint, gateway, or custom domain.

#### Step 2

Azure front-end infrastructure routes the request to the correct App Service application and worker instance.

#### Step 3

The application runtime processes the request, reads app settings, uses managed identity if needed, and calls downstream services.

#### Step 4

The response is returned to the user while logs, metrics, traces, and dependency telemetry are emitted to Application Insights and platform diagnostics.

---

## 4. Core Components

| Component | Purpose | Mandatory | Notes |
| --- | --- | --- | --- |
| App Service Plan | Defines compute size, OS, region, and scale boundary | Yes | Multiple apps can share one plan |
| App Service App | Hosts the actual web app or API | Yes | Main deployment unit |
| Deployment Slot | Supports safe rollout and swap-based releases | No | Strongly recommended for production |
| App Settings | Stores runtime configuration as environment variables | Yes | Slot settings can be sticky |
| Managed Identity | Securely accesses Azure resources without stored credentials | No | Recommended for production |
| VNet Integration | Provides outbound private access to internal services | No | Important for enterprise networking |
| Application Insights | Captures telemetry, traces, and failures | No | Recommended for observability |

### Component Deep Dive

### App Service Plan

#### What Is It?

The compute container that defines worker size, pricing tier, operating system, and scale characteristics.

#### Purpose

It provides the infrastructure boundary within which one or more apps run.

#### Key Features

- Scale up and scale out controls
- Shared hosting boundary for multiple apps

#### Real-Time Usage

Teams place related applications on the same plan when they want shared compute economics, or isolate critical apps on separate plans for performance and blast-radius reasons.

#### Limitations

Noisy-neighbor effects can appear when too many demanding apps share one plan.

#### Best Practices

Separate high-traffic or mission-critical apps from lower-priority workloads.

### Deployment Slot

#### What Is It?

A staging environment under the same app where a second version can be deployed and tested before swap.

#### Purpose

It reduces release risk and helps achieve low-downtime deployments.

#### Key Features

- Warm-up before swap
- Slot-specific configuration

#### Real-Time Usage

Used for blue-green style releases, smoke tests, and rollback-friendly production cutovers.

#### Limitations

Improper slot configuration can still cause broken swaps or production config leakage.

#### Best Practices

Mark secrets and environment-specific settings as slot settings.

### Managed Identity

#### What Is It?

An Azure identity assigned to the app so it can authenticate to Azure services without embedded passwords or keys.

#### Purpose

It removes secret sprawl and simplifies secure access to services like Key Vault, Storage, SQL, and Service Bus.

#### Key Features

- Secretless authentication
- Tight integration with Azure RBAC

#### Real-Time Usage

Used when the application reads secrets from Key Vault or calls protected Azure APIs at runtime.

#### Limitations

Authorization still has to be designed correctly; identity alone does not solve over-permissioning.

#### Best Practices

Grant least-privilege access and pair managed identity with Key Vault references when possible.

---

## 5. Features

### Basic Features

| Feature | Description |
| --- | --- |
| Managed hosting | Deploy web apps and APIs without server administration |
| Custom domains and TLS | Attach domain names and certificates for public access |
| Runtime support | Host .NET, Node.js, Java, Python, PHP, and containerized apps |

### Advanced Features

| Feature | Description |
| --- | --- |
| Deployment slots | Stage and swap releases safely |
| VNet integration | Connect privately to internal dependencies |
| Autoscale | Adjust capacity based on workload signals |

### Enterprise Features

| Feature | Description |
| --- | --- |
| Managed identity | Securely access Azure resources without stored credentials |
| Private endpoints | Restrict inbound access through private networking |
| Observability integration | Combine application telemetry with Azure monitoring and alerting |

---

## 6. Configuration Options

| Setting | Purpose | Recommended Value |
| --- | --- | --- |
| App Service Plan Tier | Controls compute, features, and SLA | Standard or Premium for production |
| Always On | Prevents app cold idling for long-running apps and APIs | Enabled for production APIs |
| Health Check Path | Gives the platform a reliable liveness signal | Set to a lightweight health endpoint |
| ARR Affinity | Controls sticky session behavior | Disable unless session affinity is required |
| Slot Setting Flag | Keeps environment-specific config from swapping | Enable for secrets and environment-only values |
| Managed Identity | Enables Azure resource authentication | Enable for production workloads |

---

## 7. Security

### Authentication

#### Supported Methods

- Microsoft Entra ID
- App-level authentication such as OpenID Connect or OAuth 2.0

#### Recommended Method

Use Microsoft Entra ID for enterprise identity and use managed identity for service-to-service Azure resource access.

---

### Authorization

#### Role-Based Access

| Role | Permissions |
| --- | --- |
| Application User | Accesses business functionality exposed by the app |
| Support Engineer | Views diagnostics and operational state |
| Platform Engineer | Manages app configuration, scaling, slots, and networking |
| Security Administrator | Reviews access patterns, secrets, certificates, and compliance controls |

---

### Security Best Practices

- Enable MFA for privileged Azure access.
- Use managed identity instead of stored secrets.
- Enable encryption in transit and use trusted certificate management.
- Follow least privilege for app identities, human access, and downstream resources.

---

## 8. Monitoring & Logging

### Key Metrics

| Metric | Why Important |
| --- | --- |
| CPU | Shows worker pressure and helps detect under-sized plans |
| Memory | Reveals leaks, pressure, and worker saturation |
| Response Time | Shows end-user experience and downstream latency impact |
| Error Rate | Indicates application or dependency failure patterns |

### Monitoring Tools

| Tool | Purpose |
| --- | --- |
| Application Insights | Captures requests, exceptions, dependencies, traces, and live telemetry |
| Azure Monitor | Aggregates metrics, dashboards, and alerts |
| App Service Diagnostics | Helps troubleshoot platform and configuration issues |

### Alerts To Configure

- High CPU
- High Memory
- Failed Requests
- Security Events

---

## 9. Performance & Scaling

### Scaling Options

#### Vertical Scaling

Move to a higher plan size when the workload needs more CPU, memory, or better underlying compute.

#### Horizontal Scaling

Add more worker instances when the application is stateless and traffic or concurrency is increasing.

---

### Performance Optimization Tips

- Keep the application stateless so scale-out works cleanly.
- Use caching and reduce synchronous dependency calls on the request path.
- Monitor dependency latency and connection behavior before increasing instance count blindly.

---

## 10. Pricing & Plans

### Available Plans

| Plan | Features | Suitable For |
| --- | --- | --- |
| Free | Shared compute, basic experimentation | Learning and very small demos |
| Basic | Dedicated compute, simple hosting | Small POCs and internal tools |
| Standard | Autoscale, slots, better production readiness | Most production web apps |
| Premium | Advanced scale, networking, and enterprise hosting features | Enterprise workloads and performance-critical apps |

### Plan Selection Guide

- Learning -> Free
- POC -> Basic
- Production -> Standard
- Enterprise -> Premium

---

## 11. Budget Planning

### Small Project

#### Requirements

Users: Few hundred daily users  
Traffic: Moderate API and web traffic  
Storage: Limited logs and application data outside the app tier

#### Estimated Cost

| Resource | Cost |
| --- | --- |
| App Service Plan | Use Azure Pricing Calculator for Basic or Standard tier in your region |
| Application Insights | Cost depends on telemetry volume |
| Key Vault / Networking | Usually modest unless enterprise networking is involved |

#### Total Monthly Cost

Estimate with Azure Pricing Calculator based on region, tier, traffic, and telemetry volume.

### Enterprise Project

#### Requirements

Users: Thousands to millions  
Traffic: High sustained traffic with peaks  
Storage: Significant telemetry, secure secrets, private networking, and multiple slots

#### Estimated Cost

| Resource | Cost |
| --- | --- |
| Premium App Service Plan | Major cost driver for enterprise hosting |
| Application Insights / Azure Monitor | Depends heavily on ingestion and retention |
| Networking and Security Add-ons | Private endpoints, gateways, WAF, and enterprise controls increase total cost |

#### Total Monthly Cost

Model with Azure Pricing Calculator and include environment count, observability retention, and network controls.

---

## 12. Advantages

| Advantage | Why Important |
| --- | --- |
| Managed platform | Reduces time spent on server maintenance |
| Fast deployment | Improves delivery speed for web and API workloads |
| Azure integration | Simplifies identity, secrets, monitoring, and networking patterns |
| Safer releases | Deployment slots reduce production risk |

---

## 13. Limitations

| Limitation | Impact |
| --- | --- |
| Less control than Kubernetes or VMs | Limits deep host customization |
| Shared-plan design risks | Bad plan design can create noisy-neighbor issues |
| Advanced networking complexity | Private networking and DNS still need careful architecture |
| Some workloads outgrow the platform model | Highly custom runtime needs may fit better elsewhere |

### Workarounds

| Limitation | Workaround |
| --- | --- |
| Less host control | Move to AKS, Container Apps, or VMs when control is the real requirement |
| Shared-plan pressure | Split critical workloads into separate App Service Plans |
| Networking complexity | Standardize DNS, route design, and pre-production connectivity validation |
| Release instability | Use slots, warm-up, smoke tests, and rollback plans |

---

## 14. Common Issues & Troubleshooting

### Issue 1

#### Symptoms

Users receive intermittent failures, latency spikes, or timeouts during peak usage.

#### Root Cause

The plan is under-sized, the app is stateful in a scale-out scenario, or downstream dependencies are the real bottleneck.

#### Resolution

Review CPU, memory, dependency latency, connection behavior, and scale rules before changing plan size or instance count.

### Issue 2

#### Symptoms

The app works in staging but fails after deployment or slot swap.

#### Root Cause

Slot settings were not isolated correctly, secret references differ by environment, or warm-up and health validation were incomplete.

#### Resolution

Use slot-specific settings, validate health before swap, and verify managed identity, secrets, DNS, and downstream connectivity per slot.

---

## 15. Best Practices

### Development

- Build stateless application behavior for easier scale-out.
- Externalize configuration and keep environment-specific settings out of code.

### Security

- Use managed identity and Key Vault references instead of embedded secrets.
- Restrict inbound and outbound access with least-privilege networking and access control.

### Performance

- Monitor dependencies before assuming the app tier is the bottleneck.
- Use health checks, caching, and async patterns where appropriate.

### Cost Optimization

- Avoid over-sizing plans for low-value workloads.
- Consolidate suitable low-risk apps, but isolate critical or noisy workloads.

---

## 16. Hands-On Exercises

### Beginner Lab

#### Objective

Deploy a basic web app to Azure App Service.

#### Steps

1. Create an App Service Plan and Web App.
2. Publish a sample web or API application.
3. Verify the endpoint and inspect basic logs.

### Intermediate Lab

#### Objective

Enable safe release practices and observability.

#### Steps

1. Add a deployment slot and configure slot-specific settings.
2. Enable Application Insights and a health check endpoint.
3. Deploy to staging, validate, and perform a slot swap.

### Advanced Lab

#### Objective

Design a production-style secure App Service deployment.

#### Steps

1. Configure managed identity, Key Vault integration, and VNet integration.
2. Add autoscale rules, alerts, and dependency monitoring.
3. Simulate a failure and walk through troubleshooting and rollback.

---

## 17. Interview Questions

### Basic

1. What is Azure App Service?
2. Why is it used?
3. What problem does it solve?

### Intermediate

1. Explain the App Service architecture and the role of the App Service Plan.
2. Explain how you would secure secrets and identities in App Service.
3. Explain how you would monitor and troubleshoot App Service in production.

### Advanced

1. How would you scale App Service for high traffic and private dependencies?
2. How would you optimize cost without harming reliability?
3. What are the major limitations of App Service and when would you choose another platform?

---

## 18. Real Project Scenarios

### Scenario 1

#### Requirement

Expose a secure internal API for enterprise employees with fast delivery.

#### Solution

Host the API on App Service, use Microsoft Entra ID for user authentication, managed identity for downstream Azure access, and deployment slots for safe releases.

#### Why This Technology?

It gives quick delivery, low platform operations effort, and strong Azure-native security integration.

### Scenario 2

#### Requirement

Run a customer-facing e-commerce API with traffic spikes during campaigns.

#### Solution

Use App Service with autoscale, Application Insights, deployment slots, and a fronting gateway or WAF pattern.

#### Why This Technology?

It balances delivery speed, platform management simplicity, and production readiness for common web workloads.

---

## 19. Exam Notes

### Must Remember

- The App Service Plan is the real compute and scale boundary.
- Deployment slots reduce release risk but require correct slot-specific settings.
- Managed identity plus Key Vault is the preferred secret and access pattern.

### Frequently Confused Topics

| Topic | Difference |
| --- | --- |
| App Service vs App Service Plan | The app is the workload; the plan is the compute boundary |
| Scale up vs Scale out | Scale up increases instance size; scale out increases instance count |
| VNet Integration vs Private Endpoint | VNet integration is mainly outbound private access; private endpoint secures inbound access |
| App Service vs AKS | App Service favors managed simplicity; AKS favors deeper container orchestration control |

---

## 20. Mastery Checklist

### Knowledge

- [x] Definition
- [x] Architecture
- [x] Components
- [x] Features
- [x] Security
- [x] Monitoring
- [x] Pricing

### Practical

- [x] Can Configure
- [x] Can Troubleshoot
- [x] Can Optimize
- [x] Can Secure

### Expert

- [x] Can Design Architecture
- [x] Can Estimate Cost
- [x] Can Conduct Reviews
- [x] Can Mentor Others
- [x] Can Handle Production Issues

### One-Line Revision

Azure App Service is managed web hosting in Azure: know why to use it, how the plan and app boundary work, how to secure and monitor it, how to scale it, what it costs, where it fails, and when another platform is the better choice.
""";

        return BuildGeneratedNotes("Azure App Service - Technology Mastery Notes", markdown);
    }

    private static GeneratedNotes BuildIisNotes()
    {
        return BuildGeneratedNotes(
            "IIS - Technology Mastery Notes",
            BuildGenericMasteryMarkdown(
                name: "IIS",
                category: "Backend / Hosting / Web Server",
                definition: "IIS is Microsoft's web server and application hosting platform for Windows-based web applications, reverse proxy scenarios, and enterprise-hosted APIs.",
                purpose: new[]
                {
                    "It was created to provide an integrated Windows web hosting stack with strong support for authentication, configuration, and server-managed web workloads.",
                    "It solves the need for stable enterprise hosting on Windows infrastructure, especially where Windows authentication and server integration matter.",
                    "Use it when the workload depends on Windows-based hosting patterns, IIS modules, or enterprise infrastructure already standardized on Windows Server."
                },
                thumbRule: "Use this when you need Windows-based web hosting, IIS-native features, or enterprise integration with existing Windows operations.",
                realWorldExamples: new[]
                {
                    "Banking portals use it for intranet applications secured with Windows authentication.",
                    "Enterprise APIs use it as a reverse proxy or host for ASP.NET and ASP.NET Core applications.",
                    "Legacy line-of-business applications use it when the runtime and operational model remain Windows-centered."
                },
                businessProblems: new[]
                {
                    ("Managed Windows hosting standardization", "Provides a consistent hosting model with application pools, modules, bindings, and Windows security integration."),
                    ("Need for integrated authentication", "Supports enterprise authentication patterns such as Windows Authentication and certificate-based access."),
                    ("Operational troubleshooting", "Offers logs, failed request tracing, and event-driven diagnostics for production support.")
                },
                architectureSummary: "User -> Load Balancer or DNS -> IIS Listener -> Application Pool / Worker Process -> Application Runtime -> Database or Downstream Services",
                coreComponents: new[]
                {
                    ("Site", "Endpoint and binding definition", "Yes", "Maps hostnames, ports, and app roots"),
                    ("Application Pool", "Isolation and worker process boundary", "Yes", "Critical for stability and permissions"),
                    ("Modules and Handlers", "Request pipeline behavior", "Yes", "Authentication, rewrite, compression, and execution hooks"),
                    ("ANCM", "ASP.NET Core integration", "No", "Important for ASP.NET Core behind IIS")
                }));
    }

    private static GeneratedNotes BuildAzureDevOpsPipelineNotes()
    {
        return BuildGeneratedNotes(
            "Azure DevOps Pipelines - Technology Mastery Notes",
            BuildGenericMasteryMarkdown(
                name: "Azure DevOps Pipelines",
                category: "DevOps / CI-CD / Delivery Automation",
                definition: "Azure DevOps Pipelines is a delivery automation service for building, testing, securing, packaging, and deploying applications through YAML or classic release workflows.",
                purpose: new[]
                {
                    "It was created to standardize and automate software delivery across environments.",
                    "It solves inconsistent releases, manual deployment risk, and poor delivery traceability.",
                    "Use it when you need repeatable CI/CD, gated promotion, build traceability, and integration with enterprise delivery controls."
                },
                thumbRule: "Use this when delivery quality, governance, and release automation must be treated as one engineering system.",
                realWorldExamples: new[]
                {
                    "Product teams use it to build, test, and package every pull request change.",
                    "Platform teams use it to promote artifacts across dev, test, and production with approvals.",
                    "Regulated industries use it for traceability, release evidence, and controlled deployment pipelines."
                },
                businessProblems: new[]
                {
                    ("Manual deployment risk", "Automates repeatable releases with validation, approvals, and rollback-aware design."),
                    ("Weak traceability", "Connects commits, builds, artifacts, tests, environments, and deployments."),
                    ("Inconsistent quality gates", "Enforces testing, code quality, security checks, and environment controls before promotion.")
                },
                architectureSummary: "Developer Commit -> Pipeline Trigger -> Build and Test -> Artifact Publish -> Environment Gates -> Deployment -> Validation and Monitoring",
                coreComponents: new[]
                {
                    ("Stages", "Promotion boundaries across environments", "Yes", "Critical for release design"),
                    ("Jobs and Steps", "Execution units inside a stage", "Yes", "Determine agent usage and task ordering"),
                    ("Agents", "Run pipeline workloads", "Yes", "Hosted or self-hosted affects security and tooling"),
                    ("Artifacts", "Immutable outputs used for deployment", "Yes", "Build once, promote many")
                }));
    }

        private static GeneratedNotes BuildDp800Notes()
        {
                var markdown = """
# Technology Mastery Notes Template

## 1. Overview

### Service / Technology Name

Name: DP-800  
Category: Database / Cloud / Administration / Migration

### Definition

> DP-800 is the Microsoft certification domain for administering and optimizing SQL Server database solutions in Azure, especially Azure SQL Database, Azure SQL Managed Instance, and SQL Server on Azure Virtual Machines.

### Purpose

- It was created to validate real-world skills for running SQL workloads in Azure across PaaS and IaaS models.
- It solves the business need for secure, performant, highly available, and cost-aware SQL platform operations in cloud environments.
- It should be used as a mastery topic when learning Azure SQL administration, migration strategy, backup and restore, high availability, performance tuning, and security.

### Thumb Rule

> "Use DP-800 when you need to understand how to operate, secure, migrate, optimize, and troubleshoot SQL workloads on Azure."

### Real-World Example

- Banking teams use DP-800 skills when moving SQL Server workloads to Azure while preserving compliance, backup, and recovery expectations.
- E-commerce platforms use DP-800 knowledge to tune Azure SQL performance under variable traffic and protect customer data.
- Enterprise modernization programs use DP-800 topics to decide between Azure SQL Database, Managed Instance, and SQL on Azure VM for migration scenarios.

---

## 2. Business Need

### Problems Solved

| Problem | How This Technology Solves It |
| --- | --- |
| Legacy SQL modernization | Helps choose the right Azure SQL target and migration path based on compatibility, cost, and operational needs. |
| Weak operational resilience | Covers backup, restore, high availability, disaster recovery, and continuity planning for SQL workloads. |
| Poor cloud SQL performance | Focuses on monitoring, tuning, indexing, workload analysis, and right-sizing in Azure. |
| Security and compliance risk | Teaches identity, encryption, auditing, vulnerability management, and least-privilege SQL administration. |

### Benefits

#### Technical Benefits

- Clear understanding of Azure SQL service models and operational trade-offs.
- Better migration design for SQL Server workloads moving into Azure.
- Stronger troubleshooting, tuning, and recovery readiness for production databases.

#### Business Benefits

- Cost reduction through better platform selection and right-sizing.
- Scalability through cloud-native SQL deployment models and performance tuning.
- Reliability through HA/DR, backup strategy, and incident response planning.
- Faster delivery through managed database operations and Azure-native automation.

---

## 3. Architecture

### High-Level Architecture

```text
Application
    ↓
Azure SQL Choice
    ↓
Azure SQL Database / Azure SQL Managed Instance / SQL Server on Azure VM
    ↓
Storage, Backups, Security, Monitoring
    ↓
Operations, HA/DR, Optimization
```

### Detailed Request Flow

#### Step 1

An application, service, or migration workload connects to the chosen Azure SQL platform using the appropriate connectivity, authentication, and network path.

#### Step 2

The database engine processes queries, applies security controls, uses configured performance settings, and interacts with storage and transaction logs.

#### Step 3

Azure services provide backups, metrics, alerts, auditing, and optional replication or failover capabilities depending on the chosen SQL model.

#### Step 4

Database administrators monitor workload behavior, tune performance, validate resilience, and respond to failures, drift, or scaling needs.

---

## 4. Core Components

| Component | Purpose | Mandatory | Notes |
| --- | --- | --- | --- |
| Azure SQL Database | Managed single database or elastic pool model | Yes | Best for modern PaaS-first designs |
| Azure SQL Managed Instance | Near-full SQL Server compatibility as PaaS | Yes | Strong choice for lift-and-shift with fewer app changes |
| SQL Server on Azure VM | Full SQL Server and OS control in Azure IaaS | Yes | Best when host-level control or feature parity is required |
| Migration Tooling | Moves schema and data into Azure targets | Yes | Includes Azure Database Migration Service and assessment tooling |
| HA/DR Design | Protects continuity under outages | Yes | Service-specific options differ significantly |
| Monitoring And Tuning | Keeps workloads stable and performant | Yes | Use Query Store, metrics, and advisor insights |

### Component Deep Dive

### Azure SQL Database

#### What Is It?

A fully managed relational database service in Azure for modern cloud applications.

#### Purpose

It minimizes operational overhead while providing built-in resilience, scaling options, and security features.

#### Key Features

- Automated backups and patching
- Built-in high availability and performance tiers

#### Real-Time Usage

Used for SaaS apps, APIs, and cloud-native workloads where app-level compatibility is already aligned with managed SQL behavior.

#### Limitations

Some SQL Server instance-level features are not available because it is a stricter PaaS model.

#### Best Practices

Choose the right service tier, enable Query Store, and design for PaaS operational boundaries early.

### Azure SQL Managed Instance

#### What Is It?

A managed SQL Server instance in Azure that offers broader engine compatibility than Azure SQL Database.

#### Purpose

It supports migrations from on-premises SQL Server with fewer application and feature changes.

#### Key Features

- Higher SQL Server compatibility
- Instance-level capabilities not available in single-database PaaS models

#### Real-Time Usage

Used in enterprise migrations where applications depend on SQL Agent, cross-database behavior, or instance-like administration patterns.

#### Limitations

Networking, provisioning, and cost can be more complex than Azure SQL Database.

#### Best Practices

Use it when compatibility needs are real and justify the operational and cost model.

### SQL Server on Azure Virtual Machine

#### What Is It?

Full SQL Server hosted on an Azure VM with complete operating system and engine-level control.

#### Purpose

It supports workloads needing maximum compatibility, custom server configuration, or host-level control.

#### Key Features

- Full SQL Server feature parity
- Full control over OS, storage, patching strategy, and instance behavior

#### Real-Time Usage

Used for hard-to-modernize workloads, specialized SQL features, and scenarios where PaaS limitations are unacceptable.

#### Limitations

It carries the highest administration burden because patching, backup design, performance tuning, and OS concerns remain with the team.

#### Best Practices

Choose it only when the control requirement is genuine and worth the added operational cost.

---

## 5. Features

### Basic Features

| Feature | Description |
| --- | --- |
| Cloud SQL hosting | Runs relational workloads on Azure |
| Backup and restore | Supports business continuity and recovery operations |
| Security controls | Uses authentication, encryption, and access governance |

### Advanced Features

| Feature | Description |
| --- | --- |
| Query performance tuning | Uses Query Store, indexing, and workload analysis |
| Migration assessment | Evaluates compatibility and migration readiness |
| HA/DR strategy | Uses failover groups, geo-replication, or IaaS-based designs depending on platform |

### Enterprise Features

| Feature | Description |
| --- | --- |
| Managed identity and Azure integration | Improves secure access and automation |
| Auditing and threat protection | Supports governance and security review |
| Platform selection trade-off analysis | Aligns workload needs to the correct Azure SQL deployment model |

---

## 6. Configuration Options

| Setting | Purpose | Recommended Value |
| --- | --- | --- |
| Compute Tier | Balances performance and cost | Choose by workload profile, not default size |
| Storage Size | Supports capacity and IO behavior | Size for growth and maintenance windows |
| Backup Retention | Controls recovery window | Align with business recovery requirements |
| Query Store | Captures workload performance history | Enabled for tuning and troubleshooting |
| Authentication Mode | Controls SQL and Azure identity access | Prefer Microsoft Entra ID where practical |
| Geo-Redundancy / Failover | Supports resilience design | Enable according to RPO and RTO requirements |

---

## 7. Security

### Authentication

#### Supported Methods

- SQL authentication
- Microsoft Entra ID authentication

#### Recommended Method

Use Microsoft Entra ID for centralized identity control when possible, and minimize traditional SQL logins.

---

### Authorization

#### Role-Based Access

| Role | Permissions |
| --- | --- |
| Application Account | Executes only required data operations |
| DBA / Platform Engineer | Manages performance, backup, restore, and configuration |
| Security Reviewer | Reviews auditing, access, and vulnerability posture |
| Operations Support | Monitors health and responds to incidents with controlled access |

---

### Security Best Practices

- Enable MFA for privileged Azure access.
- Use Microsoft Entra ID and managed identity where supported.
- Enable encryption at rest and in transit.
- Follow least privilege for database principals, application identities, and admin access.

---

## 8. Monitoring & Logging

### Key Metrics

| Metric | Why Important |
| --- | --- |
| CPU | Indicates compute pressure and under-sized service tiers |
| Memory | Helps identify workload stress and cache pressure where relevant |
| DTU / vCore / IO Usage | Shows whether the database tier matches the workload |
| Query Duration / Waits | Reveals SQL bottlenecks, blocking, and poor execution plans |

### Monitoring Tools

| Tool | Purpose |
| --- | --- |
| Azure Monitor | Captures platform metrics and alerting |
| Query Store | Tracks query performance history and regressions |
| SQL Insights / DMVs | Helps analyze engine behavior and bottlenecks |

### Alerts To Configure

- High CPU or high compute consumption
- Storage or IO pressure
- Failed logins or unusual security events
- Replication, failover, or backup failures

---

## 9. Performance & Scaling

### Scaling Options

#### Vertical Scaling

Increase compute tier, service objective, or VM size when the workload needs more processing or memory per instance.

#### Horizontal Scaling

Use read scale-out, replicas, workload splitting, or architectural partitioning where the platform supports it.

---

### Performance Optimization Tips

- Use Query Store and execution plan analysis before changing service size.
- Tune indexing, statistics, and query patterns based on observed workload behavior.
- Separate application issues from database engine bottlenecks before scaling blindly.

---

## 10. Pricing & Plans

### Available Plans

| Plan | Features | Suitable For |
| --- | --- | --- |
| General Purpose | Balanced compute and cost | Most business workloads |
| Business Critical | Higher IO and low-latency workloads | Performance-sensitive production databases |
| Hyperscale | Large-scale storage and elasticity | Very large databases |
| Azure VM-based SQL | Full control with IaaS pricing model | Specialized compatibility scenarios |

### Plan Selection Guide

- Learning -> Azure SQL Database basic or entry-level lab setup
- POC -> General Purpose tiers or smaller SQL VM sizes
- Production -> General Purpose or Business Critical based on workload behavior
- Enterprise -> Managed Instance, Business Critical, Hyperscale, or tuned SQL on VM depending on compatibility and scale

---

## 11. Budget Planning

### Small Project

#### Requirements

Users: Small internal or departmental workload  
Traffic: Moderate transactional usage  
Storage: Limited database size with manageable backup overhead

#### Estimated Cost

| Resource | Cost |
| --- | --- |
| Azure SQL Database / small SQL target | Use Azure Pricing Calculator for selected tier and region |
| Monitoring | Depends on log retention and alerting needs |
| Backup / geo features | Additional cost if extended resilience is needed |

#### Total Monthly Cost

Estimate with Azure Pricing Calculator using service tier, backup retention, region, and expected usage.

### Enterprise Project

#### Requirements

Users: Large business or external customer base  
Traffic: High throughput and latency-sensitive workload  
Storage: Large databases, long retention, HA/DR, and security operations requirements

#### Estimated Cost

| Resource | Cost |
| --- | --- |
| Production SQL tier or Managed Instance | Major cost driver |
| Monitoring and diagnostics | Depends on data retention and analysis requirements |
| HA/DR and security controls | Geo-replication, failover, auditing, and advanced protections increase cost |

#### Total Monthly Cost

Model total cost with workload size, resilience design, retention policy, region strategy, and environment count.

---

## 12. Advantages

| Advantage | Why Important |
| --- | --- |
| Strong Azure SQL platform choices | Lets teams match workloads to the correct control model |
| Better migration decision-making | Reduces failed or inefficient cloud database moves |
| Production-ready operational focus | Improves support for backup, tuning, security, and resilience |
| Exam and real-world overlap | Useful for both certification and production database administration |

---

## 13. Limitations

| Limitation | Impact |
| --- | --- |
| Service-model confusion | Wrong platform choice can increase cost or block required features |
| Migration complexity | Legacy SQL workloads may need compatibility assessment and redesign |
| Performance tuning mistakes | Poor indexing or query design can waste cloud spend |
| HA/DR assumptions | Azure SQL resilience options vary by service model and must be understood explicitly |

### Workarounds

| Limitation | Workaround |
| --- | --- |
| Wrong platform choice | Compare Azure SQL Database, Managed Instance, and SQL on Azure VM against real feature requirements |
| Migration risk | Use assessment tooling, test cutovers, and validate compatibility early |
| Performance instability | Use Query Store, workload baselines, and targeted tuning before scaling |
| Weak resilience design | Define RPO, RTO, backup, restore, and failover expectations up front |

---

## 14. Common Issues & Troubleshooting

### Issue 1

#### Symptoms

Database performance degrades under load, with rising latency, blocking, or resource saturation.

#### Root Cause

The workload is using the wrong service tier, has poor indexing, regressed execution plans, or inefficient application query patterns.

#### Resolution

Check Query Store, execution plans, waits, indexes, service tier fit, and recent deployment changes before simply scaling up.

### Issue 2

#### Symptoms

Migration to Azure completes, but application behavior or SQL jobs fail unexpectedly afterward.

#### Root Cause

The target service model does not fully match the original SQL Server features, dependencies, or operational assumptions.

#### Resolution

Review compatibility gaps, agent requirements, networking assumptions, authentication model, and unsupported instance-level dependencies.

---

## 15. Best Practices

### Development

- Design applications with clear SQL dependency patterns and avoid unnecessary chatty database access.
- Validate schema, compatibility, and migration assumptions before production cutover.

### Security

- Prefer Microsoft Entra ID and minimize static SQL credentials.
- Enable auditing, encryption, vulnerability assessment, and least-privilege access controls.

### Performance

- Baseline workload behavior and tune with Query Store, indexing, and plan analysis.
- Match service tier and architecture choice to actual workload characteristics.

### Cost Optimization

- Avoid over-sizing compute tiers without evidence.
- Choose the simplest Azure SQL model that still meets feature and compatibility requirements.

---

## 16. Hands-On Exercises

### Beginner Lab

#### Objective

Deploy and connect to an Azure SQL Database instance.

#### Steps

1. Create an Azure SQL logical server and database.
2. Configure firewall or private access and connect securely.
3. Run a basic workload and review metrics.

### Intermediate Lab

#### Objective

Compare Azure SQL Database and Managed Instance for migration readiness.

#### Steps

1. Review workload requirements and feature dependencies.
2. Assess compatibility and choose the likely Azure SQL target.
3. Validate backup, monitoring, and security configuration for the chosen platform.

### Advanced Lab

#### Objective

Design a production-style Azure SQL administration strategy for migration, resilience, and tuning.

#### Steps

1. Select the right Azure SQL target for a real migration scenario.
2. Define HA/DR, monitoring, backup retention, and security controls.
3. Simulate a performance or failover issue and document the recovery approach.

---

## 17. Interview Questions

### Basic

1. What is DP-800?
2. What is the difference between Azure SQL Database, Managed Instance, and SQL Server on Azure VM?
3. What business problem does DP-800 knowledge help solve?

### Intermediate

1. How do you choose between Azure SQL Database and Managed Instance?
2. How would you secure Azure SQL workloads in production?
3. How would you monitor and tune SQL performance in Azure?

### Advanced

1. How would you design HA/DR for an enterprise SQL workload in Azure?
2. How would you optimize cost without harming performance or resilience?
3. How would you manage migration risk for a legacy SQL Server estate?

---

## 18. Real Project Scenarios

### Scenario 1

#### Requirement

Migrate an on-premises SQL Server application with minimal application change and preserve SQL compatibility.

#### Solution

Assess dependencies, compare Managed Instance versus SQL on Azure VM, validate unsupported features, and design migration plus rollback steps.

#### Why This Technology?

DP-800 topics directly cover platform selection, compatibility trade-offs, migration planning, and operational readiness.

### Scenario 2

#### Requirement

Run a customer-facing Azure-hosted transactional database with strict uptime, performance, and security requirements.

#### Solution

Choose the appropriate Azure SQL model, configure backup and resilience, tune workload behavior, enable auditing, and monitor query performance continuously.

#### Why This Technology?

DP-800 knowledge combines database administration, cloud operations, security, and performance engineering in one operating model.

---

## 19. Exam Notes

### Must Remember

- Azure SQL Database, Managed Instance, and SQL on Azure VM exist for different control and compatibility needs.
- Migration decisions must consider feature parity, administration model, networking, and operational ownership.
- Query Store, security controls, backup strategy, and HA/DR design are core mastery areas.

### Frequently Confused Topics

| Topic | Difference |
| --- | --- |
| Azure SQL Database vs Managed Instance | Database-level PaaS simplicity versus broader SQL Server compatibility |
| Managed Instance vs SQL on Azure VM | Managed PaaS operations versus full host and engine control |
| Backup availability vs HA/DR strategy | Backup supports recovery; HA/DR supports continuity targets like RPO and RTO |
| Scaling vs tuning | Scaling adds capacity; tuning reduces waste and improves execution behavior |

---

## 20. Mastery Checklist

### Knowledge

- [x] Definition
- [x] Architecture
- [x] Components
- [x] Features
- [x] Security
- [x] Monitoring
- [x] Pricing

### Practical

- [x] Can Configure
- [x] Can Troubleshoot
- [x] Can Optimize
- [x] Can Secure

### Expert

- [x] Can Design Architecture
- [x] Can Estimate Cost
- [x] Can Conduct Reviews
- [x] Can Mentor Others
- [x] Can Handle Production Issues

### One-Line Revision

DP-800 is about choosing the right Azure SQL platform, migrating safely, securing data, tuning performance, designing HA/DR, and operating SQL workloads reliably in Azure.
""";

                return BuildGeneratedNotes("DP-800 - Technology Mastery Notes", markdown);
        }

    private static GeneratedNotes BuildAdvancedGenericNotes(string topic)
    {
        var safeTopic = string.IsNullOrWhiteSpace(topic) ? "Technology" : topic.Trim();

        return BuildGeneratedNotes(
            $"{safeTopic} - Technology Mastery Notes",
            BuildGenericMasteryMarkdown(
                name: safeTopic,
                category: "Architecture / Platform / Application Technology",
                definition: $"{safeTopic} is a technology or service that should be understood by its architecture role, operational ownership, business value, and production trade-offs.",
                purpose: new[]
                {
                    $"It exists to solve a specific engineering or business problem in the delivery, runtime, data, integration, or platform layer.",
                    $"It should be evaluated based on business need, scalability, security, cost, and operational complexity rather than feature count alone.",
                    $"Use it when it fits the required architecture boundary, delivery model, and operational ownership pattern."
                },
                thumbRule: $"Use this when {safeTopic} is the simplest technology that still meets the architectural, security, and operational requirements.",
                realWorldExamples: new[]
                {
                    $"Enterprise teams use {safeTopic} when they need controlled, repeatable behavior under production constraints.",
                    $"Platform teams use {safeTopic} when standardization and operational clarity matter as much as raw features.",
                    $"Architecture reviews use {safeTopic} as a decision point only when its trade-offs are understood clearly."
                },
                businessProblems: new[]
                {
                    ("Architecture mismatch", $"It helps when the technology is chosen to fit the actual runtime and operational need instead of forcing a poor pattern."),
                    ("Operational instability", $"It should support safer deployment, observability, security, and production support practices."),
                    ("Scaling uncertainty", $"It should provide a known scaling and constraint model so growth can be handled intentionally.")
                },
                architectureSummary: "User or System Trigger -> Application or Service Layer -> Technology Runtime Boundary -> Data or Dependency Layer -> Monitoring and Operations"));
    }

    private static string NormalizeTopic(string topic)
    {
        return string.IsNullOrWhiteSpace(topic)
            ? string.Empty
            : topic.Trim().ToLowerInvariant();
    }

    private static string BuildGenericMasteryMarkdown(
        string name,
        string category,
        string definition,
        IEnumerable<string> purpose,
        string thumbRule,
        IEnumerable<string> realWorldExamples,
        IEnumerable<(string Problem, string Solution)> businessProblems,
        string architectureSummary,
        IEnumerable<(string Component, string Purpose, string Mandatory, string Notes)>? coreComponents = null)
    {
        var builder = new StringBuilder();
        var purposeList = purpose.ToList();
        var examplesList = realWorldExamples.ToList();
        var problemsList = businessProblems.ToList();
        var componentsList = coreComponents?.ToList() ?? new List<(string Component, string Purpose, string Mandatory, string Notes)>
        {
            ("Core runtime", "Executes the main workload", "Yes", "Understand the runtime boundary"),
            ("Configuration", "Controls behavior across environments", "Yes", "Keep environment-specific values externalized"),
            ("Security model", "Protects access, identity, and secrets", "Yes", "Design for least privilege"),
            ("Observability", "Provides logs, metrics, traces, and diagnostics", "Yes", "Critical for production operation")
        };

        builder.AppendLine("# Technology Mastery Notes Template");
        builder.AppendLine();
        builder.AppendLine("## 1. Overview");
        builder.AppendLine();
        builder.AppendLine("### Service / Technology Name");
        builder.AppendLine();
        builder.AppendLine($"Name: {name}  ");
        builder.AppendLine($"Category: {category}");
        builder.AppendLine();
        builder.AppendLine("### Definition");
        builder.AppendLine();
        builder.AppendLine($"> {definition}");
        builder.AppendLine();
        builder.AppendLine("### Purpose");
        builder.AppendLine();
        foreach (var item in purposeList)
        {
            builder.AppendLine($"- {item}");
        }

        builder.AppendLine();
        builder.AppendLine("### Thumb Rule");
        builder.AppendLine();
        builder.AppendLine($"> \"{thumbRule}\"");
        builder.AppendLine();
        builder.AppendLine("### Real-World Example");
        builder.AppendLine();
        foreach (var item in examplesList)
        {
            builder.AppendLine($"- {item}");
        }

        builder.AppendLine();
        builder.AppendLine("---");
        builder.AppendLine();
        builder.AppendLine("## 2. Business Need");
        builder.AppendLine();
        builder.AppendLine("### Problems Solved");
        builder.AppendLine();
        builder.AppendLine("| Problem | How This Technology Solves It |");
        builder.AppendLine("| --- | --- |");
        foreach (var item in problemsList)
        {
            builder.AppendLine($"| {item.Problem} | {item.Solution} |");
        }

        builder.AppendLine();
        builder.AppendLine("### Benefits");
        builder.AppendLine();
        builder.AppendLine("#### Technical Benefits");
        builder.AppendLine();
        builder.AppendLine("- Better architectural clarity and runtime understanding");
        builder.AppendLine("- Stronger operational and troubleshooting readiness");
        builder.AppendLine("- Improved security, observability, and scaling design decisions");
        builder.AppendLine();
        builder.AppendLine("#### Business Benefits");
        builder.AppendLine();
        builder.AppendLine("- Cost reduction through better technology fit and fewer operational mistakes");
        builder.AppendLine("- Scalability through known growth and dependency patterns");
        builder.AppendLine("- Reliability through safer production practices");
        builder.AppendLine("- Faster development through clearer platform decisions");
        builder.AppendLine();
        builder.AppendLine("---");
        builder.AppendLine();
        builder.AppendLine("## 3. Architecture");
        builder.AppendLine();
        builder.AppendLine("### High-Level Architecture");
        builder.AppendLine();
        builder.AppendLine("```text");
        builder.AppendLine(architectureSummary.Replace(" -> ", Environment.NewLine + "  ↓" + Environment.NewLine));
        builder.AppendLine("```");
        builder.AppendLine();
        builder.AppendLine("### Detailed Request Flow");
        builder.AppendLine();
        builder.AppendLine("#### Step 1");
        builder.AppendLine();
        builder.AppendLine($"A request, event, or processing action enters the {name} boundary through its normal entry point.");
        builder.AppendLine();
        builder.AppendLine("#### Step 2");
        builder.AppendLine();
        builder.AppendLine($"The technology applies its runtime, configuration, identity, and routing behavior to process the workload.");
        builder.AppendLine();
        builder.AppendLine("#### Step 3");
        builder.AppendLine();
        builder.AppendLine("The workload interacts with dependencies such as databases, APIs, messaging systems, or infrastructure services.");
        builder.AppendLine();
        builder.AppendLine("#### Step 4");
        builder.AppendLine();
        builder.AppendLine("The response or result is returned while monitoring, logs, traces, and operational signals are captured.");
        builder.AppendLine();
        builder.AppendLine("---");
        builder.AppendLine();
        builder.AppendLine("## 4. Core Components");
        builder.AppendLine();
        builder.AppendLine("| Component | Purpose | Mandatory | Notes |");
        builder.AppendLine("| --- | --- | --- | --- |");
        foreach (var item in componentsList)
        {
            builder.AppendLine($"| {item.Component} | {item.Purpose} | {item.Mandatory} | {item.Notes} |");
        }

        builder.AppendLine();
        builder.AppendLine("### Component Deep Dive");
        builder.AppendLine();
        builder.AppendLine($"### {componentsList[0].Component}");
        builder.AppendLine();
        builder.AppendLine("#### What Is It?");
        builder.AppendLine();
        builder.AppendLine($"It is a key part of the {name} runtime or control model.");
        builder.AppendLine();
        builder.AppendLine("#### Purpose");
        builder.AppendLine();
        builder.AppendLine(componentsList[0].Purpose);
        builder.AppendLine();
        builder.AppendLine("#### Key Features");
        builder.AppendLine();
        builder.AppendLine("- Controls an important runtime or operational behavior");
        builder.AppendLine("- Directly affects supportability, security, or scale");
        builder.AppendLine();
        builder.AppendLine("#### Real-Time Usage");
        builder.AppendLine();
        builder.AppendLine($"Teams rely on it when operating {name} in production, especially during deployment, scale, or incident scenarios.");
        builder.AppendLine();
        builder.AppendLine("#### Limitations");
        builder.AppendLine();
        builder.AppendLine("Misconfiguration usually leads to instability, security risk, or poor operational clarity.");
        builder.AppendLine();
        builder.AppendLine("#### Best Practices");
        builder.AppendLine();
        builder.AppendLine("Validate the behavior in non-production and document the operational expectations clearly.");
        builder.AppendLine();
        builder.AppendLine("---");
        builder.AppendLine();
        builder.AppendLine("## 5. Features");
        builder.AppendLine();
        builder.AppendLine("### Basic Features");
        builder.AppendLine();
        builder.AppendLine("| Feature | Description |");
        builder.AppendLine("| --- | --- |");
        builder.AppendLine("| Core runtime support | Delivers the main business or technical capability of the technology |");
        builder.AppendLine("| Configuration support | Allows environment-specific behavior changes |");
        builder.AppendLine("| Operational diagnostics | Supports visibility and troubleshooting |");
        builder.AppendLine();
        builder.AppendLine("### Advanced Features");
        builder.AppendLine();
        builder.AppendLine("| Feature | Description |");
        builder.AppendLine("| --- | --- |");
        builder.AppendLine("| Integration patterns | Connects safely to dependencies and platform services |");
        builder.AppendLine("| Scaling controls | Supports predictable growth under load |");
        builder.AppendLine("| Release safety features | Improves deployment confidence and rollback capability |");
        builder.AppendLine();
        builder.AppendLine("### Enterprise Features");
        builder.AppendLine();
        builder.AppendLine("| Feature | Description |");
        builder.AppendLine("| --- | --- |");
        builder.AppendLine("| Identity and access controls | Supports enterprise-grade authentication and authorization |");
        builder.AppendLine("| Monitoring integration | Connects to production operations and support workflows |");
        builder.AppendLine("| Governance readiness | Supports repeatable control, review, and compliance patterns |");
        builder.AppendLine();
        builder.AppendLine("---");
        builder.AppendLine();
        builder.AppendLine("## 6. Configuration Options");
        builder.AppendLine();
        builder.AppendLine("| Setting | Purpose | Recommended Value |");
        builder.AppendLine("| --- | --- | --- |");
        builder.AppendLine("| Environment-specific configuration | Controls runtime behavior | Store outside code and review per environment |");
        builder.AppendLine("| Identity configuration | Controls access to dependencies | Use least-privilege identity or service connection |");
        builder.AppendLine("| Monitoring settings | Controls support visibility | Enable by default in production |");
        builder.AppendLine();
        builder.AppendLine("---");
        builder.AppendLine();
        builder.AppendLine("## 7. Security");
        builder.AppendLine();
        builder.AppendLine("### Authentication");
        builder.AppendLine();
        builder.AppendLine("#### Supported Methods");
        builder.AppendLine();
        builder.AppendLine("- Platform-native identity");
        builder.AppendLine("- Application-level authentication and token-based access");
        builder.AppendLine();
        builder.AppendLine("#### Recommended Method");
        builder.AppendLine();
        builder.AppendLine("Use the strongest enterprise identity model available and avoid embedded secrets or broad shared credentials.");
        builder.AppendLine();
        builder.AppendLine("---");
        builder.AppendLine();
        builder.AppendLine("### Authorization");
        builder.AppendLine();
        builder.AppendLine("#### Role-Based Access");
        builder.AppendLine();
        builder.AppendLine("| Role | Permissions |");
        builder.AppendLine("| --- | --- |");
        builder.AppendLine("| User | Accesses approved business functionality |");
        builder.AppendLine("| Support Engineer | Views diagnostics and runtime state |");
        builder.AppendLine("| Platform Engineer | Manages configuration, scaling, and deployment behavior |");
        builder.AppendLine("| Security Reviewer | Audits access, secrets, and compliance-relevant controls |");
        builder.AppendLine();
        builder.AppendLine("---");
        builder.AppendLine();
        builder.AppendLine("### Security Best Practices");
        builder.AppendLine();
        builder.AppendLine("- Enable MFA for privileged access.");
        builder.AppendLine("- Use managed identity or the platform equivalent when available.");
        builder.AppendLine("- Enable encryption in transit and at rest where supported.");
        builder.AppendLine("- Follow least privilege for users, services, and automation.");
        builder.AppendLine();
        builder.AppendLine("---");
        builder.AppendLine();
        builder.AppendLine("## 8. Monitoring & Logging");
        builder.AppendLine();
        builder.AppendLine("### Key Metrics");
        builder.AppendLine();
        builder.AppendLine("| Metric | Why Important |");
        builder.AppendLine("| --- | --- |");
        builder.AppendLine("| CPU | Reveals compute pressure or resource mismatch |");
        builder.AppendLine("| Memory | Helps identify leaks, pressure, or under-sizing |");
        builder.AppendLine("| Response Time | Shows customer and dependency latency impact |");
        builder.AppendLine("| Error Rate | Highlights failure trends and deployment risk |");
        builder.AppendLine();
        builder.AppendLine("### Monitoring Tools");
        builder.AppendLine();
        builder.AppendLine("| Tool | Purpose |");
        builder.AppendLine("| --- | --- |");
        builder.AppendLine("| Platform monitoring | Captures infrastructure and service health |");
        builder.AppendLine("| Application telemetry | Captures code-level requests, traces, and failures |");
        builder.AppendLine("| Alerting and dashboards | Supports operations and incident response |");
        builder.AppendLine();
        builder.AppendLine("### Alerts To Configure");
        builder.AppendLine();
        builder.AppendLine("- High CPU");
        builder.AppendLine("- High Memory");
        builder.AppendLine("- Failed Requests");
        builder.AppendLine("- Security Events");
        builder.AppendLine();
        builder.AppendLine("---");
        builder.AppendLine();
        builder.AppendLine("## 9. Performance & Scaling");
        builder.AppendLine();
        builder.AppendLine("### Scaling Options");
        builder.AppendLine();
        builder.AppendLine("#### Vertical Scaling");
        builder.AppendLine();
        builder.AppendLine("Increase the resource size when the workload needs more per-instance capacity.");
        builder.AppendLine();
        builder.AppendLine("#### Horizontal Scaling");
        builder.AppendLine();
        builder.AppendLine("Increase the instance count when the workload can be spread safely across multiple execution units.");
        builder.AppendLine();
        builder.AppendLine("---");
        builder.AppendLine();
        builder.AppendLine("### Performance Optimization Tips");
        builder.AppendLine();
        builder.AppendLine("- Measure before tuning and identify the true bottleneck first.");
        builder.AppendLine("- Reduce synchronous dependency cost on critical paths.");
        builder.AppendLine("- Align scale design with application state and concurrency behavior.");
        builder.AppendLine();
        builder.AppendLine("---");
        builder.AppendLine();
        builder.AppendLine("## 10. Pricing & Plans");
        builder.AppendLine();
        builder.AppendLine("### Available Plans");
        builder.AppendLine();
        builder.AppendLine("| Plan | Features | Suitable For |");
        builder.AppendLine("| --- | --- | --- |");
        builder.AppendLine("| Free | Learning or lightweight experimentation | Learning |");
        builder.AppendLine("| Basic | Small dedicated workloads | POC or internal use |");
        builder.AppendLine("| Standard | Stronger production capabilities | Production |");
        builder.AppendLine("| Premium | Advanced scale and enterprise controls | Enterprise workloads |");
        builder.AppendLine();
        builder.AppendLine("### Plan Selection Guide");
        builder.AppendLine();
        builder.AppendLine("- Learning -> Free");
        builder.AppendLine("- POC -> Basic");
        builder.AppendLine("- Production -> Standard");
        builder.AppendLine("- Enterprise -> Premium");
        builder.AppendLine();
        builder.AppendLine("---");
        builder.AppendLine();
        builder.AppendLine("## 11. Budget Planning");
        builder.AppendLine();
        builder.AppendLine("### Small Project");
        builder.AppendLine();
        builder.AppendLine("#### Requirements");
        builder.AppendLine();
        builder.AppendLine("Users: Small team or limited user group  ");
        builder.AppendLine("Traffic: Low to moderate  ");
        builder.AppendLine("Storage: Minimal to moderate");
        builder.AppendLine();
        builder.AppendLine("#### Estimated Cost");
        builder.AppendLine();
        builder.AppendLine("| Resource | Cost |");
        builder.AppendLine("| --- | --- |");
        builder.AppendLine("| Core service | Use the vendor pricing calculator for the selected plan and region |");
        builder.AppendLine("| Monitoring | Depends on ingestion, retention, and alerting volume |");
        builder.AppendLine("| Security or networking add-ons | Add if required by the architecture |");
        builder.AppendLine();
        builder.AppendLine("#### Total Monthly Cost");
        builder.AppendLine();
        builder.AppendLine("Estimate with the official pricing calculator using realistic traffic and environment assumptions.");
        builder.AppendLine();
        builder.AppendLine("### Enterprise Project");
        builder.AppendLine();
        builder.AppendLine("#### Requirements");
        builder.AppendLine();
        builder.AppendLine("Users: Large internal or external user base  ");
        builder.AppendLine("Traffic: High and variable  ");
        builder.AppendLine("Storage: Significant observability, data, or integration overhead");
        builder.AppendLine();
        builder.AppendLine("#### Estimated Cost");
        builder.AppendLine();
        builder.AppendLine("| Resource | Cost |");
        builder.AppendLine("| --- | --- |");
        builder.AppendLine("| Core production plan | Major cost driver at enterprise scale |");
        builder.AppendLine("| Monitoring and diagnostics | Depends on retention and telemetry ingestion |");
        builder.AppendLine("| Security, networking, and HA features | Often material in enterprise design |");
        builder.AppendLine();
        builder.AppendLine("#### Total Monthly Cost");
        builder.AppendLine();
        builder.AppendLine("Model cost with environment count, scale assumptions, dependency traffic, and observability retention.");
        builder.AppendLine();
        builder.AppendLine("---");
        builder.AppendLine();
        builder.AppendLine("## 12. Advantages");
        builder.AppendLine();
        builder.AppendLine("| Advantage | Why Important |");
        builder.AppendLine("| --- | --- |");
        builder.AppendLine("| Clear operational model | Reduces ambiguity in production support |");
        builder.AppendLine("| Scalable architecture path | Helps the system grow intentionally |");
        builder.AppendLine("| Stronger governance alignment | Supports review, security, and controlled change |");
        builder.AppendLine();
        builder.AppendLine("---");
        builder.AppendLine();
        builder.AppendLine("## 13. Limitations");
        builder.AppendLine();
        builder.AppendLine("| Limitation | Impact |");
        builder.AppendLine("| --- | --- |");
        builder.AppendLine("| Wrong use case fit | Increases cost, complexity, or operational pain |");
        builder.AppendLine("| Configuration mistakes | Can create security or stability issues |");
        builder.AppendLine("| Scaling assumptions | Can fail under real-world traffic or dependency pressure |");
        builder.AppendLine();
        builder.AppendLine("### Workarounds");
        builder.AppendLine();
        builder.AppendLine("| Limitation | Workaround |");
        builder.AppendLine("| --- | --- |");
        builder.AppendLine("| Wrong platform fit | Re-evaluate competing architecture choices early |");
        builder.AppendLine("| Configuration risk | Standardize templates, reviews, and validation gates |");
        builder.AppendLine("| Scale uncertainty | Load test and monitor dependencies before production growth |");
        builder.AppendLine();
        builder.AppendLine("---");
        builder.AppendLine();
        builder.AppendLine("## 14. Common Issues & Troubleshooting");
        builder.AppendLine();
        builder.AppendLine("### Issue 1");
        builder.AppendLine();
        builder.AppendLine("#### Symptoms");
        builder.AppendLine();
        builder.AppendLine("The workload slows down, fails intermittently, or behaves differently across environments.");
        builder.AppendLine();
        builder.AppendLine("#### Root Cause");
        builder.AppendLine();
        builder.AppendLine("Configuration drift, missing permissions, poor dependency behavior, or wrong scale assumptions.");
        builder.AppendLine();
        builder.AppendLine("#### Resolution");
        builder.AppendLine();
        builder.AppendLine("Check configuration, identity, runtime metrics, dependency telemetry, and recent deployment changes first.");
        builder.AppendLine();
        builder.AppendLine("### Issue 2");
        builder.AppendLine();
        builder.AppendLine("#### Symptoms");
        builder.AppendLine();
        builder.AppendLine("A release succeeds technically but the system is unhealthy or partially broken after change.");
        builder.AppendLine();
        builder.AppendLine("#### Root Cause");
        builder.AppendLine();
        builder.AppendLine("Validation gaps, environment mismatch, hidden dependency assumptions, or incomplete rollback planning.");
        builder.AppendLine();
        builder.AppendLine("#### Resolution");
        builder.AppendLine();
        builder.AppendLine("Add stronger pre-release validation, post-release checks, operational runbooks, and rollback safety.");
        builder.AppendLine();
        builder.AppendLine("---");
        builder.AppendLine();
        builder.AppendLine("## 15. Best Practices");
        builder.AppendLine();
        builder.AppendLine("### Development");
        builder.AppendLine();
        builder.AppendLine("- Keep environment-specific configuration out of source code.");
        builder.AppendLine("- Design with operational support and failure handling in mind.");
        builder.AppendLine();
        builder.AppendLine("### Security");
        builder.AppendLine();
        builder.AppendLine("- Minimize secrets and use identity-driven access where possible.");
        builder.AppendLine("- Review permissions, network exposure, and auditability regularly.");
        builder.AppendLine();
        builder.AppendLine("### Performance");
        builder.AppendLine();
        builder.AppendLine("- Profile the real bottleneck before scaling.");
        builder.AppendLine("- Tune the dependency path, not just the compute layer.");
        builder.AppendLine();
        builder.AppendLine("### Cost Optimization");
        builder.AppendLine();
        builder.AppendLine("- Match plan size to real traffic and business criticality.");
        builder.AppendLine("- Remove waste in idle environments, over-retained telemetry, and over-provisioned capacity.");
        builder.AppendLine();
        builder.AppendLine("---");
        builder.AppendLine();
        builder.AppendLine("## 16. Hands-On Exercises");
        builder.AppendLine();
        builder.AppendLine("### Beginner Lab");
        builder.AppendLine();
        builder.AppendLine("#### Objective");
        builder.AppendLine();
        builder.AppendLine($"Understand the basic setup and runtime behavior of {name}.");
        builder.AppendLine();
        builder.AppendLine("#### Steps");
        builder.AppendLine();
        builder.AppendLine("1. Create or provision the basic environment.");
        builder.AppendLine("2. Configure a simple working scenario.");
        builder.AppendLine("3. Validate the result and review logs or telemetry.");
        builder.AppendLine();
        builder.AppendLine("### Intermediate Lab");
        builder.AppendLine();
        builder.AppendLine("#### Objective");
        builder.AppendLine();
        builder.AppendLine($"Add security, monitoring, and controlled change practices to {name}.");
        builder.AppendLine();
        builder.AppendLine("#### Steps");
        builder.AppendLine();
        builder.AppendLine("1. Enable security and identity controls.");
        builder.AppendLine("2. Add observability and health validation.");
        builder.AppendLine("3. Test an operational or release scenario.");
        builder.AppendLine();
        builder.AppendLine("### Advanced Lab");
        builder.AppendLine();
        builder.AppendLine("#### Objective");
        builder.AppendLine();
        builder.AppendLine($"Operate {name} with production-style architecture, failure handling, and optimization decisions.");
        builder.AppendLine();
        builder.AppendLine("#### Steps");
        builder.AppendLine();
        builder.AppendLine("1. Add enterprise-grade identity, scaling, and resilience design.");
        builder.AppendLine("2. Simulate a failure or load problem.");
        builder.AppendLine("3. Troubleshoot, optimize, and document the production response.");
        builder.AppendLine();
        builder.AppendLine("---");
        builder.AppendLine();
        builder.AppendLine("## 17. Interview Questions");
        builder.AppendLine();
        builder.AppendLine("### Basic");
        builder.AppendLine();
        builder.AppendLine($"1. What is {name}?");
        builder.AppendLine("2. Why is it used?");
        builder.AppendLine("3. What problem does it solve?");
        builder.AppendLine();
        builder.AppendLine("### Intermediate");
        builder.AppendLine();
        builder.AppendLine("1. Explain the architecture.");
        builder.AppendLine("2. Explain the security design.");
        builder.AppendLine("3. Explain the monitoring approach.");
        builder.AppendLine();
        builder.AppendLine("### Advanced");
        builder.AppendLine();
        builder.AppendLine("1. How would you scale it?");
        builder.AppendLine("2. How would you optimize cost?");
        builder.AppendLine("3. What are its limitations?");
        builder.AppendLine();
        builder.AppendLine("---");
        builder.AppendLine();
        builder.AppendLine("## 18. Real Project Scenarios");
        builder.AppendLine();
        builder.AppendLine("### Scenario 1");
        builder.AppendLine();
        builder.AppendLine("#### Requirement");
        builder.AppendLine();
        builder.AppendLine("Deliver a business workload safely with clear operational ownership.");
        builder.AppendLine();
        builder.AppendLine("#### Solution");
        builder.AppendLine();
        builder.AppendLine($"Design {name} with proper identity, observability, scaling, and deployment controls.");
        builder.AppendLine();
        builder.AppendLine("#### Why This Technology?");
        builder.AppendLine();
        builder.AppendLine("Because it fits the required architecture boundary and operational model better than more complex or weaker alternatives.");
        builder.AppendLine();
        builder.AppendLine("### Scenario 2");
        builder.AppendLine();
        builder.AppendLine("#### Requirement");
        builder.AppendLine();
        builder.AppendLine("Support growth, reliability, and reviewability under production conditions.");
        builder.AppendLine();
        builder.AppendLine("#### Solution");
        builder.AppendLine();
        builder.AppendLine("Use controlled deployment, monitoring, security, and cost-aware scaling practices.");
        builder.AppendLine();
        builder.AppendLine("#### Why This Technology?");
        builder.AppendLine();
        builder.AppendLine($"Because {name} can satisfy the delivery and operational need when used with the correct boundaries and practices.");
        builder.AppendLine();
        builder.AppendLine("---");
        builder.AppendLine();
        builder.AppendLine("## 19. Exam Notes");
        builder.AppendLine();
        builder.AppendLine("### Must Remember");
        builder.AppendLine();
        builder.AppendLine("- Understand the architecture boundary first.");
        builder.AppendLine("- Know the core components and what they control.");
        builder.AppendLine("- Explain security, monitoring, scaling, and failure handling together.");
        builder.AppendLine();
        builder.AppendLine("### Frequently Confused Topics");
        builder.AppendLine();
        builder.AppendLine("| Topic | Difference |");
        builder.AppendLine("| --- | --- |");
        builder.AppendLine("| Feature list vs architecture role | Features describe capability; architecture role explains when and why the technology should exist |");
        builder.AppendLine("| Scaling vs performance tuning | Scaling adds capacity; tuning removes waste or bottlenecks |");
        builder.AppendLine("| Security vs compliance | Security protects the system; compliance proves required controls and evidence exist |");
        builder.AppendLine();
        builder.AppendLine("---");
        builder.AppendLine();
        builder.AppendLine("## 20. Mastery Checklist");
        builder.AppendLine();
        builder.AppendLine("### Knowledge");
        builder.AppendLine();
        builder.AppendLine("- [x] Definition");
        builder.AppendLine("- [x] Architecture");
        builder.AppendLine("- [x] Components");
        builder.AppendLine("- [x] Features");
        builder.AppendLine("- [x] Security");
        builder.AppendLine("- [x] Monitoring");
        builder.AppendLine("- [x] Pricing");
        builder.AppendLine();
        builder.AppendLine("### Practical");
        builder.AppendLine();
        builder.AppendLine("- [x] Can Configure");
        builder.AppendLine("- [x] Can Troubleshoot");
        builder.AppendLine("- [x] Can Optimize");
        builder.AppendLine("- [x] Can Secure");
        builder.AppendLine();
        builder.AppendLine("### Expert");
        builder.AppendLine();
        builder.AppendLine("- [x] Can Design Architecture");
        builder.AppendLine("- [x] Can Estimate Cost");
        builder.AppendLine("- [x] Can Conduct Reviews");
        builder.AppendLine("- [x] Can Mentor Others");
        builder.AppendLine("- [x] Can Handle Production Issues");
        builder.AppendLine();
        builder.AppendLine("### One-Line Revision");
        builder.AppendLine();
        builder.AppendLine($"{name}: what it is, why it exists, core components, request flow, security, monitoring, scaling, pricing, limitations, troubleshooting, best practices, and real-world usage.");

        return builder.ToString();
    }

    private static string BuildOneNoteHtml(GeneratedNotes notes)
    {
        return string.Concat(
            "<!DOCTYPE html><html><head><title>",
            HtmlEncode(notes.Title),
            "</title><meta name=\"created\" content=\"",
            DateTime.UtcNow.ToString("s"),
            "Z\" /></head><body><pre>",
            HtmlEncode(notes.MarkdownContent),
            "</pre></body></html>");
    }

    public static string BuildPreviewText(GeneratedNotes notes)
    {
        return notes.PreviewText;
    }

    private static GeneratedNotes BuildGeneratedNotes(string title, string markdown)
    {
        var normalized = markdown.Trim() + Environment.NewLine;
        return new GeneratedNotes(title, normalized, normalized);
    }

    private static string HtmlEncode(string value) => WebUtility.HtmlEncode(value);

    internal sealed record GeneratedNotes(
        string Title,
        string MarkdownContent,
        string PreviewText);
}
