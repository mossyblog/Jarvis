import { Page, Locator } from '@playwright/test';
import { TestHelpers } from '../utils/test-helpers';

export class BentoPage {
  readonly page: Page;
  readonly bentoGrid: Locator;
  readonly componentPalette: Locator;
  readonly gridOverlay: Locator;
  readonly componentTiles: Locator;
  readonly dropZones: Locator;
  readonly editModeToggle: Locator;
  readonly layoutSelector: Locator;
  readonly deviceSelector: Locator;
  readonly gridItems: Locator;

  constructor(page: Page) {
    this.page = page;
    this.bentoGrid = page.locator('[data-testid="bento-grid"], .bento-grid');
    this.componentPalette = page.locator('[data-testid="component-palette"], .component-palette');
    this.gridOverlay = page.locator('[data-testid="grid-overlay"], .grid-overlay');
    this.componentTiles = page.locator('[data-testid="component-tile"], .component-tile');
    this.dropZones = page.locator('[data-testid="drop-zone"], .drop-zone');
    this.editModeToggle = page.locator('[data-testid="edit-mode-toggle"]');
    this.layoutSelector = page.locator('[data-testid="layout-selector"]');
    this.deviceSelector = page.locator('[data-testid="device-selector"]');
    this.gridItems = page.locator('[data-testid="grid-item"], .grid-item');
  }

  async navigateToBento() {
    await this.page.goto('/bento');
    await TestHelpers.waitForNetworkIdle(this.page);
    await this.page.waitForSelector('[data-testid="bento-grid"], .bento-grid');
  }

  async enableEditMode() {
    if (await this.editModeToggle.isVisible()) {
      const isActive = await this.editModeToggle.getAttribute('data-active');
      if (isActive !== 'true') {
        await this.editModeToggle.click();
        await this.page.waitForTimeout(500);
      }
    }
  }

  async disableEditMode() {
    if (await this.editModeToggle.isVisible()) {
      const isActive = await this.editModeToggle.getAttribute('data-active');
      if (isActive === 'true') {
        await this.editModeToggle.click();
        await this.page.waitForTimeout(500);
      }
    }
  }

  async isEditModeEnabled(): Promise<boolean> {
    if (await this.editModeToggle.isVisible()) {
      const isActive = await this.editModeToggle.getAttribute('data-active');
      return isActive === 'true';
    }
    return false;
  }

  async getAvailableComponents(): Promise<string[]> {
    await this.enableEditMode();
    const tiles = await this.componentTiles.all();
    const components = [];
    
    for (const tile of tiles) {
      const text = await tile.textContent();
      if (text) components.push(text.trim());
    }
    
    return components;
  }

  async dragComponentToGrid(componentName: string, targetGridPosition?: { row: number, col: number }) {
    await this.enableEditMode();
    
    // Find the component tile
    const componentTile = this.page.locator(`[data-testid="component-tile"]:has-text("${componentName}"), .component-tile:has-text("${componentName}")`);
    await TestHelpers.waitForStableElement(this.page, componentTile.locator('.').first());
    
    // Find target drop zone
    let targetZone;
    if (targetGridPosition) {
      targetZone = this.page.locator(`[data-grid-row="${targetGridPosition.row}"][data-grid-col="${targetGridPosition.col}"]`);
    } else {
      // Use first available drop zone
      targetZone = this.dropZones.first();
    }
    
    await TestHelpers.waitForStableElement(this.page, targetZone.locator('.').first());
    
    // Perform drag and drop
    await TestHelpers.dragAndDrop(
      this.page,
      componentTile.locator('.').first(),
      targetZone.locator('.').first()
    );
    
    // Wait for component to be placed
    await this.page.waitForTimeout(1000);
  }

