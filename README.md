<img width="110" height="128" alt="image" src="https://github.com/user-attachments/assets/86382633-41b0-4c7d-aed9-a9b68106a1df" />

# Gheetah - Test Orchestration Platform

![License](https://img.shields.io/badge/License-MIT-blue.svg)
![Status](https://img.shields.io/badge/Status-Open--Source-brightgreen)
![.NET](https://img.shields.io/badge/.NET-8.0-purple)
![Java](https://img.shields.io/badge/Java-11+-red)
![Playwright](https://img.shields.io/badge/Playwright-TypeScript-2EAD33)

**Gheetah** is an open-source test orchestration platform designed to streamline the execution and management of BDD (Behavior-Driven Development) and modern test automation projects. With built-in IDE capabilities, project scaffolding, enterprise pull request management, and distributed execution, Gheetah transforms how QA teams work with C#, Java, and Playwright.

---

## Table of Contents
- [Introduction](#introduction)
- [What's New in v2.1](#whats-new-in-v21)
- [What's New in v2.0](#whats-new-in-v20)
- [System Requirements](#system-requirements)
- [Installation](#installation)
- [Features](#features)
- [Usage](#usage)
- [Support](#support)
- [License](#license)

---

## Introduction

Gheetah is a powerful **Test Orchestration Platform** with:

- **Cross-platform** .NET 8.0 MVC backend
- **Modern frontend** designed using Tabler.io
- **No database dependency** – stores data as JSON files in `/Data` directory
- **Support for BDD and modern test automation** in C# (Reqnroll/SpecFlow), Java (TestNG-Cucumber/JUnit-Cucumber), and **Playwright (TypeScript)**
- **Run tests remotely** via Gheetah Agent or directly on the Gheetah Server
- **Real-time test results** with language-specific structured reporting

### Key Features

| Category | Features |
|----------|----------|
| **Test Execution** | Run tests remotely via Gheetah Agent or locally, tag/test-name filtering, real-time SignalR output |
| **IDE Capabilities** | Monaco editor, IntelliSense, step definition navigation, TypeScript support, diff viewer |
| **Project Scaffolding** | Create C#/Java/Playwright projects from scratch (API, Web, Mobile, Desktop) |
| **Pull Requests** | Inline comments, conflict resolver, approval workflow, merge & build pipeline |
| **Enterprise-Ready** | Customizable dashboard, user/role management, SSO (Azure AD, Google) |
| **CI/CD Integrations** | Azure DevOps, Jenkins, GitLab |
| **Lightweight** | Simple JSON-based storage, no database required |

---

## What's New in v2.1 🚀

### 🎭 Full Playwright (TypeScript) Support

Gheetah v2.1 brings complete Playwright support with full parity to the existing C# and Java features.

#### Project Management
- **Create** Playwright projects from scratch via the Project Wizard (Language → Playwright)
- **Upload** existing Playwright projects as `.zip` / `.rar` archives
- **Clone** from GitHub, GitLab, Azure DevOps — Playwright projects auto-detected from `playwright.config.ts` and `.spec.ts` files
- **Build** Playwright projects (`npm install` + test discovery) — no separate build step needed

#### Test Explorer
- **`test.describe` hierarchy** — JsTree groups tests by their describe block, showing each `test(...)` as a runnable leaf node
- **Scenario Content panel** — clicking a test shows only that test's TypeScript code (not the entire file), with the describe block shown as context comment
- **TypeScript syntax highlighting** in the Scenario Content panel

#### Test Execution
- **Run single test** — uses `--grep 'test name'` to execute only the selected test
- **Run all tests** — executes the entire spec suite
- **Screenshot + video capture** for every run via automatically generated `gheetah-runner.config.ts` — works even if the project's own config has `only-on-failure` settings
- **Playwright HTML Report** — Gheetah-styled report showing suite grouping, pass/fail/skip badges, browser label, duration, error details (expandable), and embedded screenshots/videos

#### Reporting
- **Playwright JSON** reporter output parsed into a Gheetah-native report with:
  - Summary row: `✓ N passed`, `✗ N failed`, `− N skipped`, total duration
  - Suite groupings by spec file
  - Each test with status icon, name, browser, duration
  - Failed tests auto-expanded showing full error message
  - Screenshots embedded as base64
  - Videos served via `/Scenarios/GetPlaywrightAttachment`

#### Agent Execution
- Full Playwright remote execution via Gheetah Agent
- Agent creates the same `gheetah-runner.config.ts` override for consistent screenshot/video capture
- JSON results sent back to server and rendered identically to local runs

#### IDE (Gheetah Editor)
- **Add New Item** dialog extended with TypeScript file types:
  - **Playwright Spec Test** (`.spec.ts`) — generates a `test.describe` skeleton
  - **Page Object Model** (`.ts`) — generates a typed POM class
  - **Custom Fixture** (`.ts`) — generates a `test.extend` fixture
  - **TypeScript Class** (`.ts`) — generates a plain class
- `.spec.ts` files get a test-pipe icon; `.ts` files get a TypeScript icon in the file tree

#### Rich Templates
The Playwright `Base_Web` template ships with **14 `test.describe` blocks** covering all major Playwright capabilities:

| Describe Block | What it demonstrates |
|----------------|----------------------|
| `Navigation` | `goto`, `goBack`, `goForward`, `reload`, URL/title assertions |
| `Locator Strategies` | role, text, placeholder, label, CSS, XPath, testId, nth, filter |
| `Form Interactions` | `fill`, `clear`, `check`, `uncheck`, `selectOption`, form submission |
| `Keyboard and Mouse` | `keyboard.press`, key combos, hover, dblclick, right-click, drag-and-drop, scroll |
| `Waiting and Auto-Wait` | `waitForSelector`, `waitForURL`, `networkidle`, `waitForResponse`, `waitForFunction` |
| `Dialogs and Alerts` | alert accept/dismiss, confirm, prompt, message capture |
| `Frames` | `frameLocator`, nested frames |
| `Multiple Pages and Tabs` | `context.waitForEvent('page')`, multi-context, popup handling |
| `Network Interception` | `route.fulfill`, block resources, modify headers, log requests |
| `Screenshots` | full-page, element, clipped screenshots |
| `Playwright Assertions` | state, text, attribute, count, value, URL, title |
| `API Testing` | GET, POST, PUT, DELETE, auth, reusable `request.newContext` |
| `Mobile Viewport` | custom viewport, custom user agent |
| `Storage and Cookies` | cookies, localStorage, sessionStorage |
| `JavaScript Execution` | `evaluate`, DOM manipulation, element evaluate |

### 🔧 Execution & Reporting Improvements

#### Smarter Output Handling
- **ANSI code stripping** — terminal escape sequences (`\x1B[...`) are stripped from all output before display
- **Intelligent stderr filtering** — Java logger INFO messages, WebDriverManager notifications, CDP warnings, and npm informational output are no longer prefixed with `Error:`, preventing false alarms in the output panel

#### Java — Cucumber JSON Reports (Server & Agent)
- Java executors now look for `target/cucumber-reports/**/*.json` (Cucumber's actual output directory) instead of the non-existent `TestResults/` directory
- Cucumber JSON parsed into a **Gheetah-native step-level report**: each step shown with ✓/✗/− icon, keyword highlighted in indigo, duration, and full error message for failed steps
- Failed tests no longer cause early `return` before the report is collected — the JSON report is always generated even when tests fail

#### GheetahHub — Multi-format Result Routing
`SendOutput`, `SendResult`, and `ReceiveResult` in the Hub now detect result content type automatically:
- Content starting with `[` → **Cucumber JSON** → `GenerateCucumberHtmlReport`
- Content starting with `{` → **Playwright JSON** → `GeneratePlaywrightHtmlReport`
- All other content → **TRX/XML** → `ParseStdOutFromXml` + `GenerateHtmlReport` (C# existing behavior preserved)

#### C# — Step Status Fixed
Reqnroll step status is now correctly determined from each step's own output:
- `-> skipped because of previous errors` → **Failed** (red) — previously showed green
- `-> No matching step definition found` → **Failed** (red)
- `-> error:` / `-> fail:` → **Failed** (red)
- `-> skipped:` → **Skipped** (yellow)
- `-> done:` → **Passed** (green)

### 🏗️ Expanded BDD Templates

Both C# (Reqnroll) and Java (Cucumber) templates now include **comprehensive step definitions** covering the full Selenium WebDriver API:

| Category | New Steps Added |
|----------|----------------|
| **Dropdown** | Select by text / value / index; assert selected option |
| **Checkbox/Radio** | Check, uncheck, assert checked state |
| **Alerts** | Accept, dismiss, enter text in prompt, assert alert text |
| **Frames** | Switch by ID/Name/XPath/index; switch to parent; switch to default |
| **Tabs/Windows** | Open new tab, switch to tab by index, close tab, switch to main |
| **Scroll** | Scroll to top/bottom, scroll by pixels, scroll to element |
| **Wait** | Wait N seconds/ms, wait for element visible/disappear, wait for URL/title |
| **Keyboard** | Arrow keys, Home/End, PageUp/Down, Ctrl/Alt/Shift combos, key on element |
| **JavaScript** | Execute script, set attribute, highlight element |
| **Drag & Drop** | `dragTo` / `DragAndDrop` |
| **File Upload** | `SendKeys` with file path |
| **Cookies** | Add, delete, assert cookie exists |
| **Assertions** | URL (be/contain/end with), page source, element count (exact/greater than), CSS property, attribute (not be empty), dropdown selection, checkbox state |

Feature files include **10–12 scenarios** demonstrating all steps against publicly available test sites (`the-internet.herokuapp.com`, `reqres.in`).

### 📁 Upload — Playwright Support
The **Upload Projects** tab now has three language options (Java, C#, Playwright) with auto-detection: if a `.zip` contains `playwright.config.ts` and `.spec.ts` files, it is accepted as a Playwright project regardless of the selected language.

---

## What's New in v2.0 🚀

### 🔧 Built-in Professional IDE
- **Monaco Editor** – Same engine as VS Code with syntax highlighting for C#, Java, Gherkin, TypeScript, JSON, XML
- **IntelliSense & Autocomplete** – Code snippets for classes, methods, properties, step definitions, test methods
- **Ctrl+Click Navigation** – Jump from `.feature` steps to step definition implementations instantly
- **Real-time Diff Viewer** – Track changes before commit
- **Tab Management** – Open multiple files, close with Ctrl+Q, switch with Ctrl+1-9

### 🏗️ Scaffold Projects from Scratch
Create complete BDD automation projects directly within Gheetah:

| Language | Frameworks | Template Types |
|----------|------------|----------------|
| **C# / .NET** | SpecFlow, Reqnroll | API, Web, Mobile, Desktop |
| **Java** | Cucumber-JVM (JUnit/TestNG) | API, Web, Mobile, Desktop |

Generated projects include pre-configured package references, sample feature files, sample step definition classes, and build configuration ready for Gheetah execution.

### 👥 Enterprise Pull Request System
- **Inline Code Comments** – Discuss code line by line on specific lines
- **Visual Conflict Resolver** – Ours/Theirs merge conflict UI
- **Merge & Build Pipeline** – Auto-build source branch → merge → build target branch
- **Automatic Remote Sync** – Push to GitHub/Azure DevOps after merge (or create remote PR if branch protected)
- **Approval Workflow** – Reviewers must approve before merge
- **Real-time Pipeline Status** – Track each step (build source, merge, build target) in UI

---

## System Requirements

### Gheetah Server

| Component | Minimum | Recommended |
|-----------|---------|-------------|
| **Operating System** | Windows Server 2016/2019/2022 or Linux (RHEL 8+, Ubuntu 20.04+) | Same |
| **Compute** | 4 CPU cores | 8+ CPU cores |
| **Memory** | 8GB RAM | 16GB RAM |
| **Storage** | 5GB available space | 10GB+ SSD |
| **Software** | .NET 8.0 Runtime | .NET 8.0 SDK |

### Execution Agent

| Component | Minimum | Recommended |
|-----------|---------|-------------|
| **Operating System** | Windows 10/11 or Linux (x64 distributions) | Same |
| **Compute** | 2 CPU cores | 4+ CPU cores |
| **Memory** | 4GB RAM | 8GB RAM |
| **Runtime** | .NET 8.0 | .NET 8.0 + Java 11+ + Node.js 18+ |

### Language-Specific Requirements (Server & Agent)

| Language | Required Software |
|----------|-------------------|
| **C# / Reqnroll** | .NET 8.0 SDK, NuGet restore capability |
| **Java / Cucumber** | JDK 11+, Maven 3.6+ or Gradle 7.4+, `JAVA_HOME` set |
| **Playwright / TypeScript** | Node.js 18+ with `npm` in PATH, Chromium (installed via `npx playwright install` or bundled) |

> **Note for Playwright:** Run `npx playwright install chromium` on the server/agent machine after Node.js is installed. The build step runs `npm install`; browser binaries are installed separately.

---

## Installation

### First Installation Wizard

When you deploy **Gheetah** to a compatible server or run it via Visual Studio, you will be greeted by the **"First Installation"** wizard:

1. **SSO Provider Selection**
   - **Azure AD**: Configure tenant ID, application ID, and secret key
   - **Google Workspace**: Set up OAuth 2.0 credentials and domain-wide delegation
   - **No SSO**: Built-in JWT-based authentication with custom register/login

2. **Manage Groups**
   - Define and organize user groups to streamline access control
   - Default groups: `admin-grp`, `lead-grp`, `runner-grp`
   - Supports hierarchical group structures with inheritance capabilities

3. **Add Permissions to Groups**
   - Assign specific permissions to defined groups
   - Granular permissions available for all system modules
   - Default permissions: Clone, Build, Delete, ManageUsers, ManageGroups, ManagePermissions, ViewDashboard, RunScenarios, ViewResults

4. **Configure Project Folder**
   - ⚠️ **Critical System Configuration** – defines the root directory where all test automation projects will be stored
   - If no directory is specified, projects default to `Gheetah\CopiedProjects`
   - Without this configuration, the entire test orchestration pipeline will be non-functional

### Agent Setup

#### Windows Agent Installation

1. Navigate to the **"Agent List"** page in your Gheetah instance
2. Download the `GheetahAgentSetup.exe` package
3. Run the installer and follow the wizard (click "Next" through each step)
4. After installation completes, launch the Agent
5. Enter your Gheetah server **root URL** (without trailing slash, e.g., `https://your-gheetah-instance.com`)
6. The Agent sends a registration request to your Gheetah server
7. Administrator approves the **"Pending Request"** in the Gheetah admin interface
8. Agent becomes officially registered and ready for remote test execution

#### Linux Agent Setup

For Linux environments, prepare the agent manually using the `Gheetah.Agent` component from the project repository.

### Pending Request Security Mechanism

The Pending Request system serves as a critical security gateway:
- All agent registration requests require manual administrator approval
- Unauthorized registration attempts may result in the agent being **blacklisted**
- Declined requests require manual intervention to reset

**Blacklist Recovery Process:**
1. Clear the corresponding entry from `Gheetah/Data/agents-black-list.json`
2. Navigate to the Agent installation directory and clear all JSON files in the `Data` folder
3. Restart the Agent and submit a new registration request

---

## Features

### 🖥️ Built-in IDE

| Feature | Description |
|---------|-------------|
| **File Explorer** | Tree view with language-specific icons (C#, Java, Gherkin, TypeScript, JSON, XML) |
| **Monaco Editor** | Same engine as VS Code with syntax highlighting for all supported languages |
| **IntelliSense** | Code completion for C#, Java, Gherkin, TypeScript |
| **Code Snippets** | `class`, `method`, `prop`, `stepdef`, `test`, Feature/Scenario, Playwright spec templates |
| **Add New Item** | C# (Class/Interface/StepDef), Java (Class/StepDef), TypeScript (Spec/POM/Fixture/Class), Feature, XML, JSON |
| **Step Definition Navigation** | Ctrl+Click on Given/When/Then → opens step definition |
| **Diff Viewer** | Side-by-side comparison of modified files |
| **Tab Management** | Open multiple files, close with Ctrl+Q, switch with Ctrl+1-9 |
| **Keyboard Shortcuts** | Ctrl+S (Save), Ctrl+Shift+N (New File), Delete (Delete File) |
| **Autosave Backup** | Local storage backup of unsaved changes |

### 🔄 Git & Version Control

| Feature | Description |
|---------|-------------|
| **Branch Management** | Create, switch, delete branches from dropdown |
| **Commit & Push** | Stage changes and push to new or existing branches |
| **Pull & Merge** | Fetch and merge remote changes |
| **Remote Providers** | GitHub, Azure DevOps, GitLab, Bitbucket |
| **Remote Repo Creation** | Create repository on provider directly from Gheetah |
| **Connect to Remote** | Link local project to existing remote repository |
| **Disconnect Remote** | Remove remote connection without deleting files |
| **Push History** | Track all pushes with commit hashes and PR status |

### 👥 Pull Request System

| Feature | Description |
|---------|-------------|
| **Create PR** | From push history, select target branch, add reviewers, write description |
| **Inline Comments** | Click gutter on line numbers to add comments on specific lines |
| **Resolve Comments** | Track resolution status (Active → Resolved) |
| **Reactivate Comments** | Reopen resolved comments if needed |
| **Approve/Reject** | Reviewers can approve or reject changes |
| **Conflict Resolver** | Visual Ours (Source) / Theirs (Target) resolution UI |
| **Merge & Build Pipeline** | Auto-build source → merge → build target with real-time status |
| **Retry Build** | Re-run target branch build if it fails |
| **Remote Sync** | Auto-push to remote after merge (or create remote PR if branch protected) |
| **PR Activities** | Timeline of all actions (created, approved, merged, commented) |
| **PR Snapshots** | Store file snapshots for closed PRs to preserve diff history |

### 🏗️ Project Scaffolding

Create new test projects directly from Gheetah without leaving the platform:

**Supported Project Types:**

| Type | C# (.NET) | Java | Playwright |
|------|-----------|------|------------|
| **Web Testing** | Selenium + Reqnroll | Selenium + Cucumber | `@playwright/test` |
| **API Testing** | RestSharp + Reqnroll | RestAssured + Cucumber | `@playwright/test` request context |
| **Mobile Testing** | Appium | Appium | — |
| **Desktop Testing** | WinAppDriver | WinAppDriver | — |

**Generated Project Includes:**
- Pre-configured dependencies (NuGet / Maven / npm)
- Sample feature/spec file with example scenarios covering all key capabilities
- Sample step definition class with 40+ ready-to-use steps (C#/Java)
- `.gitignore` configured for each ecosystem
- Build configuration ready for Gheetah execution
- **Playwright:** `playwright.config.ts`, `tsconfig.json`, `package.json`, comprehensive `example.spec.ts`

### 📁 Project Management

| Feature | Description |
|---------|-------------|
| **Add Projects** | Via remote repository clone, local ZIP/RAR upload, or scaffold from scratch |
| **Supported Frameworks** | C# (Reqnroll/SpecFlow), Java (Cucumber-JUnit/TestNG), Playwright (TypeScript) |
| **Auto Language Detection** | Clone/upload of TypeScript repos with `playwright.config.ts` auto-detected as Playwright |
| **Build Projects** | `dotnet build` (C#), `mvn package` (Java), `npm install` (Playwright) |
| **Delete Projects** | Admin/Lead only, blocked during active execution |

### 🧪 Test Execution

| Feature | Description |
|---------|-------------|
| **Scenario/Test Discovery** | `.feature` files for C#/Java (with unique tag assignment); `.spec.ts` test functions for Playwright |
| **Tree Grouping** | C#/Java: feature file → scenario; Playwright: spec file → describe block → individual test |
| **Test Content Preview** | Selected scenario/test shows its content with syntax highlighting |
| **Tag / Name Filtering** | C#/Java: run by generated tag; Playwright: run by test name via `--grep` |
| **Agent Selection** | Choose remote agent or execute locally |
| **Real-time Output** | SignalR streaming of execution logs with ANSI stripping |
| **Language-specific Reports** | C#: TRX step report; Java: Cucumber JSON step report; Playwright: JSON suite report with screenshots/videos |
| **Hangfire Integration** | Background job tracking for "Run All Scenarios/Tests" |

### 📊 Dashboard

| Widget Type | Description |
|-------------|-------------|
| **Azure Pipeline Charts** | Bar charts of test results (requires Azure CI/CD integration) |
| **Azure Test Result Table** | Detailed test result tables |
| **Recent Scenario Execution** | Latest individual scenario/test runs |
| **Recent Hangfire Jobs** | Background job monitoring |
| **Custom Widgets** | Drag-and-drop, resizable, user-specific layouts |

### 🔌 Integrations

| Category | Supported Platforms | Status |
|----------|---------------------|--------|
| **CI/CD** | Azure DevOps | Fully tested |
| **CI/CD** | Jenkins | Basic integration |
| **CI/CD** | GitLab CI/CD | Basic integration |
| **SSO** | Azure AD | Fully tested |
| **SSO** | Google Workspace | Fully tested |
| **SSO** | No SSO (Built-in) | Fully tested |
| **Version Control** | GitHub | Fully tested |
| **Version Control** | Azure DevOps | Fully tested |
| **Version Control** | GitLab | Available (not fully tested) |
| **Version Control** | Bitbucket | Available (not fully tested) |
| **Email** | SMTP, SendGrid, Azure Communication | Configuration ready |

### 👥 User & Role Management

| Feature | Description |
|---------|-------------|
| **Customizable RBAC** | Fully customizable role-based access control |
| **Default Roles** | Admin (`admin-perm`), Lead (`lead-perm`), Runner (`runner-perm`) |
| **Group Management** | Create custom groups with any naming convention |
| **Permission Assignment** | Granular permissions for all system modules |
| **User Management Dashboard** | Manage users and their roles |

---

## Usage

### Quick Start: Create a New Project

1. Navigate to **Projects → Add Project** from the top navigation bar
2. Select the **"Create New"** tab
3. Choose your **Language** (C#, Java, or **Playwright**)
4. Select a **Test Adapter** (auto-selected for Playwright)
5. Select a **Project Template** (Web for Playwright; API/Web/Mobile/Desktop for C#/Java)
6. Enter a **Project Name**
7. Click **"Create Project"**

### Quick Start: Run a Playwright Test

1. From **Project List**, click **Build** on your Playwright project (runs `npm install`)
2. Click the **Scenarios** icon to open the Test Explorer
3. Expand the spec file → expand the describe block → click an individual test
4. The **Scenario Content** panel shows only that test's TypeScript code
5. Click **"Run Options"** → the selected test name is shown in the panel
6. Optionally select a remote Agent
7. Click **"Run Test"**
8. Monitor real-time output; the **Scenario Execution Report** shows the Playwright-styled report with pass/fail badges, duration, and any screenshots/videos

### Quick Start: Create a New File in the IDE

1. Right-click a folder in the file explorer → **"Add New Item..."**
2. Select the **File Type**:
   - For Playwright projects: choose **TypeScript File (.ts)**
   - Select a **TypeScript Template**: Playwright Spec Test, Page Object Model, Custom Fixture, or TypeScript Class
   - Spec Test filenames automatically get `.spec` appended (e.g., `Login` → `Login.spec.ts`)
3. Enter the file name and click **Create**

### Quick Start: Open and Edit a Feature File

1. From the **Project List**, click on your project to open the IDE
2. In the **Solution Explorer** (left sidebar), navigate to your feature files
3. Click any `.feature` file to open it in the editor
4. Edit the scenario steps with full syntax highlighting
5. Press **Ctrl+Click** on any Given/When/Then step → automatically navigates to the step definition
6. Press **Ctrl+S** to save changes

### Quick Start: Commit and Create Pull Request

1. Make changes to files (dirty indicator appears on modified files)
2. Switch to the **Git** tab in the left sidebar
3. Select or create a branch from the dropdown (cannot push directly to main/master)
4. Click the **Commit** button
5. After successful push, go to **Push History** panel at the bottom
6. Find your push and click the **PR** button
7. Select **Target Branch**, add **Reviewers**, write a **Description**
8. Click **"Create Pull Request"**

### Running Tests

1. From **Project List**, click the **Scenarios** icon on your built project
2. Select a scenario/test from the JsTree
3. Click **"Run Options"** to configure:
   - **C#/Java**: select the generated tag for the specific scenario
   - **Playwright**: selected test name is shown automatically
   - Choose an Agent (or leave empty for local execution)
4. Click **"Run Scenario"** / **"Run Test"** or **"Run All"**
5. Monitor real-time output in the modal window via SignalR
6. View detailed HTML reports after execution completes

### Agent Management

1. Navigate to the **Agents** section from the top navigation bar
2. Download `GheetahAgentSetup.exe` for Windows agents
3. Install and run the agent on target machines
4. Agent appears as **"Pending Request"** in the agent list
5. Administrator approves the request → agent becomes active
6. Select the agent when running scenarios for remote execution

### Dashboard Customization

1. Click the **gear icon** on the dashboard
2. Add, remove, or configure widgets
3. Drag and drop widgets to rearrange
4. Resize widgets using corner handles
5. Layout is automatically saved per user

---

## Support

### Professional Support & Consulting

For assistance with:
- Test automation strategy and implementation
- Gheetah deployment and configuration
- Custom integration development
- Dashboard widget development
- CI/CD pipeline integration
- Custom reporting solutions

**Contact:**
- 📧 **Email**: [gheetahinfo@gmail.com](mailto:gheetahinfo@gmail.com)
- 💬 **GitHub Discussions**: [gokcenveysel/Gheetah/discussions](https://github.com/gokcenveysel/Gheetah/discussions)
- 🔗 **LinkedIn**: [Veysel Gökçen](https://linkedin.com/in/veyselgokcen/)

### Community Support
- 🐛 **Report Issues**: [GitHub Issues](https://github.com/gokcenveysel/Gheetah/issues)
- 💡 **Feature Requests**: Welcome via GitHub Discussions
- ⭐ **Star the Project**: Show your support on GitHub

---

## License

Gheetah is released under the **MIT License** with the following considerations:

- ✅ Complete open-source codebase (framework and implementation)
- ✅ Free for **non-commercial** use only
- ⚠️ No warranty or liability for untested features
- ⚠️ Users should perform their own security assessments before production deployment
- ⚠️ Some features remain theoretically implemented and not fully tested

**For commercial licensing or enterprise support**, please contact [gheetahinfo@gmail.com](mailto:gheetahinfo@gmail.com).

---

## Quick Links

| Resource | Link |
|----------|------|
| 🌐 **Landing Page** | [gokcenveysel.github.io/Gheetah](https://gokcenveysel.github.io/Gheetah) |
| 📘 **Documentation** | [gokcenveysel.github.io/Gheetah/howto.html](https://gokcenveysel.github.io/Gheetah/howto.html) |
| 💻 **GitHub Repository** | [github.com/gokcenveysel/Gheetah](https://github.com/gokcenveysel/Gheetah) |
| 🐛 **Issue Tracker** | [GitHub Issues](https://github.com/gokcenveysel/Gheetah/issues) |
| 💬 **Discussions** | [GitHub Discussions](https://github.com/gokcenveysel/Gheetah/discussions) |

---

*Built with ❤️ for the QA and test automation community.*

**Gheetah** – *Orchestrate, Execute, Innovate.*
