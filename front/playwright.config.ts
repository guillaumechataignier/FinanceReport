import { defineConfig } from '@playwright/test';
import { mkdtempSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';

/**
 * Tests de bout en bout (Playwright) sur l'application en production locale : front compilé servi par l'API,
 * dossier de données vierge à chaque exécution, port dédié pour ne pas gêner une instance en cours.
 */
const port = 5099;
const dataRoot = mkdtempSync(join(tmpdir(), 'financereport-e2e-'));

export default defineConfig({
  testDir: './e2e',
  fullyParallel: false,
  workers: 1,
  timeout: 60_000,
  expect: { timeout: 10_000 },
  reporter: [['list']],
  use: {
    baseURL: `http://localhost:${port}`,
    channel: 'chrome',
    viewport: { width: 1440, height: 900 },
    locale: 'fr-FR',
    timezoneId: 'Europe/Paris',
    trace: 'retain-on-failure',
  },
  webServer: {
    command: 'bash ../scripts/run.sh',
    url: `http://localhost:${port}/api/auth/status`,
    reuseExistingServer: false,
    timeout: 240_000,
    env: {
      Api__Port: String(port),
      Storage__DataPath: join(dataRoot, 'data'),
      Logs__Path: join(dataRoot, 'logs'),
    },
  },
});
