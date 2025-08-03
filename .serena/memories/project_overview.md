# Jarvis Project Overview

## Purpose
Jarvis is an Entity Component System (ECS) SDK for .NET that provides a handler-based approach to building scalable, maintainable applications. It includes:
- A battle-tested ECS architecture
- Built-in security with JWT-based Row Level Security
- Multi-tenant support
- Audit trail integration

## Tech Stack
- **Backend**: .NET 8.0, C#, PostgreSQL
- **Frontend**: React 19, TypeScript, Vite, Tailwind CSS
- **API**: Azure Functions
- **Testing**: xUnit (backend), Vitest & Playwright (frontend)
- **UI Components**: Radix UI, shadcn/ui, Lucide icons
- **State Management**: React Query (TanStack Query)
- **Forms**: React Hook Form with Zod validation

## Project Structure
- `core.jarvis/` - Main ECS framework
- `core.jarvis.data/` - Low-level data access with JWT RLS
- `core.jarvis.api/` - REST API layer with Azure Functions
- `core.jarvis.ui.studio/` - React frontend application
- `core.jarvis.tests/` - Backend integration and unit tests
- `docs/` - TOGAF-structured architecture documentation

## Development Environment
- Running on WSL (Windows Subsystem for Linux)
- Use PowerShell.exe for Windows commands
- dotnet CLI available at `/usr/bin/dotnet`
- Node.js/npm for frontend development