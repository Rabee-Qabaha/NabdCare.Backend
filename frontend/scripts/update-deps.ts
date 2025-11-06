import { execSync } from 'child_process';
import { readFileSync, writeFileSync } from 'fs';

// Read current package.json
const packageJson = JSON.parse(readFileSync('package.json', 'utf-8'));

// Get installed version using bun.lockb
const getDependencyVersion = (pkg: string) => {
  try {
    // Run bun install to ensure lockfile is up to date
    const output = execSync('bun install --no-save').toString();
    // Get version from node_modules package.json
    const pkgJson = JSON.parse(readFileSync(`node_modules/${pkg}/package.json`, 'utf-8'));
    return pkgJson.version || null;
  } catch (error) {
    console.error(`Error getting version for ${pkg}`);
    return null;
  }
};

// Update dependencies
console.log('Updating dependencies...');
for (const [pkg, _] of Object.entries(packageJson.dependencies || {})) {
  const version = getDependencyVersion(pkg);
  if (version) {
    console.log(`${pkg}: ${version}`);
    packageJson.dependencies[pkg] = `^${version}`;
  }
}

// Update devDependencies
console.log('\nUpdating devDependencies...');
for (const [pkg, _] of Object.entries(packageJson.devDependencies || {})) {
  const version = getDependencyVersion(pkg);
  if (version) {
    console.log(`${pkg}: ${version}`);
    packageJson.devDependencies[pkg] = `^${version}`;
  }
}

// Write updated package.json
writeFileSync('package.json', JSON.stringify(packageJson, null, 4) + '\n', 'utf-8');

console.log('\nDependencies updated successfully!');
