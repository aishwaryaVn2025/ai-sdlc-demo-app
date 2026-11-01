import { expect, test } from '@playwright/test';

test('Manager-like approver is rejected when approving an invoice above 50,000 but below 500,000', async ({ page }) => {
  await page.goto('/');

  await page.locator('#username').fill('demo.approver');
  await page.locator('#password').fill('ChangeMe123!');
  await page.getByRole('button', { name: 'Sign in' }).click();

  await expect(page.locator('#whoami')).toHaveText('demo.approver (Approver)');

  const customerName = 'Manager Over Limit Invoice';
  await page.locator('#customer').fill(customerName);
  await page.locator('#amount').fill('50001');
  await page.getByRole('button', { name: 'Create invoice' }).click();

  const tableRow = page.locator('#invoices-table tbody tr').filter({ hasText: customerName }).first();
  await expect(tableRow).toContainText(customerName);

  await tableRow.getByRole('button', { name: /Submit invoice/i }).click();
  await expect(tableRow).toContainText('Pending Approval');

  await page.getByRole('tab', { name: 'Approvals' }).click();
  const pendingItem = page.locator('#pending-list li').filter({ hasText: customerName }).first();
  await expect(pendingItem).toBeVisible();
  await expect(pendingItem).toContainText(customerName);
  await expect(pendingItem).toContainText('$50,001.00');

  await pendingItem.getByRole('button', { name: /Approve invoice/i }).click();

  const decisionError = page.locator('#decision-error');
  await expect(decisionError).toBeVisible();
await expect(decisionError).toContainText('Approval limit exceeded');
await expect(decisionError).toContainText('50,001');
await expect(decisionError).toContainText('50,000');
  await expect(decisionError).toContainText('50,000');

  await page.getByRole('tab', { name: 'Invoices' }).click();
  const refreshedRow = page.locator('#invoices-table tbody tr').filter({ hasText: customerName }).first();
  await expect(refreshedRow).toContainText(customerName);
  await expect(refreshedRow).toContainText('Pending Approval');
  await expect(refreshedRow).not.toContainText('Approved');
  await expect(refreshedRow).not.toContainText('Rejected');

  const session = await page.evaluate(() => JSON.parse(sessionStorage.getItem('session') || 'null'));
  const response = await page.request.get('http://localhost:5000/api/invoices', {
    headers: { Authorization: `Bearer ${session.token}` },
  });

  expect(response.ok()).toBeTruthy();
  const invoices = await response.json();
  const invoice = invoices.find((entry: { customerName: string; status: string; amount: number; decidedBy?: string | null; decisionNote?: string | null; approvalHistory?: unknown[] }) => entry.customerName === customerName);

  expect(invoice).toBeTruthy();
  expect(invoice.status).toBe('PendingApproval');
  expect(invoice.amount).toBe(50001);
  expect(invoice.decidedBy ?? null).toBeNull();
  expect(invoice.decisionNote ?? null).toBeNull();
  expect(invoice.approvalHistory ?? []).toHaveLength(0);
});
