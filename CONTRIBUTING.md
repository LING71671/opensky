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
├── core/               # Platform-agnostic domain contracts and models
│   ├── contracts/      # Segregated interfaces (IMouseDriver, ISemanticDriver, etc.)
│   └── models/         # Segregated data models (geometry, semantic, vision, input)
├── driver/             # Driver implementations
│   ├── transport/      # IPC abstractions (stdio, named pipe)
│   └── windows/        # Windows sub-drivers (mouse, keyboard, semantic, vision)
├── native/             # C# micro-services organized into functional subdirectories
│   ├── Common/         # Win32 structures and constants
│   ├── Input/          # Physical hardware input simulation
│   ├── Semantic/       # UIA traversal and in-memory pattern invocation
│   ├── Vision/         # Screen capture
│   ├── Window/         # Window management and isolation
│   ├── Overlay/        # Layered hardware composition & Spring physics
│   └── Host/           # Daemon entry point
├── adapters/           # MCP server, Anthropic bridge, Codex exporter
└── bin/                # CLI entry point
```

## Development Guidelines

Please strictly follow our **[Development & Architecture Guidelines](./docs/DEVELOPMENT.md)** before submitting code:
- **Hierarchical Over Flat**: Never dump files into a single flat directory. Use meaningful subdirectories.
- **Single Responsibility (SRP)**: Each file must do one thing (recommended 50-200 lines). Each function must do one thing (recommended 10-40 lines).
- **Zero-Cursor Priority**: Standard controls must use `invoke_element` (in-memory patterns) to avoid stealing user mouse focus.
- **Blind Actions**: Mouse gestures (scroll, drag) must run independently without screenshot overhead.
- **Apple Frosted Monochrome & Spring Physics**: All animations must use 2nd-order harmonic spring damping (`SpringPhysics.Evaluate`).

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
3. Follow the rules in [docs/DEVELOPMENT.md](./docs/DEVELOPMENT.md).
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
