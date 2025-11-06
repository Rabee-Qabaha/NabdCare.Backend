// scripts/syncPermissions.ts
import chalk from 'chalk';
import fs from 'fs';
import path from 'path';

const CONSTANTS_DIR = path.resolve('src/types/backend/constants');
const OUTPUT_FILE = path.resolve('src/config/permissionsRegistry.ts');

function toPascalCase(name: string) {
  return name
    .split(/[-_]/)
    .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
    .join('');
}

const files = fs
  .readdirSync(CONSTANTS_DIR)
  .filter((f) => f.endsWith('.ts') && !f.endsWith('.d.ts'));

if (!files.length) {
  console.log(chalk.red('❌ No constant files found.'));
  process.exit(1);
}

const imports: string[] = [];
const exports: string[] = [];

for (const file of files) {
  const base = file.replace('.ts', '');
  const className = toPascalCase(base);
  imports.push(`import { ${className} } from "@/types/backend/constants/${base}";`);
  exports.push(`  ${className},`);
}

const content = `/** AUTO-GENERATED FILE — DO NOT EDIT MANUALLY */
${imports.join('\n')}

export const PermissionRegistry = {
${exports.join('\n')}
} as const;
`;

fs.writeFileSync(OUTPUT_FILE, content);
console.log(chalk.green('✅ Permission Registry synced successfully!'));
