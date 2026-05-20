# Virtual Machines & Installing IIS — Complete Guide

> A clean, beginner-to-pro walkthrough of **what a VM is, how it differs from containers, when to use one, how to create one in Azure, and how to install + configure IIS to host an ASP.NET Core app on it.**

---

# PART 1 — Virtual Machines

## 1. What is a Virtual Machine?

A **Virtual Machine (VM)** is a software-emulated computer running on top of physical hardware. It has its own:
- CPU & RAM (carved out of the host)
- Disk (a virtual hard disk file)
- Network adapter
- **Full guest operating system** (Windows Server, Ubuntu, etc.)

> **Analogy:** A VM is like **renting an apartment in a building** — your own walls, kitchen, bedroom, locked door. A container is like **renting a room in a shared apartment**.

The software that runs VMs is called a **hypervisor** (Hyper-V, VMware ESXi, KVM, Xen, VirtualBox).

---

## 2. Why VMs exist (the problems they solve)

| Problem | VM fix |
|---|---|
| One physical server underused (5% CPU) | Run 10 VMs on it → much better utilization |
| Need different OSes on one machine | Run Windows + Linux + macOS side-by-side |
| Hardware failure breaks the app | Move the VM to another host (live migration) |
| Need to test risky changes | Snapshot → break things → revert |
| Strong isolation between apps | Each VM has its own kernel — full security boundary |
| Legacy app needs Windows Server 2012 | Keep it in a VM forever |

---

## 3. VM vs Container — when to pick which?

| | Virtual Machine | Container |
|---|---|---|
| Isolation | Full OS (separate kernel) | Process-level (shared kernel) |
| Size | GBs | MBs |
| Boot time | Minutes | Milliseconds |
| Density per host | 10s | 100s–1000s |
| Best for | Different OS, legacy apps, full IT stacks | Microservices, modern web/API apps |
| Patch model | Patch each VM's OS | Rebuild image |
| Tools | Hyper-V, VMware, Azure VM | Docker, Kubernetes |

**Rule of thumb:**
- **VM** = a whole computer (you manage the OS).
- **Container** = an application (you manage the app).

Use VMs for: legacy software, full server stacks (AD, SQL, file servers), apps that need a specific OS, strong isolation, dev workstations.

---

## 4. Hypervisor types

| Type | Where it runs | Examples |
|---|---|---|
| **Type 1 (bare metal)** | Directly on hardware | Hyper-V Server, VMware ESXi, KVM, Xen, Azure host |
| **Type 2 (hosted)** | On top of a normal OS | VirtualBox, VMware Workstation, Hyper-V on Windows 10/11 Pro |

Azure VMs run on a Type 1 hypervisor managed by Microsoft.

---

## 5. Core VM concepts (vocabulary)

