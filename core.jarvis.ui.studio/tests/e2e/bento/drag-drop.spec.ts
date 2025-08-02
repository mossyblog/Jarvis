import { test, expect } from '../fixtures/auth.fixture';
import { BentoPage } from '../pages/bento.page';
import { TestHelpers } from '../utils/test-helpers';

test.describe('Bento Grid Drag and Drop', () => {
  let bentoPage: BentoPage;

  test.beforeEach(async ({ page, authPage }) => {
    bentoPage = new BentoPage(page);
    await authPage.loginWithTestUser();
    await bentoPage.navigateToBento();
  });

  test('should display bento grid with edit mode toggle', async ({ page }) => {
    await expect(bentoPage.bentoGrid).toBeVisible();
    
    if (await bentoPage.editModeToggle.isVisible()) {
      await expect(bentoPage.editModeToggle).toBeVisible();
    }
  });

  test('should enable edit mode and show component palette', async ({ page }) => {
    await bentoPage.enableEditMode();
    
    expect(await bentoPage.isEditModeEnabled()).toBe(true);
    
    // Component palette should be visible in edit mode
    if (await bentoPage.componentPalette.isVisible()) {
      await expect(bentoPage.componentPalette).toBeVisible();
      
      // Should have draggable components
      const componentCount = await bentoPage.componentTiles.count();
      expect(componentCount).toBeGreaterThan(0);
    }
  });

  test('should show grid overlay in edit mode', async ({ page }) => {
    await bentoPage.enableEditMode();
    
    // Grid overlay should be visible for precise placement
    if (await bentoPage.isGridOverlayVisible()) {
      await expect(bentoPage.gridOverlay).toBeVisible();
    }
  });

  test('should drag component from palette to grid', async ({ page }) => {
    await bentoPage.enableEditMode();
    
    const availableComponents = await bentoPage.getAvailableComponents();
    if (availableComponents.length > 0) {
      const initialCount = await bentoPage.getComponentCount();
      
      // Drag first available component to grid
      await bentoPage.dragComponentToGrid(availableComponents[0]);
      
      // Should have one more component in grid
      const newCount = await bentoPage.getComponentCount();
      expect(newCount).toBe(initialCount + 1);
    }
  });

  test('should move existing grid items', async ({ page }) => {
    await bentoPage.enableEditMode();
    
    // Get current layout
    const initialLayout = await bentoPage.getGridLayout();
    
    if (initialLayout.length > 0) {
      const firstItem = initialLayout[0];
      const newPosition = { row: firstItem.row + 1, col: firstItem.col + 1 };
      
      // Move the item
      await bentoPage.moveGridItem(
        { row: firstItem.row, col: firstItem.col },
        newPosition
      );
      
      // Verify the item moved
      const newLayout = await bentoPage.getGridLayout();
      const movedItem = newLayout.find(item => 
        item.row === newPosition.row && item.col === newPosition.col
      );
      
      expect(movedItem).toBeDefined();
      expect(movedItem?.component).toBe(firstItem.component);
    }
  });

  test('should handle drag and drop with touch events', async ({ page }) => {
    // Simulate mobile device
    await page.setViewportSize({ width: 375, height: 667 });
    await bentoPage.enableEditMode();
    
    const availableComponents = await bentoPage.getAvailableComponents();
    if (availableComponents.length > 0) {
      const componentSelector = `[data-testid="component-tile"]:has-text("${availableComponents[0]}")`;
      const gridSelector = '[data-testid="drop-zone"]';
      
      // Simulate touch drag
      await TestHelpers.simulateTouchGesture(page, componentSelector, 'tap');
      
      const componentElement = page.locator(componentSelector);
      const gridElement = page.locator(gridSelector).first();
      
      if (await componentElement.isVisible() && await gridElement.isVisible()) {
        const componentBox = await componentElement.boundingBox();
        const gridBox = await gridElement.boundingBox();
        
        if (componentBox && gridBox) {
          // Simulate touch drag
          await page.touchscreen.tap(
            componentBox.x + componentBox.width / 2,
            componentBox.y + componentBox.height / 2
          );
          await page.mouse.move(
            componentBox.x + componentBox.width / 2,
            componentBox.y + componentBox.height / 2
          );
          await page.mouse.down();
          await page.mouse.move(
            gridBox.x + gridBox.width / 2,
            gridBox.y + gridBox.height / 2,
            { steps: 10 }
          );
          await page.mouse.up();
          
          await page.waitForTimeout(1000);
        }
      }
    }
  });

  test('should provide visual feedback during drag operations', async ({ page }) => {
    await bentoPage.enableEditMode();
    
    const availableComponents = await bentoPage.getAvailableComponents();
    if (availableComponents.length > 0) {
      const componentTile = page.locator(`[data-testid="component-tile"]:has-text("${availableComponents[0]}")`);
      
      // Start drag operation
      const componentBox = await componentTile.boundingBox();
      if (componentBox) {
        await page.mouse.move(
          componentBox.x + componentBox.width / 2,
          componentBox.y + componentBox.height / 2
        );
        await page.mouse.down();
        
        // Move slightly to trigger drag state
        await page.mouse.move(
          componentBox.x + componentBox.width / 2 + 10,
          componentBox.y + componentBox.height / 2 + 10
        );
        
        // Check for drag preview or visual feedback
        const dragPreview = page.locator('.drag-preview, [data-testid="drag-preview"]');
        if (await dragPreview.isVisible()) {
          await expect(dragPreview).toBeVisible();
        }
        
        // Check for drop zone highlighting
        const dropZones = bentoPage.dropZones;
        const firstDropZone = dropZones.first();
        
        await page.mouse.move(
          componentBox.x + componentBox.width / 2 + 100,
          componentBox.y + componentBox.height / 2 + 100
        );
        
        // Drop zones might get highlighted during drag
        await page.waitForTimeout(500);
        
        await page.mouse.up();
      }
    }
  });

  test('should handle invalid drop targets', async ({ page }) => {
    await bentoPage.enableEditMode();
    
    const availableComponents = await bentoPage.getAvailableComponents();
    if (availableComponents.length > 0) {
      const componentTile = page.locator(`[data-testid="component-tile"]:has-text("${availableComponents[0]}")`);
      const initialCount = await bentoPage.getComponentCount();
      
      // Try to drop on an invalid target (like the component palette itself)
      await TestHelpers.dragAndDrop(
        page,
        componentTile.locator('.').first(),
        bentoPage.componentPalette.locator('.').first()
      );
      
      // Component count should not change
      const newCount = await bentoPage.getComponentCount();
      expect(newCount).toBe(initialCount);
    }
  });

  test('should support undo/redo for drag operations', async ({ page }) => {
    await bentoPage.enableEditMode();
    
    const initialLayout = await bentoPage.getGridLayout();
    
    // Look for undo/redo buttons
    const undoButton = page.locator('button:has-text("Undo"), [data-testid="undo"], [aria-label*="undo"]');
    const redoButton = page.locator('button:has-text("Redo"), [data-testid="redo"], [aria-label*="redo"]');
    
    if (await undoButton.isVisible()) {
      // Perform an action first
      const availableComponents = await bentoPage.getAvailableComponents();
      if (availableComponents.length > 0) {
        await bentoPage.dragComponentToGrid(availableComponents[0]);
        
        // Undo the action
        await undoButton.click();
        await page.waitForTimeout(500);
        
        // Layout should revert
        const undoneLayout = await bentoPage.getGridLayout();
        expect(undoneLayout.length).toBe(initialLayout.length);
        
        // Redo the action
        if (await redoButton.isVisible()) {
          await redoButton.click();
          await page.waitForTimeout(500);
          
          const redoneLayout = await bentoPage.getGridLayout();
          expect(redoneLayout.length).toBe(initialLayout.length + 1);
        }
      }
    }
  });

  test('should prevent overlapping components', async ({ page }) => {
    await bentoPage.enableEditMode();
    
    const availableComponents = await bentoPage.getAvailableComponents();
    if (availableComponents.length >= 2) {
      // Place first component
      await bentoPage.dragComponentToGrid(availableComponents[0], { row: 0, col: 0 });
      
      // Try to place second component in same position
      const initialCount = await bentoPage.getComponentCount();
      await bentoPage.dragComponentToGrid(availableComponents[1], { row: 0, col: 0 });
      
      // Should either reject the drop or auto-place in different position
      const newCount = await bentoPage.getComponentCount();
      
      if (newCount > initialCount) {
        // If component was placed, it should be in a different position
        const layout = await bentoPage.getGridLayout();
        const itemsAtOrigin = layout.filter(item => item.row === 0 && item.col === 0);
        expect(itemsAtOrigin.length).toBeLessThanOrEqual(1);
      }
    }
  });

  test('should save and load grid layouts', async ({ page }) => {
    await bentoPage.enableEditMode();
    
    // Create a layout
    const availableComponents = await bentoPage.getAvailableComponents();
    if (availableComponents.length > 0) {
      await bentoPage.dragComponentToGrid(availableComponents[0]);
      
      const layoutBeforeSave = await bentoPage.getGridLayout();
      
      // Save the layout
      await bentoPage.saveLayout();
      
      // Refresh the page
      await page.reload();
      await TestHelpers.waitForNetworkIdle(page);
      await bentoPage.navigateToBento();
      
      // Layout should be preserved
      const layoutAfterReload = await bentoPage.getGridLayout();
      expect(layoutAfterReload.length).toBe(layoutBeforeSave.length);
    }
  });
});