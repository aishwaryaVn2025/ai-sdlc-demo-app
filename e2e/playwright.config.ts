import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './tests',
  workers: 1,
  retries: process.env.CI ? 1 : 0,
  reporter: [['list'], ['html', { open: 'never' }]],
  use: {
    baseURL: 'http://localhost:5000',
    trace: 'on-first-retry',
  },
  projects: [
  {
    name: 'edge',
    use: {
      ...devices['Desktop Chrome'],
      channel: 'msedge',
    },
  },
],
  webServer: {
    command: 'dotnet run --project ../backend/src/InvoiceApp.Api',
    url: 'http://localhost:5000',
    reuseExistingServer: true,
    timeout: 120_000,
  },
});