  async moveGridItem(fromPosition: { row: number, col: number }, toPosition: { row: number, col: number }) {
    await this.enableEditMode();
    
    const sourceItem = this.page.locator(`[data-grid-row="${fromPosition.row}"][data-grid-col="${fromPosition.col}"] .grid-item`);
    const targetZone = this.page.locator(`[data-grid-row="${toPosition.row}"][data-grid-col="${toPosition.col}"]`);
    
    await TestHelpers.dragAndDrop(
      this.page,
      sourceItem.locator('.').first(),
      targetZone.locator('.').first()
    );
    
    await this.page.waitForTimeout(1000);
  }

  async resizeGridItem(position: { row: number, col: number }, newSize: { width: number, height: number }) {
    await this.enableEditMode();
    
    const gridItem = this.page.locator(`[data-grid-row="${position.row}"][data-grid-col="${position.col}"] .grid-item`);
    await gridItem.hover();
    
    // Look for resize handle
    const resizeHandle = gridItem.locator('.resize-handle, [data-testid="resize-handle"]');
    if (await resizeHandle.isVisible()) {
      const handle = await resizeHandle.boundingBox();
      if (handle) {
        await this.page.mouse.move(handle.x + handle.width / 2, handle.y + handle.height / 2);
        await this.page.mouse.down();
        await this.page.mouse.move(
          handle.x + newSize.width * 50, // Approximate pixel per grid unit
          handle.y + newSize.height * 50,
          { steps: 5 }
        );
        await this.page.mouse.up();
        await this.page.waitForTimeout(500);
      }
    }
  }

  async deleteGridItem(position: { row: number, col: number }) {
    await this.enableEditMode();
    
    const gridItem = this.page.locator(`[data-grid-row="${position.row}"][data-grid-col="${position.col}"] .grid-item`);
    await gridItem.hover();
    
    // Look for delete button
    const deleteButton = gridItem.locator('.delete-button, [data-testid="delete-button"], button:has-text("Delete")');
    if (await deleteButton.isVisible()) {
      await deleteButton.click();
      await this.page.waitForTimeout(500);
    } else {
      // Try right-click context menu
      await gridItem.click({ button: 'right' });
      const contextDelete = this.page.locator('.context-menu button:has-text("Delete"), .context-menu [data-action="delete"]');
      if (await contextDelete.isVisible()) {
        await contextDelete.click();
      }
    }
  }

  async selectLayout(layoutName: string) {
    if (await this.layoutSelector.isVisible()) {
      await this.layoutSelector.click();
      const option = this.page.locator(`option:has-text("${layoutName}"), [data-value="${layoutName}"]`);
      await option.click();
      await this.page.waitForTimeout(500);
    }
  }

  async selectDevice(deviceName: 'desktop' | 'tablet' | 'mobile') {
    if (await this.deviceSelector.isVisible()) {
      await this.deviceSelector.click();
      const option = this.page.locator(`button:has-text("${deviceName}"), [data-device="${deviceName}"]`);
      await option.click();
      await this.page.waitForTimeout(500);
    }
  }

  async getGridLayout(): Promise<Array<{ row: number, col: number, width: number, height: number, component: string }>> {
    const items = await this.gridItems.all();
    const layout = [];
    
    for (const item of items) {
      const row = await item.getAttribute('data-grid-row');
      const col = await item.getAttribute('data-grid-col');
      const width = await item.getAttribute('data-grid-width');
      const height = await item.getAttribute('data-grid-height');
      const component = await item.textContent();
      
      layout.push({
        row: parseInt(row || '0'),
        col: parseInt(col || '0'),
        width: parseInt(width || '1'),
        height: parseInt(height || '1'),
        component: component?.trim() || ''
      });
    }
    
    return layout;
  }

  async isGridOverlayVisible(): Promise<boolean> {
    return await this.gridOverlay.isVisible();
  }

  async saveLayout() {
    const saveButton = this.page.locator('button:has-text("Save"), [data-testid="save-layout"]');
    if (await saveButton.isVisible()) {
      await saveButton.click();
      await TestHelpers.waitForNetworkIdle(this.page);
    }
  }

  async previewLayout() {
    await this.disableEditMode();
    await this.page.waitForTimeout(500);
  }

  async getComponentCount(): Promise<number> {
    const items = await this.gridItems.all();
    return items.length;
  }
}