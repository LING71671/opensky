# Contributing to OpenSky

Thank you for your interest in contributing to OpenSky! This document provides guidelines and instructions for contributing.

## Development Setup

### Prerequisites
- Windows 10 / 11 (64-bit)
- Node.js >= 20.0.0
- pnpm (`npm install -g pnpm`)
- .NET Framework 4.8 (built-in on Windows 10/11)

### Getting Started

```bash
# Clone the repository
git clone https://github.com/LING71671/opensky.git
cd opensky

# Install dependencies
pnpm install

# Build the native helper and TypeScript
pnpm run build

# Run tests
pnpm test
```

## Project Structure

```
src/
├── adapters/       # MCP server, Anthropic bridge, Codex exporter
├── bin/            # CLI entry point
├── core/           # Platform-agnostic interfaces and models
├── driver/         # Transport layer and driver implementations
│   └── transport/  # IPC abstractions (stdio, named pipe)
└── native/         # C# source for Windows native helper
    ├── AccessibilityService.cs
    ├── CaptureService.cs
    ├── InputService.cs
    ├── OverlayModels.cs
    ├── OverlayRenderer.cs
    ├── OverlayService.cs
    ├── NativeTypes.cs
    └── WindowService.cs
```

## How to Contribute

### Reporting Bugs
- Use [GitHub Issues](https://github.com/LING71671/opensky/issues) to report bugs.
- Include OS version, Node.js version, and steps to reproduce.

### Suggesting Features
- Open an issue with the `enhancement` label.
- Describe the use case and expected behavior.

### Pull Requests
1. Fork the repository.
2. Create a feature branch: `git checkout -b feat/your-feature`.
3. Follow the existing code style and architecture patterns.
4. Ensure all tests pass: `pnpm test`.
5. Commit using [Conventional Commits](https://www.conventionalcommits.org/):
   - `feat:` for new features
   - `fix:` for bug fixes
   - `docs:` for documentation
   - `refactor:` for code refactoring
6. Push to your fork and open a Pull Request.

## Architecture Principles

- **Single Responsibility**: Each file should have one clear purpose.
- **Decoupled Layers**: Core interfaces → Transport → Platform Drivers → Adapters.
- **CLI-First**: Prioritize background/CLI-based automation. Visual/mouse interaction is secondary.
- **Accessibility-First**: Prefer UI Automation (Accessibility Tree) over screenshot-based interaction.

## Code Style

- TypeScript: ESM modules, strict mode.
- C#: .NET Framework 4.8 compatible, compiled via system `csc.exe`.
- Use meaningful variable and function names.
- Document public APIs with JSDoc / XML comments.

## License

By contributing, you agree that your contributions will be licensed under the [MIT License](./LICENSE).
