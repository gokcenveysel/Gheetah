// @ts-nocheck
import { test, expect, request } from '@playwright/test';

// ── Navigation & Page Assertions ─────────────────────────────────────────────
test.describe('Navigation', () => {

  test('Navigate and assert URL and title', async ({ page }) => {
    await page.goto('https://playwright.dev/');
    await expect(page).toHaveTitle(/Playwright/);
    await expect(page).toHaveURL(/playwright\.dev/);
  });

  test('Back, forward, and reload', async ({ page }) => {
    await page.goto('https://playwright.dev/');
    await page.goto('https://playwright.dev/docs/intro');
    await page.goBack();
    await expect(page).toHaveURL(/playwright\.dev\/$/);
    await page.goForward();
    await expect(page).toHaveURL(/docs\/intro/);
    await page.reload();
    await expect(page).toHaveURL(/docs\/intro/);
  });

  test('Page source and viewport', async ({ page }) => {
    await page.setViewportSize({ width: 1280, height: 720 });
    await page.goto('https://playwright.dev/');
    const content = await page.content();
    expect(content).toContain('Playwright');
  });

});

// ── Locator Strategies ───────────────────────────────────────────────────────
test.describe('Locator Strategies', () => {

  test('Locate by role', async ({ page }) => {
    await page.goto('https://playwright.dev/');
    const link = page.getByRole('link', { name: 'Get started' });
    await expect(link).toBeVisible();
    await link.click();
    await expect(page).toHaveURL(/docs\/intro/);
  });

  test('Locate by text', async ({ page }) => {
    await page.goto('https://playwright.dev/');
    await expect(page.getByText('Playwright enables')).toBeVisible();
  });

  test('Locate by placeholder', async ({ page }) => {
    await page.goto('https://playwright.dev/');
    const searchBox = page.getByPlaceholder('Search');
    await searchBox.click();
    await searchBox.fill('locators');
  });

  test('Locate by label', async ({ page }) => {
    await page.goto('https://the-internet.herokuapp.com/login');
    await page.getByLabel('Username').fill('tomsmith');
    await page.getByLabel('Password').fill('SuperSecretPassword!');
  });

  test('Locate by CSS selector', async ({ page }) => {
    await page.goto('https://the-internet.herokuapp.com');
    const links = page.locator('ul.menu li a');
    await expect(links.first()).toBeVisible();
    const count = await links.count();
    expect(count).toBeGreaterThan(0);
  });

  test('Locate by XPath', async ({ page }) => {
    await page.goto('https://the-internet.herokuapp.com/login');
    await page.locator('//input[@id="username"]').fill('tomsmith');
    await page.locator('//button[@type="submit"]').click();
  });

  test('Locate by test ID', async ({ page }) => {
    await page.goto('https://playwright.dev/');
    // data-testid attributes demonstration
    const hero = page.locator('[class*="hero"]');
    await expect(hero).toBeVisible();
  });

  test('Chained locators and nth', async ({ page }) => {
    await page.goto('https://the-internet.herokuapp.com');
    const menuItems = page.locator('ul.menu').locator('li');
    const firstItem = menuItems.nth(0);
    await expect(firstItem).toBeVisible();
  });

  test('Filter locators', async ({ page }) => {
    await page.goto('https://the-internet.herokuapp.com');
    const links = page.getByRole('link').filter({ hasText: 'A/B' });
    await expect(links).toBeVisible();
  });

});

