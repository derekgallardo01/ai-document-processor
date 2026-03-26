const puppeteer = require('puppeteer');
const path = require('path');

const BASE = 'http://localhost:3000';
const OUT = __dirname;

async function delay(ms) {
  return new Promise(r => setTimeout(r, ms));
}

(async () => {
  const browser = await puppeteer.launch({
    headless: false,
    defaultViewport: { width: 1440, height: 900 },
  });

  const page = await browser.newPage();
  console.log('\n📸 Capturing compare screenshots...\n');

  await page.goto(BASE, { waitUntil: 'networkidle2' });
  await delay(1500);

  // Navigate to Compare page
  const navButtons = await page.$$('nav button');
  for (const btn of navButtons) {
    const text = await btn.evaluate(el => el.textContent);
    if (text.includes('Compare')) { await btn.click(); break; }
  }
  await delay(1000);

  // Select first document in left dropdown
  const selects = await page.$$('select');
  if (selects.length >= 2) {
    // Get all option values from left select
    const leftOptions = await selects[0].evaluate(el => {
      return Array.from(el.querySelectorAll('option'))
        .filter(o => o.value)
        .map(o => ({ value: o.value, text: o.textContent }));
    });
    console.log('  Left options:', leftOptions.map(o => o.text).join(', '));

    const rightOptions = await selects[1].evaluate(el => {
      return Array.from(el.querySelectorAll('option'))
        .filter(o => o.value)
        .map(o => ({ value: o.value, text: o.textContent }));
    });
    console.log('  Right options:', rightOptions.map(o => o.text).join(', '));

    // Pick an invoice for left, a receipt for right (or first two different docs)
    const invoiceOpt = leftOptions.find(o => o.text.includes('Invoice')) || leftOptions[0];
    const receiptOpt = rightOptions.find(o => o.text.includes('Receipt')) || rightOptions[rightOptions.length > 1 ? 1 : 0];

    if (invoiceOpt) {
      await selects[0].select(invoiceOpt.value);
      console.log(`  Selected left: ${invoiceOpt.text}`);
    }
    await delay(500);

    if (receiptOpt) {
      await selects[1].select(receiptOpt.value);
      console.log(`  Selected right: ${receiptOpt.text}`);
    }
    await delay(1000);

    await page.screenshot({ path: path.join(OUT, '09-compare-selected.png') });
    console.log('  ✓ 09-compare-selected.png');

    // Scroll down to see the comparison table
    await page.evaluate(() => {
      const main = document.querySelector('main');
      if (main) main.scrollTo(0, 400);
    });
    await delay(500);
    await page.screenshot({ path: path.join(OUT, '09b-compare-table.png') });
    console.log('  ✓ 09b-compare-table.png');
  } else {
    console.log('  ⚠ Could not find select dropdowns');
  }

  // Also capture the empty compare page
  await page.screenshot({ path: path.join(OUT, '08-compare-page.png') });
  console.log('  ✓ 08-compare-page.png');

  console.log('\n✅ Done!\n');
  await browser.close();
})();
