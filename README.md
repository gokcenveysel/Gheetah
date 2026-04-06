<img width="110" height="128" alt="image" src="https://github.com/user-attachments/assets/86382633-41b0-4c7d-aed9-a9b68106a1df" />

# Gheetah - Test Orchestration Platform

![License](https://img.shields.io/badge/License-MIT-blue.svg)
![Status](https://img.shields.io/badge/Status-Open--Source-brightgreen)

Gheetah is an open-source test orchestration platform designed to streamline the execution and management of BDD (Behavior-Driven Development) test automation projects.

## Table of Contents
- [Introduction](#introduction)
- [System Requirements](#system-requirements)
- [Installation](#installation)
- [Features](#features)
- [Usage](#usage)
- [Support](#support)
- [License](#license)

## Introduction

Gheetah is a powerful Test Orchestration Platform with:
- Cross-platform .NET 8.0 MVC backend
- Modern frontend designed using Tabler.io
- Support for BDD test projects in C# (Reqnroll/SpecFlow) or Java (TestNG-Cucumber/JUnit-Cucumber)
- No database dependency - stores data as JSON files

### Key Features
- **Seamless Test Execution**: Run tests remotely via Gheetah Agent or locally
- **Enterprise-Ready**: Customizable dashboard, user/role management, CI/CD integrations
- **Lightweight**: Simple JSON-based storage in /Data directory

## System Requirements

### Gheetah Server
- **OS**: Windows Server 2016/2019/2022 or Linux (RHEL 8+, Ubuntu 20.04+)
- **CPU**: 4 cores (8+ recommended)
- **RAM**: 8GB minimum (16GB recommended)
- **Storage**: 5GB available space (SSD recommended)
- **Software**: .NET 8.0, ASP.NET Core

### Execution Agent
- **OS**: Windows 10/11 or Linux (x64)
- **CPU**: 2 cores minimum (4+ recommended)
- **RAM**: 4GB minimum
- **Runtime**: .NET 8.0, Java 11+

## Installation

1. **First Installation Wizard**:
   - Select SSO provider (Azure AD, Google Workspace, or No SSO)
   - Define user groups and permissions
   - Configure project folder location (critical for functionality)

2. **Agent Setup**:
   - Download and run `GheetahAgentSetup.exe`
   - Register agent with Gheetah server URL
   - Admin approves pending request

## Features

### Core Capabilities
- **Test Execution**:
  - Run specific scenarios or entire suites
  - Tag filtering support
  - Real-time results via SignalR
- **Project Management**:
  - Add projects via remote clone or local upload
  - Required formats: Java (TestNG/JUnit with Cucumber) or .NET (xUnit with Reqnroll/SpecFlow)
- **Dashboard**:
  - Customizable widgets
  - CI/CD integration visualizations

### Integrations
- **CI/CD**: Azure DevOps, Jenkins, GitLab
- **SSO**: Azure AD, Google Workspace
- **Email**: SMTP, SendGrid, Azure Communication (planned)

## Usage

### Running Tests
1. Add projects via clone/upload
2. Build projects (required before execution)
3. Execute:
   - Specific scenarios with tag selection
   - Entire test suites
4. Monitor real-time results

### Agent Management
- Windows agents: Pre-built installer available
- Linux agents: Manual setup required
- Pending requests require admin approval

## Support

For professional support regarding:
- Test automation strategies
- Gheetah implementation
- Custom development and integration

Contact:
- [LinkedIn](https://linkedin.com/in/veyselgokcen/)
- Email: [gheetahinfo@gmail.com](mailto:gheetahinfo@gmail.com)

## License

# Gheetah Community License

**Copyright (c) 2026 Veysel Gökçen**

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to use, copy, modify, and merge the software for **non-commercial purposes**, subject to the following conditions:

---

### 1. NON-COMMERCIAL USE ONLY
The software may not be used for commercial purposes, sold, or integrated into a paid product/service without prior written permission from the copyright holder.

**Definition of Commercial Use:**
For the purposes of this license, "Commercial Use" includes, but is not limited to:
* Offering the software as a service (SaaS).
* Bundling or integrating the software into a paid package, product, or tool.
* Direct resale or redistribution of the software for a fee.
* Using the software to provide paid consultancy or managed testing services to third parties.

### 2. AUDIENCE
The software is intended for free use by individual developers and QA engineers for testing, automation development, and educational purposes.

### 3. ATTRIBUTION
The original copyright notice and this permission notice must be included in all copies or substantial portions of the Software.

### 4. SECURITY & LIABILITY
**THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND.** Users are solely responsible for performing their own security assessments before deployment. The authors shall not be liable for any claims, damages, or other liability arising from the use of the software.
