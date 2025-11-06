// scripts/validateRoutePermissions.ts
import chalk from 'chalk';
import fs from 'fs';
import path from 'path';
import { PermissionRegistry } from '../src/config/permissionsRegistry';

const ROUTER_DIR = path.resolve('src/router');
const REPORT_DIR = path.resolve('dist/reports');
const REPORT_FILE = path.join(REPORT_DIR, 'permissions-report.html');

// ---------- Utils ----------
function loadRouteFiles(dir: string): string[] {
  const files: string[] = [];
  for (const entry of fs.readdirSync(dir)) {
    const full = path.join(dir, entry);
    if (fs.statSync(full).isDirectory()) files.push(...loadRouteFiles(full));
    else if (entry.endsWith('.ts')) files.push(full);
  }
  return files;
}

function ensureDir(p: string) {
  if (!fs.existsSync(p)) fs.mkdirSync(p, { recursive: true });
}

// ---------- Load Permission Keys ----------
const knownKeys = Object.values(PermissionRegistry)
  .flatMap((cls: any) => Object.values(cls))
  .filter((v) => typeof v === 'string');

console.log(chalk.cyan(`🔍 Loaded ${knownKeys.length} permission keys from registry.`));
if (!knownKeys.length) {
  console.error(chalk.red('❌ No permissions found. Run npm run sync:permissions first.'));
  process.exit(1);
}

// ---------- Scan Routes ----------
const routeFiles = loadRouteFiles(ROUTER_DIR);
const results: {
  file: string;
  permission?: string;
  isPublic?: boolean;
  status: 'ok' | 'missing' | 'invalid' | 'publicWithPerm';
}[] = [];

for (const file of routeFiles) {
  const content = fs.readFileSync(file, 'utf8');
  const matches = [...content.matchAll(/meta:\s*{([^}]+)}/g)];

  for (const match of matches) {
    const meta = match[1];
    const perm = meta.match(/permission:\s*["'`](.*?)["'`]/)?.[1];
    const isPublic = meta.match(/public:\s*(true|false)/)?.[1] === 'true';

    if (isPublic && perm)
      results.push({
        file,
        permission: perm,
        isPublic,
        status: 'publicWithPerm',
      });
    else if (!isPublic && !perm) results.push({ file, isPublic, status: 'missing' });
    else if (perm && !knownKeys.includes(perm))
      results.push({ file, permission: perm, isPublic, status: 'invalid' });
    else results.push({ file, permission: perm, isPublic, status: 'ok' });
  }
}

// ---------- Console Summary ----------
const invalid = results.filter((r) => r.status === 'invalid').length;
const missing = results.filter((r) => r.status === 'missing').length;
const publicPerm = results.filter((r) => r.status === 'publicWithPerm').length;

console.log('\n-------------------------------------');
console.log(chalk.bold(`🧾 Total Routes Scanned: ${routeFiles.length}`));
console.log(chalk.yellow(`⚠️ Missing Permissions: ${missing}`));
console.log(chalk.red(`❌ Invalid Permissions: ${invalid}`));
console.log(chalk.magenta(`🚫 Public Routes with Permissions: ${publicPerm}`));
console.log(chalk.green(`✅ Registry Size: ${knownKeys.length}`));
console.log('-------------------------------------');

// ---------- Generate HTML Report ----------
ensureDir(REPORT_DIR);
const rows = results
  .map(
    (r) => `
      <tr class="${r.status}">
        <td>${path.relative(process.cwd(), r.file)}</td>
        <td>${r.permission || '-'}</td>
        <td>${r.isPublic ? '✅' : '❌'}</td>
        <td>${r.status}</td>
      </tr>`,
  )
  .join('\n');

const html = `<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="UTF-8" />
<title>Route Permissions Report</title>
<style>
body { font-family: Inter, Arial, sans-serif; background: #fafafa; color: #222; padding: 2rem; }
h1 { margin-bottom: 1rem; }
table { width: 100%; border-collapse: collapse; }
th, td { border: 1px solid #ddd; padding: 8px; }
th { background: #f0f0f0; text-align: left; }
tr.ok td { background: #e6ffe6; }
tr.missing td { background: #fffbe6; }
tr.invalid td { background: #ffe6e6; }
tr.publicWithPerm td { background: #f9e6ff; }
</style>
</head>
<body>
<h1>Route Permissions Validation Report</h1>
<p>Generated: ${new Date().toLocaleString()}</p>
<table>
<thead><tr><th>Route File</th><th>Permission</th><th>Public</th><th>Status</th></tr></thead>
<tbody>${rows}</tbody>
</table>
</body>
</html>`;

fs.writeFileSync(REPORT_FILE, html);
console.log(chalk.greenBright(`📄 Report saved to ${REPORT_FILE}`));

// ---------- Exit Status ----------
if (invalid || missing || publicPerm) {
  console.error(chalk.redBright('🚫 Validation failed! Some routes have permission issues.'));
  process.exit(1);
}
console.log(chalk.greenBright('✅ All route permissions validated successfully!'));