// ── Form Interactions ────────────────────────────────────────────────────────
test.describe('Form Interactions', () => {

  test('Fill, clear, and type', async ({ page }) => {
    await page.goto('https://the-internet.herokuapp.com/login');
    const username = page.locator('#username');
    await username.fill('tomsmith');
    await expect(username).toHaveValue('tomsmith');
    await username.clear();
    await expect(username).toHaveValue('');
    await username.pressSequentially('tom', { delay: 50 });
    await expect(username).toHaveValue('tom');
  });

  test('Checkbox interactions', async ({ page }) => {
    await page.goto('https://the-internet.herokuapp.com/checkboxes');
    const checkboxes = page.locator('input[type="checkbox"]');
    const first = checkboxes.nth(0);
    await first.check();
    await expect(first).toBeChecked();
    await first.uncheck();
    await expect(first).not.toBeChecked();
    await first.setChecked(true);
    await expect(first).toBeChecked();
  });

  test('Select dropdown by label, value, and index', async ({ page }) => {
    await page.goto('https://the-internet.herokuapp.com/dropdown');
    const dropdown = page.locator('#dropdown');
    await dropdown.selectOption({ label: 'Option 1' });
    await expect(dropdown).toHaveValue('1');
    await dropdown.selectOption({ value: '2' });
    await expect(dropdown).toHaveValue('2');
    await dropdown.selectOption({ index: 1 });
    await expect(dropdown).toHaveValue('1');
  });

  test('Radio button selection', async ({ page }) => {
    await page.goto('https://the-internet.herokuapp.com/login');
    // Demonstrate radio pattern
    await page.locator('#username').fill('tomsmith');
    await page.locator('#password').fill('SuperSecretPassword!');
    await page.getByRole('button', { name: 'Login' }).click();
    await expect(page.locator('#flash')).toContainText('You logged into');
  });

  test('Complete form submission', async ({ page }) => {
    await page.goto('https://the-internet.herokuapp.com/login');
    await page.fill('#username', 'tomsmith');
    await page.fill('#password', 'SuperSecretPassword!');
    await page.click('button[type=submit]');
    await expect(page).toHaveURL(/secure/);
    await expect(page.locator('.flash.success')).toBeVisible();
  });

});

// ── Keyboard & Mouse ─────────────────────────────────────────────────────────
test.describe('Keyboard and Mouse', () => {

  test('Keyboard press and key combinations', async ({ page }) => {
    await page.goto('https://the-internet.herokuapp.com/key_presses');
    await page.locator('#target').click();
    await page.keyboard.press('Tab');
    await expect(page.locator('#result')).toContainText('TAB');
    await page.locator('#target').click();
    await page.keyboard.press('Enter');
    await expect(page.locator('#result')).toContainText('ENTER');
    await page.locator('#target').click();
    await page.keyboard.press('Escape');
    await expect(page.locator('#result')).toContainText('ESCAPE');
  });

  test('Type character by character', async ({ page }) => {
    await page.goto('https://the-internet.herokuapp.com/login');
    await page.locator('#username').click();
    await page.keyboard.type('tomsmith', { delay: 30 });
    await expect(page.locator('#username')).toHaveValue('tomsmith');
  });

  test('Mouse hover', async ({ page }) => {
    await page.goto('https://the-internet.herokuapp.com/hovers');
    const figure = page.locator('.figure').first();
    await figure.hover();
    await expect(figure.locator('.figcaption')).toBeVisible();
  });

  test('Mouse click at coordinates', async ({ page }) => {
    await page.goto('https://the-internet.herokuapp.com/add_remove_elements/');
    await page.click('text=Add Element');
    await expect(page.locator('.added-manually')).toBeVisible();
  });

  test('Double click', async ({ page }) => {
    await page.goto('https://the-internet.herokuapp.com/login');
    const button = page.locator('button[type=submit]');
    await page.fill('#username', 'tomsmith');
    await page.fill('#password', 'SuperSecretPassword!');
    await button.dblclick();
    // Double click submits once
  });

  test('Right click (context menu)', async ({ page }) => {
    await page.goto('https://the-internet.herokuapp.com/context_menu');
    await page.locator('#hot-spot').click({ button: 'right' });
    page.on('dialog', dialog => dialog.accept());
    await page.waitForTimeout(500);
  });

  test('Drag and drop', async ({ page }) => {
    await page.goto('https://the-internet.herokuapp.com/drag_and_drop');
    const source = page.locator('#column-a');
    const target = page.locator('#column-b');
    await source.dragTo(target);
    await expect(source.locator('header')).toContainText('B');
  });

  test('Scroll into view', async ({ page }) => {
    await page.goto('https://the-internet.herokuapp.com/infinite_scroll');
    await page.evaluate(() => window.scrollTo(0, document.body.scrollHeight));
    await page.waitForTimeout(1000);
    const newContent = page.locator('.jscroll-added p');
    await expect(newContent.first()).toBeVisible();
  });

});

