# Gheetah - Test Orchestration Platform

<img width="110" height="128" alt="image" src="https://github.com/user-attachments/assets/86382633-41b0-4c7d-aed9-a9b68106a1df" />


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
- Email: [gokcenveysel@gmail.com](mailto:gokcenveysel@gmail.com)
- [GitHub Repository](https://github.com/gokcenveysel/Gheetah)

## License

Gheetah is released under the MIT License with important considerations:
- Complete open-source codebase
- Strictly non-commercial use only
- No warranty or liability for untested features
- Users should perform their own security assessments