| Term | Meaning |
|---|---|
| **Host** | Physical machine running the hypervisor |
| **Guest** | The VM (and its OS) running on the host |
| **vCPU** | Virtual CPU core assigned to a VM |
| **VHD / VHDX / VMDK** | Virtual disk file formats |
| **Snapshot / Checkpoint** | Point-in-time copy you can revert to |
| **Image** | Template used to create new VMs (OS + tools pre-installed) |
| **NIC** | Virtual network interface |
| **Availability Set / Zone** | Spread VMs across racks/datacenters for HA |
| **Scale Set (VMSS)** | Group of identical VMs that auto-scale |
| **Managed Disk** | Azure-managed VHD (you don't pick storage accounts) |
| **Boot diagnostics** | Console screenshot + serial console for troubleshooting |

---

## 6. Common Azure VM SKUs (sizes)

| Family | Use case | Example |
|---|---|---|
| **B-series** | Burstable, cheap, dev/test | B2s (2 vCPU, 4 GB) |
| **D-series** | General-purpose production | D4s_v5 (4 vCPU, 16 GB) |
| **E-series** | Memory-heavy (SQL, in-mem cache) | E8s_v5 (8 vCPU, 64 GB) |
| **F-series** | CPU-heavy (batch, compute) | F8s_v2 |
| **L-series** | Storage-optimized (NoSQL, big-data) | L8s_v3 |
| **N-series** | GPU (ML, rendering) | NC6s_v3 |
| **M-series** | Massive memory (SAP HANA) | M128ms |

> Suffix decoder: `s` = premium storage, `v5` = generation, `ms` = more memory.

---

## 7. Creating an Azure VM — Azure CLI

```powershell
# 1. Resource group
az group create -n rg-demo -l eastus

# 2. Create a Windows Server VM (with public IP and default NSG)
az vm create `
  -g rg-demo `
  -n vm-web-01 `
  --image Win2022AzureEditionCore `
  --size Standard_B2s `
  --admin-username azureuser `
  --admin-password 'P@ssw0rd!ChangeMe' `
  --public-ip-sku Standard

# 3. Open ports (RDP + HTTP + HTTPS)
az vm open-port -g rg-demo -n vm-web-01 --port 3389 --priority 1000
az vm open-port -g rg-demo -n vm-web-01 --port 80   --priority 1010
az vm open-port -g rg-demo -n vm-web-01 --port 443  --priority 1020

# 4. Get the public IP
az vm show -g rg-demo -n vm-web-01 -d --query publicIps -o tsv
```

For Linux: swap `--image Win2022AzureEditionCore` for `--image Ubuntu2204` and use `--ssh-key-values ~/.ssh/id_rsa.pub`.

---

## 8. Connecting to an Azure VM

### Windows VM — via RDP
```powershell
mstsc /v:<public-ip>
```
- Username: `azureuser` (or what you set)
- Password: the admin password
- Better: **Azure Bastion** (browser-based RDP/SSH, no public IP needed)

### Linux VM — via SSH
```powershell
ssh azureuser@<public-ip>
```

---

## 9. Day-2 VM operations

```powershell
# Start / Stop / Restart
az vm start    -g rg-demo -n vm-web-01
az vm stop     -g rg-demo -n vm-web-01       # still billed (powered off in OS)
az vm deallocate -g rg-demo -n vm-web-01     # stops billing for compute
az vm restart  -g rg-demo -n vm-web-01

# Resize (shutdown not always needed)
az vm resize -g rg-demo -n vm-web-01 --size Standard_D4s_v5

# Add a data disk
az vm disk attach -g rg-demo --vm-name vm-web-01 `
  --name datadisk1 --new --size-gb 128 --sku Premium_LRS

# Snapshot the OS disk
az snapshot create -g rg-demo -n vm-web-01-snap `
  --source $(az vm show -g rg-demo -n vm-web-01 --query storageProfile.osDisk.managedDisk.id -o tsv)

# Delete
az vm delete -g rg-demo -n vm-web-01 --yes
```

> **Stop vs Deallocate:** "Stop" in the OS leaves you billed for compute. **Deallocate** (via portal/CLI) releases the host — that's the one that stops the bill.

---

## 10. VM availability & scaling

| Concept | What it does |
|---|---|
| **Availability Set** | Spreads VMs across physical racks → ~99.95% SLA |
| **Availability Zone** | Spreads across datacenters in a region → ~99.99% SLA |
| **VM Scale Set (VMSS)** | Auto-scales identical VMs based on metrics |
| **Azure Load Balancer / App Gateway** | Front the VMs/VMSS for traffic distribution |
| **Backup (Recovery Services Vault)** | Scheduled point-in-time recovery |
| **Site Recovery (ASR)** | DR replication to another region |

---

## 11. VM security checklist

- ✅ **Don't expose RDP/SSH to the internet** — use Azure Bastion or JIT (Just-In-Time) access.
- ✅ **NSG**: allow only required ports from required sources.
- ✅ **Managed Identity** for the VM to access Azure resources (no passwords).
- ✅ **Microsoft Defender for Servers** for threat detection.
- ✅ **Update Management** / Azure Update Manager for patching.
- ✅ **Disk encryption** (Azure Disk Encryption / SSE with CMK).
- ✅ **Diagnostic logs** + Log Analytics agent.
- ✅ Strong admin password / SSH keys; rotate via Key Vault.

---

## 12. Cost-saving tips

- **Deallocate** VMs when not in use (don't just shut them down).
- **Auto-shutdown** schedule for dev/test (`az vm auto-shutdown`).
- **B-series** for low-baseline workloads.
- **Reserved Instances / Savings Plan** for 24×7 prod (up to 70% off).
- **Spot VMs** for fault-tolerant batch (huge discount, can be evicted).
- **Right-size** with Azure Advisor recommendations.

---

# PART 2 — Installing IIS on a Windows VM

**IIS (Internet Information Services)** is the built-in Windows web server. It hosts:
- Classic ASP.NET (Framework) apps
- ASP.NET Core apps (as a reverse proxy to Kestrel via the ANCM module)
- Static sites, PHP, FastCGI apps

---

## 13. Install IIS — Windows Server (GUI)

1. Open **Server Manager** → **Manage** → **Add Roles and Features**.
2. Choose **Role-based or feature-based installation**.
3. Select your server → click **Next**.
4. Tick **Web Server (IIS)** → click **Next**.
5. (Features step) leave defaults → **Next**.
6. On **Role Services**, add as needed:
   - ✅ Common HTTP Features (Default Document, HTTP Errors, Static Content)
   - ✅ Health and Diagnostics (Logging, Request Monitor)
   - ✅ Performance (Static Content Compression)
   - ✅ Security (Request Filtering)
   - ✅ Application Development → **ASP.NET 4.x**, **.NET Extensibility**, **ISAPI**
   - ✅ Management Tools → IIS Management Console
7. Click **Install** → wait → **Close**.
8. Browse to `http://localhost` — you'll see the IIS welcome page.

---

## 14. Install IIS — PowerShell (fast, scriptable, recommended)

```powershell
# Windows Server: install IIS + common features
Install-WindowsFeature -Name `
  Web-Server,                  # core IIS
  Web-Common-Http,             # static content, default doc, errors
  Web-Mgmt-Console,            # IIS Manager UI
  Web-Http-Logging,
  Web-Stat-Compression,
  Web-Dyn-Compression,
  Web-Filtering,               # request filtering
  Web-Asp-Net45,               # if you still need classic ASP.NET
  Web-Net-Ext45,
  Web-ISAPI-Ext,
  Web-ISAPI-Filter `
  -IncludeManagementTools

# Verify
Get-Service W3SVC                       # World Wide Web Publishing Service
Invoke-WebRequest http://localhost      # should be 200 OK
```

### Windows 10/11 (client, dev box)
```powershell
# Run elevated
Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServerRole         -All
Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServer             -All
Enable-WindowsOptionalFeature -Online -FeatureName IIS-ManagementConsole     -All
Enable-WindowsOptionalFeature -Online -FeatureName IIS-ASPNET45              -All
Enable-WindowsOptionalFeature -Online -FeatureName IIS-NetFxExtensibility45  -All
Enable-WindowsOptionalFeature -Online -FeatureName IIS-ISAPIExtensions       -All
Enable-WindowsOptionalFeature -Online -FeatureName IIS-ISAPIFilter           -All
```

---

## 15. Verify the install

- Open browser → `http://localhost` → IIS default page.
- Open **IIS Manager** (`inetmgr` from Run).
- The **Default Web Site** is bound to port 80.

---

## 16. Open the firewall

```powershell
# Windows Firewall — allow inbound HTTP/HTTPS
New-NetFirewallRule -DisplayName "HTTP 80"  -Direction Inbound -Protocol TCP -LocalPort 80  -Action Allow
New-NetFirewallRule -DisplayName "HTTPS 443"-Direction Inbound -Protocol TCP -LocalPort 443 -Action Allow
```

On Azure, **also open the NSG** (`az vm open-port` shown earlier). Both layers must allow the port.

---

## 17. Host an **ASP.NET Core** app on IIS

ASP.NET Core does **not** run inside IIS. IIS acts as a **reverse proxy** to **Kestrel** (the in-process app server) using the **ASP.NET Core Module (ANCM)**.

### Step 1 — Install the **.NET Hosting Bundle** on the VM
Required even if you have the SDK. It installs ANCM into IIS.

```powershell
# Example: .NET 9 Hosting Bundle (replace the URL with the current installer)
Invoke-WebRequest `
  -Uri "https://aka.ms/dotnet/9.0/dotnet-hosting-win.exe" `
  -OutFile "$env:TEMP\dotnet-hosting.exe"
Start-Process "$env:TEMP\dotnet-hosting.exe" -ArgumentList "/quiet","/norestart" -Wait
iisreset
```

After install, IIS Manager will recognize the **AspNetCoreModuleV2** handler.

### Step 2 — Publish your app
On your dev machine:
```powershell
dotnet publish -c Release -o C:\publish\MyApi
```

Copy `C:\publish\MyApi` to the VM, e.g. `C:\inetpub\wwwroot\MyApi`.

### Step 3 — Create a Website (PowerShell)
```powershell
Import-Module WebAdministration

# App pool with "No Managed Code" (Kestrel does the work, not .NET Framework)
New-WebAppPool -Name "MyApiPool"
Set-ItemProperty IIS:\AppPools\MyApiPool -Name "managedRuntimeVersion" -Value ""

# Site bound to port 80, host header optional
New-Website `
  -Name "MyApi" `
  -PhysicalPath "C:\inetpub\wwwroot\MyApi" `
  -ApplicationPool "MyApiPool" `
  -Port 80 `
  -HostHeader "api.example.com"

# Or to replace the Default Web Site entirely:
# Stop-Website "Default Web Site"; Remove-Website "Default Web Site"
```

### Step 4 — Permissions
The app pool identity (`IIS AppPool\MyApiPool`) needs **read** on the site folder, and **write** to any log folder.

```powershell
icacls "C:\inetpub\wwwroot\MyApi" /grant "IIS AppPool\MyApiPool:(OI)(CI)R" /T
```

### Step 5 — Hit the site
```powershell
Invoke-WebRequest http://localhost
```
You should see your API's response.

---

## 18. The `web.config` for an ASP.NET Core app

`dotnet publish` generates one automatically. It looks like:

```xml
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*"
             modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>
      <aspNetCore processPath=".\MyApi.exe"
                  stdoutLogEnabled="false"
                  stdoutLogFile=".\logs\stdout"
                  hostingModel="inprocess" />
    </system.webServer>
  </location>
</configuration>
```

- **`inprocess`** (default) — Kestrel runs inside IIS worker process (`w3wp.exe`). Faster.
- **`outofprocess`** — IIS proxies to a separate `dotnet.exe` process. Used when you need full IIS features that don't work in-process.

---

## 19. HTTPS in IIS

### Option A — Bind an existing PFX
```powershell
# Import cert to the local computer's My store
$pwd = ConvertTo-SecureString "P@ss" -AsPlainText -Force
Import-PfxCertificate -FilePath C:\certs\mysite.pfx -CertStoreLocation Cert:\LocalMachine\My -Password $pwd

# Bind to your site
New-WebBinding -Name "MyApi" -Protocol https -Port 443 -HostHeader "api.example.com" -SslFlags 1
$cert = Get-ChildItem Cert:\LocalMachine\My | Where-Object Subject -Match "api.example.com"
(Get-WebBinding -Name "MyApi" -Protocol https).AddSslCertificate($cert.Thumbprint, "My")
```

### Option B — Free cert from Let's Encrypt
Use **win-acme** (`wacs.exe`) — interactive wizard, supports auto-renew via Task Scheduler.

### Force HTTPS redirect
Easiest: in your ASP.NET Core app `Program.cs`:
```csharp
app.UseHttpsRedirection();
app.UseHsts();
```

---

## 20. Common IIS troubleshooting

| Symptom | Likely cause | Fix |
|---|---|---|
| **500.30 — ANCM in-process start failure** | App crashed on startup; bad config | Check Windows Event Viewer → Application; enable stdout log in `web.config` |
| **500.19 — Bad config** | Permissions on the site folder; broken `web.config` | Grant app pool read; validate XML |
| **502.5 — Process failure** | Wrong .NET runtime / Hosting Bundle missing | Install matching Hosting Bundle, `iisreset` |
| **HTTP Error 404 on extension-less URLs** | Static handler matching first | Ensure ANCM handler is registered (it is by default for .NET Core sites) |
| **Slow first request** | App pool idle timeout | Set `idleTimeout="0"` on the pool; enable preload |
| **App stops at night** | App pool recycle | Tune recycle settings; add Always On (Azure App Service) |
| **Cannot write logs** | App pool identity has no write perm | `icacls` grant on logs folder |
| **HTTPS cert not appearing** | Cert imported to wrong store | Import to `LocalMachine\My`, not `CurrentUser` |

Enable **stdout logging** temporarily for ASP.NET Core to see startup exceptions:
```xml
<aspNetCore ... stdoutLogEnabled="true" stdoutLogFile=".\logs\stdout" />
```

---

## 21. IIS internal architecture (good to know)

```
            Client
              │  HTTP/HTTPS
              ▼
   ┌─────────────────────┐
   │  http.sys (kernel)  │  ← receives requests, queues them per app pool
   └─────────┬───────────┘
             ▼
   ┌─────────────────────┐
   │  WAS (Windows Activation Service)  │  ← starts app pools
   └─────────┬───────────┘
             ▼
   ┌─────────────────────┐
   │  w3wp.exe per app pool  │  ← worker process; loads modules
   │  └─ AspNetCoreModuleV2 │  ← spawns Kestrel for .NET Core
   │     └─ Your app (in-proc) │
   └─────────────────────┘
```

- **Application Pool** = isolation boundary (one crashed pool doesn't kill others).
- **Site** = a logical web site bound to one or more (IP:port:host) endpoints.
- **Application / Virtual Directory** = sub-paths under a site, can have their own pool.

---

## 22. Day-2 IIS commands you'll actually use

```powershell
# Service control
iisreset                              # restart entire IIS
Restart-WebAppPool MyApiPool          # bounce one pool
Stop-Website     MyApi
Start-Website    MyApi

# Inventory
Get-Website
Get-WebApplication
Get-WebBinding -Name MyApi
Get-WebAppPoolState MyApiPool

# Recycle settings (memory limit, schedule)
Set-ItemProperty IIS:\AppPools\MyApiPool `
  -Name recycling.periodicRestart.memory -Value 1500000      # KB
Set-ItemProperty IIS:\AppPools\MyApiPool `
  -Name recycling.periodicRestart.schedule -Value @('03:00:00')
```

`appcmd.exe` (in `%windir%\system32\inetsrv\`) is the classic CLI; PowerShell `WebAdministration` module is the modern choice.

---

## 23. IIS vs Kestrel vs Azure App Service

| | Kestrel alone | IIS + Kestrel | Azure App Service |
|---|---|---|---|
| What it is | The in-process .NET web server | IIS proxies to Kestrel | Managed Windows/Linux web hosting (uses IIS under the hood on Windows plans) |
| You manage | App + OS | App + OS + IIS | Just the app |
| Best for | Linux, containers, K8s | Windows VMs / on-prem | Most modern web apps |
| Scale | Manual / VMSS | Manual / VMSS | Built-in autoscale |

> If you're already in Azure, **App Service** or **Container Apps** is usually a better fit than building IIS on a VM yourself — unless you need full server control.

---

## 24. Quick interview-style Q&A

**Q: What is a VM?**
An emulated computer running on a hypervisor, with its own OS, vCPU, memory, disk, and NIC.

**Q: VM vs container?**
VMs virtualize hardware and run a full guest OS (heavy, strong isolation). Containers virtualize the OS process space (light, fast, share kernel).

**Q: What's the difference between Stop and Deallocate on an Azure VM?**
"Stop" leaves the VM allocated (still billed for compute). "Deallocate" releases the underlying host (stops compute billing; disks still cost).

**Q: How do you achieve VM high availability in Azure?**
Use **Availability Zones** (or Availability Sets), VMSS for horizontal scale, front them with a Load Balancer or App Gateway, and Azure Backup + Site Recovery for DR.

**Q: What is IIS?**
Microsoft's web server included with Windows. Hosts ASP.NET (Framework) directly and ASP.NET Core via the ANCM reverse proxy to Kestrel.

**Q: How does IIS host ASP.NET Core?**
The **ASP.NET Core Module v2** runs inside the IIS worker process and either hosts Kestrel **in-process** (default) or proxies to an out-of-process `dotnet.exe`.

**Q: What is the Hosting Bundle?**
The installer that adds the ANCM, .NET runtime, and ASP.NET Core runtime to a Windows server so IIS can run .NET Core apps.

**Q: Why use an Application Pool?**
For isolation, identity, recycling, and resource limits per app. A crash in one pool doesn't bring down sites in other pools.

**Q: In-process vs out-of-process hosting?**
In-process is faster (no extra hop) and the default. Out-of-process is needed for some advanced scenarios or non-Windows-friendly configs.

**Q: How do you troubleshoot a 502.5 in IIS?**
Hosting Bundle missing or version mismatch → install the matching one and `iisreset`. Check Event Viewer and stdout logs.

**Q: Where would you NOT use IIS today?**
For greenfield cloud-native apps — prefer App Service, Container Apps, or AKS. IIS shines for legacy ASP.NET, Windows-auth-heavy intranets, and tight on-prem integrations.

---

## 25. Mental model (one-liner)

> **A VM is a full virtual computer you own end-to-end; IIS is the Windows web server that lives inside such a VM, accepting HTTP traffic and (via the ASP.NET Core Module) handing it to Kestrel which actually runs your .NET app.**