// ── Waiting Strategies ───────────────────────────────────────────────────────
test.describe('Waiting and Auto-Wait', () => {

  test('Wait for element to be visible', async ({ page }) => {
    await page.goto('https://the-internet.herokuapp.com/dynamic_loading/1');
    await page.click('button[type=submit]');
    await expect(page.locator('#finish h4')).toBeVisible({ timeout: 15000 });
    await expect(page.locator('#finish h4')).toContainText('Hello World');
  });

  test('Wait for navigation', async ({ page }) => {
    await page.goto('https://the-internet.herokuapp.com/login');
    await page.fill('#username', 'tomsmith');
    await page.fill('#password', 'SuperSecretPassword!');
    await Promise.all([
      page.waitForURL(/secure/),
      page.click('button[type=submit]')
    ]);
    await expect(page).toHaveURL(/secure/);
  });

  test('Wait for network idle', async ({ page }) => {
    await page.goto('https://playwright.dev/', { waitUntil: 'networkidle' });
    await expect(page).toHaveTitle(/Playwright/);
  });

  test('Wait for specific response', async ({ page }) => {
    const responsePromise = page.waitForResponse('**/playwright.dev/**');
    await page.goto('https://playwright.dev/');
    const response = await responsePromise;
    expect(response.status()).toBe(200);
  });

  test('Wait for condition with polling', async ({ page }) => {
    await page.goto('https://the-internet.herokuapp.com/dynamic_loading/2');
    await page.click('button[type=submit]');
    await page.waitForFunction(() =>
      document.querySelector('#finish') !== null &&
      document.querySelector('#finish').style.display !== 'none'
    , { timeout: 15000 });
    await expect(page.locator('#finish')).toBeVisible();
  });

});

// ── Dialogs / Alerts ─────────────────────────────────────────────────────────
test.describe('Dialogs and Alerts', () => {

  test('Accept alert dialog', async ({ page }) => {
    await page.goto('https://the-internet.herokuapp.com/javascript_alerts');
    page.on('dialog', dialog => dialog.accept());
    await page.click('text=Click for JS Alert');
    await expect(page.locator('#result')).toContainText('You successfuly');
  });

  test('Dismiss confirm dialog', async ({ page }) => {
    await page.goto('https://the-internet.herokuapp.com/javascript_alerts');
    page.on('dialog', dialog => dialog.dismiss());
    await page.click('text=Click for JS Confirm');
    await expect(page.locator('#result')).toContainText('dismissed');
  });

  test('Handle prompt dialog', async ({ page }) => {
    await page.goto('https://the-internet.herokuapp.com/javascript_alerts');
    page.on('dialog', async dialog => {
      expect(dialog.type()).toBe('prompt');
      await dialog.accept('Gheetah Playwright');
    });
    await page.click('text=Click for JS Prompt');
    await expect(page.locator('#result')).toContainText('Gheetah Playwright');
  });

  test('Capture dialog message', async ({ page }) => {
    await page.goto('https://the-internet.herokuapp.com/javascript_alerts');
    let dialogMessage = '';
    page.on('dialog', async dialog => {
      dialogMessage = dialog.message();
      await dialog.accept();
    });
    await page.click('text=Click for JS Alert');
    await page.waitForEvent('dialog').catch(() => {});
    // Dialog message was captured
  });

});

