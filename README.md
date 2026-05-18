<img width="110" height="128" alt="image" src="https://github.com/user-attachments/assets/86382633-41b0-4c7d-aed9-a9b68106a1df" />

# Gheetah - Test Orchestration Platform

![License](https://img.shields.io/badge/License-MIT-blue.svg)
![Status](https://img.shields.io/badge/Status-Open--Source-brightgreen)
![.NET](https://img.shields.io/badge/.NET-8.0-purple)
![Java](https://img.shields.io/badge/Java-11+-red)

**Gheetah** is an open-source test orchestration platform designed to streamline the execution and management of BDD (Behavior-Driven Development) test automation projects. With built-in IDE capabilities, project scaffolding, enterprise pull request management, and distributed execution, Gheetah transforms how QA teams work with BDD.

---

## Table of Contents
- [Introduction](#introduction)
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
- **Support for BDD test projects** in C# (Reqnroll/SpecFlow) or Java (TestNG-Cucumber/JUnit-Cucumber)
- **Run tests remotely** via Gheetah Agent or directly on the Gheetah Server
- **Real-time test results** with structured reporting

### Key Features

| Category | Features |
|----------|----------|
| **Test Execution** | Run tests remotely via Gheetah Agent or locally, tag filtering, real-time SignalR output |
| **IDE Capabilities** | Monaco editor, IntelliSense, step definition navigation, diff viewer |
| **Project Scaffolding** | Create C#/Java BDD projects from scratch (API, Web, Mobile, Desktop) |
| **Pull Requests** | Inline comments, conflict resolver, approval workflow, merge & build pipeline |
| **Enterprise-Ready** | Customizable dashboard, user/role management, SSO (Azure AD, Google) |
| **CI/CD Integrations** | Azure DevOps, Jenkins, GitLab |
| **Lightweight** | Simple JSON-based storage, no database required |

---

## What's New in v2.0 🚀

### 🔧 Built-in Professional IDE
- **Monaco Editor** – Same engine as VS Code with syntax highlighting for C#, Java, Gherkin, JSON, XML
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
| **Runtime** | .NET 8.0 | .NET 8.0 + Java 11+ |

### For Java Project Support (on Gheetah Server)

- **JDK**: OpenJDK 11+ or Oracle JDK 11+
- **Build Tools**: Maven 3.6+ or Gradle 7.4+
- **Environment**: Properly configured `JAVA_HOME` and build tool paths

### Language-Specific Agent Requirements

| Language | Requirements |
|----------|--------------|
| **C# / SpecFlow** | NuGet package restore capability, MSBuild or dotnet CLI |
| **Java / Cucumber** | Maven or Gradle installation, network access to artifact repositories, proper `JAVA_HOME` |

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
   - ⚠️ **Critical System Configuration** – defines the root directory where all BDD test automation projects will be stored
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
| **File Explorer** | Tree view of project files with folder/file icons |
| **Monaco Editor** | Same engine as VS Code with syntax highlighting |
| **IntelliSense** | Code completion for C#, Java, Gherkin |
| **Code Snippets** | `class`, `method`, `prop`, `stepdef`, `test`, Feature/Scenario templates |
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

Create new BDD projects directly from Gheetah without leaving the platform:

**Supported Project Types:**

| Type | C# (.NET) | Java |
|------|-----------|------|
| **API Testing** | RestSharp + SpecFlow/Reqnroll | RestAssured + Cucumber |
| **Web Testing** | Selenium WebDriver / Playwright | Selenium WebDriver |
| **Mobile Testing** | Appium | Appium |
| **Desktop Testing** | WinAppDriver | WinAppDriver |

**Generated Project Includes:**
- Pre-configured package references (SpecFlow/Reqnroll for C#, Cucumber for Java)
- Sample feature file with example scenario
- Sample step definition class
- `.gitignore` file for proper version control
- Build configuration ready for Gheetah execution

### 📁 Project Management

| Feature | Description |
|---------|-------------|
| **Add Projects** | Via remote repository clone or local ZIP upload |
| **Required Formats** | Java (TestNG/JUnit with Cucumber) or .NET (xUnit with Reqnroll/SpecFlow) |
| **Build Projects** | Mandatory step before test execution |
| **Delete Projects** | Admin/Lead only, blocked during active execution |
| **Project Settings** | Configure environment variables and execution preferences |
| **Test Suites** | Organize scenarios into logical groups |

### 🧪 Test Execution

| Feature | Description |
|---------|-------------|
| **Scenario Discovery** | Automatic scanning of `.feature` files with unique tag assignment |
| **Tag Filtering** | Run specific scenarios by selecting their generated tag |
| **Agent Selection** | Choose remote agent or execute locally |
| **Real-time Output** | SignalR streaming of execution logs |
| **HTML Reports** | Detailed test execution reports |
| **Hangfire Integration** | Background job tracking for "Run All Scenarios" |

### 📊 Dashboard

| Widget Type | Description |
|-------------|-------------|
| **Azure Pipeline Charts** | Bar charts of test results (requires Azure CI/CD integration) |
| **Azure Test Result Table** | Detailed test result tables |
| **Recent Scenario Execution** | Latest individual scenario runs |
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
| **Email** | SMTP, SendGrid, Azure Communication | Configuration ready (sending planned) |

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
3. Choose your **Language** (C# or Java)
4. Select a **Project Template** (API, Web, Mobile, Desktop)
5. Enter a **Project Name** and optional **Description**
6. Click **"Create Project"**

### Quick Start: Open and Edit a Feature File

1. From the **Project List**, click on your project to open the IDE
2. In the **Solution Explorer** (left sidebar), navigate to your feature files
3. Click any `.feature` file to open it in the editor
4. Edit the scenario steps with full syntax highlighting
5. Press **Ctrl+Click** on any Given/When/Then step → automatically navigates to the step definition
6. Press **Ctrl+S** to save changes
7. Modified files show a **red dot** indicator and "dirty" state

### Quick Start: Commit and Create Pull Request

1. Make changes to files (dirty indicator appears on modified files)
2. Switch to the **Git** tab in the left sidebar
3. Select or create a branch from the dropdown (cannot push directly to main/master)
4. Click the **Commit** button
5. Enter a branch name if creating a new branch
6. After successful push, go to **Push History** panel at the bottom
7. Find your push and click the **PR** button
8. Select **Target Branch** (e.g., main, develop)
9. Add **Reviewers** (Admin or Lead users)
10. Write a **Description** explaining your changes
11. Click **"Create Pull Request"**

### Quick Start: Review and Merge a PR

1. Open the PR from push history or direct link (`/Editor/PRDetails/{id}`)
2. Review the **Overview** tab for pipeline status and approvals
3. Go to the **Files** tab to see changed files
4. Click a file to view the diff side-by-side
5. **Add inline comments** by clicking the gutter area (left of line numbers)
6. Resolve comments by changing their status from "Active" to "Resolved"
7. Approve or reject the PR using buttons in the top-right
8. Creator clicks **"Complete Merge"** to start the pipeline
9. Pipeline runs automatically: Build Source → Merge → Build Target
10. If conflicts appear, go to **Conflicts** tab and resolve using Ours/Theirs buttons
11. After successful merge, remote repository is automatically synced

### Running Tests

1. From **Project List**, click **Manage** on your project
2. Click **Build** (required before first execution)
3. Select individual scenarios from the tree or list view
4. Click **"Run Options"** to configure:
   - Select the generated tag for specific scenario
   - Choose an Agent (or leave empty for local execution)
5. Click **"Run Scenario"** or **"Run All Scenarios"**
6. Monitor real-time output in the modal window via SignalR
7. View detailed HTML reports after execution completes

### Agent Management

1. Navigate to the **Agents** section from the top navigation bar
2. Download `GheetahAgentSetup.exe` for Windows agents
3. Install and run the agent on target machines
4. Agent appears as **"Pending Request"** in the agent list
5. Administrator approves the request → agent becomes active
6. Select the agent when running scenarios for remote execution

### Dashboard Customization

1. Click the **gear icon** (<i class="ti ti-settings"></i>) on the dashboard
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
