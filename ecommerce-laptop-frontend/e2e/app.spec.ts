import { expect, test } from '@playwright/test';

test.describe('Homepage', () => {
    test('should display the homepage correctly', async ({ page }) => {
        await page.goto('/');

        // Check if the page title is correct
        await expect(page).toHaveTitle(/LaptopStore/);

        // Check if the main navigation exists
        await expect(page.locator('nav')).toBeVisible();

        // Check if the hero section exists
        await expect(page.locator('h1')).toBeVisible();

        // Check if product cards are displayed
        await expect(page.locator('[data-testid="product-card"]')).toHaveCount(8);
    });

    test('should navigate to product detail page', async ({ page }) => {
        await page.goto('/');

        // Click on the first product card
        const firstProduct = page.locator('[data-testid="product-card"]').first();
        await firstProduct.click();

        // Should navigate to product detail page
        await expect(page).toHaveURL(/\/products\/.+/);
    });

    test('should be able to search for products', async ({ page }) => {
        await page.goto('/');

        // Find search input and type a query
        const searchInput = page.locator('input[placeholder*="Tm kim"]');
        await searchInput.fill('laptop gaming');
        await searchInput.press('Enter');

        // Should show search results or navigate to search page
        await expect(page).toHaveURL(/search/);
    });
});

test.describe('Authentication', () => {
    test('should display login page', async ({ page }) => {
        await page.goto('/auth/login');

        await expect(page).toHaveTitle(/ng nhp/);
        await expect(page.locator('h1')).toContainText('ng nhp');

        // Check for form elements
        await expect(page.locator('input[type="email"]')).toBeVisible();
        await expect(page.locator('input[type="password"]')).toBeVisible();
        await expect(page.locator('button[type="submit"]')).toBeVisible();
    });

    test('should display register page', async ({ page }) => {
        await page.goto('/auth/register');

        await expect(page).toHaveTitle(/Đăng ký/);
        await expect(page.locator('h1')).toContainText('Đăng ký');

        // Check for form elements
        await expect(page.locator('input[name="firstName"]')).toBeVisible();
        await expect(page.locator('input[name="lastName"]')).toBeVisible();
        await expect(page.locator('input[type="email"]')).toBeVisible();
        await expect(page.locator('input[type="password"]')).toBeVisible();
    });

    test('should show validation errors on invalid login', async ({ page }) => {
        await page.goto('/auth/login');

        // Try to submit without filling required fields
        await page.locator('button[type="submit"]').click();

        // Should show validation errors
        await expect(page.locator('text=Email l bt buc')).toBeVisible();
        await expect(page.locator('text=Mt khu l bt buc')).toBeVisible();
    });
});

test.describe('Shopping Cart', () => {
    test('should add product to cart', async ({ page }) => {
        await page.goto('/');

        // Click add to cart on first product
        const addToCartButton = page.locator('[data-testid="add-to-cart"]').first();
        await addToCartButton.click();

        // Should show cart notification or update cart count
        await expect(page.locator('[data-testid="cart-count"]')).toContainText('1');
    });

    test('should display cart sidebar', async ({ page }) => {
        await page.goto('/');

        // Add a product to cart first
        await page.locator('[data-testid="add-to-cart"]').first().click();

        // Click cart icon to open sidebar
        await page.locator('[data-testid="cart-toggle"]').click();

        // Should show cart sidebar
        await expect(page.locator('[data-testid="cart-sidebar"]')).toBeVisible();
        await expect(page.locator('[data-testid="cart-item"]')).toHaveCount(1);
    });

    test('should navigate to checkout page', async ({ page }) => {
        await page.goto('/');

        // Add a product to cart
        await page.locator('[data-testid="add-to-cart"]').first().click();

        // Open cart and proceed to checkout
        await page.locator('[data-testid="cart-toggle"]').click();
        await page.locator('[data-testid="checkout-button"]').click();

        // Should navigate to checkout page
        await expect(page).toHaveURL('/checkout');
        await expect(page.locator('h1')).toContainText('Thanh toán');
    });
});

test.describe('Responsive Design', () => {
    test('should work on mobile devices', async ({ page }) => {
        // Set mobile viewport
        await page.setViewportSize({ width: 375, height: 667 });
        await page.goto('/');

        // Check if mobile navigation works
        const mobileMenuButton = page.locator('[data-testid="mobile-menu-toggle"]');
        if (await mobileMenuButton.isVisible()) {
            await mobileMenuButton.click();
            await expect(page.locator('[data-testid="mobile-menu"]')).toBeVisible();
        }

        // Check if product cards are responsive
        await expect(page.locator('[data-testid="product-card"]')).toBeVisible();
    });

    test('should work on tablet devices', async ({ page }) => {
        // Set tablet viewport
        await page.setViewportSize({ width: 768, height: 1024 });
        await page.goto('/');

        // Check if content is properly displayed
        await expect(page.locator('nav')).toBeVisible();
        await expect(page.locator('[data-testid="product-card"]')).toBeVisible();
    });
});

test.describe('PWA Features', () => {
    test('should work offline', async ({ page, context }) => {
        await page.goto('/');

        // Wait for service worker to be registered
        await page.waitForTimeout(2000);

        // Go offline
        await context.setOffline(true);

        // Navigate to a cached page
        await page.reload();

        // Should still display content
        await expect(page.locator('h1')).toBeVisible();
    });

    test('should show install prompt', async ({ page }) => {
        await page.goto('/');

        // Check if PWA installer component is present
        const pwaInstaller = page.locator('[data-testid="pwa-installer"]');
        if (await pwaInstaller.isVisible()) {
            await expect(pwaInstaller).toContainText('Cài đặt ứng dụng');
        }
    });
});