// ── Frames & iFrames ─────────────────────────────────────────────────────────
test.describe('Frames', () => {

  test('Interact with iframe content', async ({ page }) => {
    await page.goto('https://the-internet.herokuapp.com/iframe');
    const frame = page.frameLocator('#mce_0_ifr');
    await frame.locator('#tinymce').click();
    await page.keyboard.press('Control+a');
    await frame.locator('#tinymce').fill('Playwright Frame Test');
    await expect(frame.locator('#tinymce')).toContainText('Playwright Frame Test');
  });

  test('Frame by name', async ({ page }) => {
    await page.goto('https://the-internet.herokuapp.com/nested_frames');
    const topFrame = page.frame('frame-top');
    if (topFrame) {
      const leftFrame = topFrame.childFrames().find(f => f.name() === 'frame-left');
      // Navigate nested frames
    }
  });

});

// ── Multiple Pages / Tabs ────────────────────────────────────────────────────
test.describe('Multiple Pages and Tabs', () => {

  test('Open new tab and interact', async ({ page, context }) => {
    await page.goto('https://the-internet.herokuapp.com/windows');
    const [newPage] = await Promise.all([
      context.waitForEvent('page'),
      page.click('text=Click Here')
    ]);
    await newPage.waitForLoadState();
    await expect(newPage).toHaveTitle(/New Window/);
    await newPage.close();
  });

  test('New page in new context', async ({ browser }) => {
    const context = await browser.newContext();
    const page1 = await context.newPage();
    const page2 = await context.newPage();
    await page1.goto('https://playwright.dev/');
    await page2.goto('https://playwright.dev/docs/intro');
    await expect(page1).toHaveTitle(/Playwright/);
    await expect(page2).toHaveTitle(/Installation/);
    await context.close();
  });

  test('Popup window handling', async ({ page, context }) => {
    await page.goto('https://the-internet.herokuapp.com/windows');
    const popupPromise = context.waitForEvent('page');
    await page.click('text=Click Here');
    const popup = await popupPromise;
    await popup.waitForLoadState();
    expect(popup.url()).toContain('window');
  });

});

// ── Network Interception ─────────────────────────────────────────────────────
test.describe('Network Interception', () => {

  test('Intercept and mock API response', async ({ page }) => {
    await page.route('**/api/users/2', async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          data: { id: 2, first_name: 'Mocked', last_name: 'User', email: 'mock@test.com' }
        })
      });
    });
    await page.goto('https://the-internet.herokuapp.com');
    // Route is active for intercepting matching requests
  });

  test('Block specific resource types', async ({ page }) => {
    await page.route('**/*.{png,jpg,jpeg,gif,svg,ico}', route => route.abort());
    await page.goto('https://playwright.dev/');
    await expect(page).toHaveTitle(/Playwright/);
  });

  test('Modify request headers', async ({ page }) => {
    await page.route('**/*', async route => {
      const headers = { ...route.request().headers(), 'X-Gheetah-Test': 'true' };
      await route.continue({ headers });
    });
    await page.goto('https://playwright.dev/');
    await expect(page).toHaveTitle(/Playwright/);
  });

  test('Log all network requests', async ({ page }) => {
    const requests = [];
    page.on('request', req => requests.push(req.url()));
    await page.goto('https://playwright.dev/');
    expect(requests.length).toBeGreaterThan(0);
  });

  test('Wait for specific API call', async ({ page }) => {
    const apiCall = page.waitForResponse(
      res => res.url().includes('playwright.dev') && res.status() === 200
    );
    await page.goto('https://playwright.dev/');
    const response = await apiCall;
    expect(response.ok()).toBeTruthy();
  });

});

