# Sakai Vue TypeScript

A Vue 3 TypeScript application template based on [Sakai](https://sakai.primevue.org/) and [PrimeVue](https://primevue.org/), using Bun as the package manager.

## Getting Started

### Prerequisites

- [Bun](https://bun.sh) version 1.0.0 or higher
- Node.js version 18.0.0 or higher

### Installation

First, install Bun if you haven't already:

```bash
curl -fsSL https://bun.sh/install | bash
```

Then, install the project dependencies:

```bash
bun install
```

### Project Setup

Before starting development, update the project details in `package.json`:

```json
{
  "name": "your-project-name",
  "version": "1.0.0",
  "type": "module"
}
```

Also update the title in `index.html` to match your project name:

```html
<title>Your Project Name</title>
```

### Updating Dependencies

There are two ways to update dependencies:

1. Using npm-check-updates:

```bash
# Install npm-check-updates globally
bun add -g npm-check-updates

# Check for possible updates
ncu

# Apply the updates to package.json
ncu -u

# Install the updated dependencies
bun install
```

2. Using the built-in update script:

```bash
# Update dependencies based on installed Bun packages
bun run update-deps
```

### Development

To start the development server:

```bash
bun run dev
```

### Build

To build for production:

```bash
bun run build
```

### Preview Production Build

To preview the production build:

```bash
bun run preview
```

### Type Checking

```bash
bun run type-check
```

### Linting

```bash
bun run lint
```

## Project Structure

The template is built with:

- [Vue 3](https://vuejs.org/)
- [Vite](https://vitejs.dev/)
- [TypeScript](https://www.typescriptlang.org/)
- [PrimeVue](https://primevue.org/)
- [Tailwind CSS](https://tailwindcss.com/)

Visit the [documentation](https://sakai.primevue.org/documentation) to learn more about the template features.
