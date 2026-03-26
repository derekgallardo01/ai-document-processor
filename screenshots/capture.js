const puppeteer = require('puppeteer');
const path = require('path');

const BASE = 'http://localhost:3000';
const OUT = __dirname;

async function delay(ms) {
  return new Promise(r => setTimeout(r, ms));
}

async function capture(page, name, action) {
  if (action) await action();
  await delay(800);
  await page.screenshot({ path: path.join(OUT, `${name}.png`), fullPage: false });
  console.log(`  ✓ ${name}.png`);
}

(async () => {
  const browser = await puppeteer.launch({
    headless: false,
    defaultViewport: { width: 1440, height: 900 },
    args: ['--start-maximized'],
  });

  const page = await browser.newPage();

  console.log('\n📸 Capturing screenshots...\n');

  // 1. Dashboard - Light Mode
  await page.goto(BASE, { waitUntil: 'networkidle2' });
  await delay(1500);
  await capture(page, '01-dashboard-light');

  // 2. Dashboard - Dark Mode
  await capture(page, '02-dashboard-dark', async () => {
    // Click the dark mode toggle (the button with moon/sun icon in topbar)
    const toggleBtn = await page.$('header button[title*="dark"], header button[title*="light"]');
    if (toggleBtn) await toggleBtn.click();
  });

  // 3. Upload page with type selector
  await capture(page, '03-upload-page', async () => {
    // Click Upload nav item
    const navButtons = await page.$$('nav button');
    for (const btn of navButtons) {
      const text = await btn.evaluate(el => el.textContent);
      if (text.includes('Upload')) { await btn.click(); break; }
    }
  });

  // 4. Documents page with filter
  await capture(page, '04-documents-list', async () => {
    const navButtons = await page.$$('nav button');
    for (const btn of navButtons) {
      const text = await btn.evaluate(el => el.textContent);
      if (text.includes('Documents')) { await btn.click(); break; }
    }
  });

  // 5. Document detail panel - click first document
  await capture(page, '05-document-detail', async () => {
    await delay(500);
    const docItems = await page.$$('[class*="cursor-pointer"][class*="flex"][class*="items-center"][class*="gap-3"][class*="px-3"][class*="py-3"]');
    if (docItems.length > 0) await docItems[0].click();
  });

  // 6. Line Items tab
  await capture(page, '06-line-items', async () => {
    const tabs = await page.$$('button');
    for (const tab of tabs) {
      const text = await tab.evaluate(el => el.textContent);
      if (text.includes('Line Items')) { await tab.click(); break; }
    }
  });

  // 7. Processing Log tab
  await capture(page, '07-processing-log', async () => {
    const tabs = await page.$$('button');
    for (const tab of tabs) {
      const text = await tab.evaluate(el => el.textContent);
      if (text.includes('Log')) { await tab.click(); break; }
    }
  });

  // 8. Close detail panel
  await page.keyboard.press('Escape');
  await delay(300);

  // 9. Compare page
  await capture(page, '08-compare-page', async () => {
    const navButtons = await page.$$('nav button');
    for (const btn of navButtons) {
      const text = await btn.evaluate(el => el.textContent);
      if (text.includes('Compare')) { await btn.click(); break; }
    }
  });

  // 10. Compare with documents selected (if 2+ docs exist)
  await capture(page, '09-compare-selected', async () => {
    const selects = await page.$$('select');
    if (selects.length >= 2) {
      // Select first option in each dropdown
      await selects[0].select(await selects[0].evaluate(el => {
        const opts = el.querySelectorAll('option');
        return opts.length > 1 ? opts[1].value : '';
      }));
      await delay(300);
      await selects[1].select(await selects[1].evaluate(el => {
        const opts = el.querySelectorAll('option');
        return opts.length > 2 ? opts[2].value : opts.length > 1 ? opts[1].value : '';
      }));
    }
  });

  // 11. Switch back to light mode for a final dashboard shot
  await capture(page, '10-dashboard-final', async () => {
    const navButtons = await page.$$('nav button');
    for (const btn of navButtons) {
      const text = await btn.evaluate(el => el.textContent);
      if (text.includes('Dashboard')) { await btn.click(); break; }
    }
    await delay(300);
    const toggleBtn = await page.$('header button[title*="dark"], header button[title*="light"]');
    if (toggleBtn) await toggleBtn.click();
  });

  console.log(`\n✅ Done! Screenshots saved to ${OUT}\n`);

  await browser.close();
})();
