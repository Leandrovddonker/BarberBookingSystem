import { build } from 'esbuild';
import { readdir } from 'node:fs/promises';
import { spawnSync } from 'node:child_process';
const testFiles = (await readdir('tests')).filter((file) =>
  file.endsWith('.test.ts'),
);
await build({
  entryPoints: testFiles.map((file) => `tests/${file}`),
  outdir: '.angular/tests',
  outExtension: { '.js': '.mjs' },
  bundle: true,
  platform: 'node',
  format: 'esm',
  packages: 'external',
});
const result = spawnSync(
  process.execPath,
  [
    '--test',
    ...testFiles.map(
      (file) => `.angular/tests/${file.replace(/\.ts$/, '.mjs')}`,
    ),
  ],
  { stdio: 'inherit' },
);
process.exitCode = result.status ?? 1;