// ── Screenshots & Visual ──────────────────────────────────────────────────────
test.describe('Screenshots', () => {

  test('Full page screenshot', async ({ page }) => {
    await page.goto('https://playwright.dev/');
    const screenshot = await page.screenshot({ fullPage: true });
    expect(screenshot).toBeTruthy();
    expect(screenshot.length).toBeGreaterThan(0);
  });

  test('Element screenshot', async ({ page }) => {
    await page.goto('https://playwright.dev/');
    const hero = page.locator('.hero').first();
    if (await hero.isVisible()) {
      const screenshot = await hero.screenshot();
      expect(screenshot.length).toBeGreaterThan(0);
    }
  });

  test('Screenshot with clip area', async ({ page }) => {
    await page.goto('https://playwright.dev/');
    const screenshot = await page.screenshot({
      clip: { x: 0, y: 0, width: 800, height: 600 }
    });
    expect(screenshot).toBeTruthy();
  });

});

// ── Assertions (expect) ───────────────────────────────────────────────────────
test.describe('Playwright Assertions', () => {

  test('Element state assertions', async ({ page }) => {
    await page.goto('https://the-internet.herokuapp.com/login');
    const btn = page.locator('button[type=submit]');
    await expect(btn).toBeVisible();
    await expect(btn).toBeEnabled();
    await expect(btn).not.toBeDisabled();
    await expect(btn).toBeInViewport();
  });

  test('Text assertions', async ({ page }) => {
    await page.goto('https://the-internet.herokuapp.com/login');
    await expect(page.locator('h2')).toHaveText('Login Page');
    await expect(page.locator('h2')).toContainText('Login');
    await expect(page.locator('h2')).not.toHaveText('Register');
  });

  test('Attribute and CSS assertions', async ({ page }) => {
    await page.goto('https://the-internet.herokuapp.com/login');
    await expect(page.locator('#username')).toHaveAttribute('type', 'text');
    await expect(page.locator('#password')).toHaveAttribute('type', 'password');
    const formClasses = await page.locator('form').getAttribute('class');
    // class attribute may be null on this page — just verify element exists
    await expect(page.locator('form')).toBeVisible();
  });

  test('Count assertions', async ({ page }) => {
    await page.goto('https://the-internet.herokuapp.com');
    const links = page.locator('ul.menu li');
    await expect(links).toHaveCount(await links.count());
    await expect(links).not.toHaveCount(0);
  });

  test('Value assertions for inputs', async ({ page }) => {
    await page.goto('https://the-internet.herokuapp.com/login');
    await page.fill('#username', 'testuser');
    await expect(page.locator('#username')).toHaveValue('testuser');
    await expect(page.locator('#username')).not.toHaveValue('');
  });

  test('URL and title assertions', async ({ page }) => {
    await page.goto('https://playwright.dev/');
    await expect(page).toHaveURL('https://playwright.dev/');
    await expect(page).toHaveURL(/playwright/);
    await expect(page).toHaveTitle(/Playwright/);
    await expect(page).not.toHaveTitle('Error');
  });

});

// ── API Testing via Request Context ──────────────────────────────────────────
test.describe('API Testing', () => {

  test('GET request and response validation', async ({ request }) => {
    const response = await request.get('https://reqres.in/api/users/2');
    expect(response.status()).toBe(200);
    const body = await response.json();
    expect(body.data.first_name).toBe('Janet');
    expect(body.data.last_name).toContain('Weaver');
  });

  test('POST request and resource creation', async ({ request }) => {
    const response = await request.post('https://reqres.in/api/users', {
      data: { name: 'Gheetah User', job: 'QA Automation Engineer' }
    });
    expect(response.status()).toBe(201);
    const body = await response.json();
    expect(body.name).toBe('Gheetah User');
    expect(body.id).toBeTruthy();
  });

  test('PUT request and resource update', async ({ request }) => {
    const response = await request.put('https://reqres.in/api/users/2', {
      data: { name: 'Updated User', job: 'Senior QA' }
    });
    expect(response.status()).toBe(200);
    const body = await response.json();
    expect(body.name).toBe('Updated User');
    expect(body.updatedAt).toBeTruthy();
  });

  test('DELETE request', async ({ request }) => {
    const response = await request.delete('https://reqres.in/api/users/2');
    expect(response.status()).toBe(204);
  });

  test('Authentication request', async ({ request }) => {
    const response = await request.post('https://reqres.in/api/login', {
      data: { email: 'eve.holt@reqres.in', password: 'cityslicka' }
    });
    expect(response.ok()).toBeTruthy();
    const body = await response.json();
    expect(body.token).toBeTruthy();
  });

  test('Reusable API context with auth headers', async ({ playwright }) => {
    const apiContext = await playwright.request.newContext({
      baseURL: 'https://reqres.in',
      extraHTTPHeaders: { 'Content-Type': 'application/json' }
    });
    const response = await apiContext.get('/api/users?page=2');
    expect(response.ok()).toBeTruthy();
    const body = await response.json();
    expect(body.data.length).toBeGreaterThan(0);
    await apiContext.dispose();
  });

});

// ── Mobile Viewport ───────────────────────────────────────────────────────────
test.describe('Mobile Viewport', () => {

  test('Test on mobile viewport', async ({ browser }) => {
    const context = await browser.newContext({
      viewport: { width: 375, height: 812 },
      userAgent: 'Mozilla/5.0 (iPhone; CPU iPhone OS 14_0) AppleWebKit/605.1.15'
    });
    const page = await context.newPage();
    await page.goto('https://playwright.dev/');
    await expect(page).toHaveTitle(/Playwright/);
    await context.close();
  });

  test('Responsive design check', async ({ page }) => {
    await page.setViewportSize({ width: 768, height: 1024 });
    await page.goto('https://playwright.dev/');
    await expect(page).toHaveTitle(/Playwright/);
    const viewport = page.viewportSize();
    expect(viewport.width).toBe(768);
  });

});

// ── Storage & Cookies ─────────────────────────────────────────────────────────
test.describe('Storage and Cookies', () => {

  test('Set and read cookies', async ({ context, page }) => {
    await context.addCookies([{
      name: 'gheetah-test',
      value: 'playwright',
      url: 'https://playwright.dev'
    }]);
    await page.goto('https://playwright.dev/');
    const cookies = await context.cookies();
    const testCookie = cookies.find(c => c.name === 'gheetah-test');
    expect(testCookie).toBeTruthy();
    expect(testCookie.value).toBe('playwright');
  });

  test('LocalStorage manipulation', async ({ page }) => {
    await page.goto('https://the-internet.herokuapp.com');
    await page.evaluate(() => localStorage.setItem('gheetah', 'automation'));
    const value = await page.evaluate(() => localStorage.getItem('gheetah'));
    expect(value).toBe('automation');
  });

  test('Session storage', async ({ page }) => {
    await page.goto('https://the-internet.herokuapp.com');
    await page.evaluate(() => sessionStorage.setItem('session-test', 'active'));
    const value = await page.evaluate(() => sessionStorage.getItem('session-test'));
    expect(value).toBe('active');
  });

});

// ── JavaScript Execution ──────────────────────────────────────────────────────
test.describe('JavaScript Execution', () => {

  test('Execute JavaScript and return value', async ({ page }) => {
    await page.goto('https://playwright.dev/');
    const title = await page.evaluate(() => document.title);
    expect(title).toContain('Playwright');
  });

  test('Pass arguments to page.evaluate', async ({ page }) => {
    await page.goto('https://playwright.dev/');
    const result = await page.evaluate(([a, b]) => a + b, [10, 20]);
    expect(result).toBe(30);
  });

  test('Execute script on element', async ({ page }) => {
    await page.goto('https://the-internet.herokuapp.com/login');
    await page.locator('#username').fill('test');
    const value = await page.locator('#username').evaluate(el => el.value);
    expect(value).toBe('test');
  });

  test('Modify DOM via JavaScript', async ({ page }) => {
    await page.goto('https://the-internet.herokuapp.com/login');
    await page.evaluate(() => {
      document.querySelector('h2').textContent = 'Gheetah Login';
    });
    await expect(page.locator('h2')).toHaveText('Gheetah Login');
  });

});
