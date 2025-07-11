import { COMPONENT_SPACING } from '../../utils/spacing';

/**
 * Example component demonstrating proper spacing usage
 */
export function SpacingExample() {
  return (
    <div className={COMPONENT_SPACING.page.padding}>
      {/* Page-level container with 24px padding */}
      
      <section className={COMPONENT_SPACING.page.section}>
        {/* Section with 32px bottom margin */}
        <h2 className="text-2xl font-semibold mb-md">Spacing Example</h2>
        
        {/* Card with proper spacing */}
        <div className={`${COMPONENT_SPACING.card.padding} bg-card rounded-lg border`}>
          <h3 className="text-lg font-medium mb-sm">Card Title</h3>
          <p className="text-muted-foreground mb-md">
            This card uses our standard card padding (16px).
          </p>
          
          {/* Button group with consistent gaps */}
          <div className={`flex ${COMPONENT_SPACING.card.gap}`}>
            <button className={`${COMPONENT_SPACING.button.md} bg-primary text-primary-foreground rounded`}>
              Medium Button
            </button>
            <button className={`${COMPONENT_SPACING.button.sm} bg-secondary text-secondary-foreground rounded`}>
              Small Button
            </button>
          </div>
        </div>
      </section>

      {/* Form example */}
      <section className={COMPONENT_SPACING.page.section}>
        <h3 className="text-lg font-medium mb-sm">Form Example</h3>
        <form className={`flex flex-col ${COMPONENT_SPACING.form.gap}`}>
          <input
            type="text"
            placeholder="Name"
            className={`${COMPONENT_SPACING.form.inputPadding} border rounded`}
          />
          <input
            type="email"
            placeholder="Email"
            className={`${COMPONENT_SPACING.form.inputPadding} border rounded`}
          />
          <button className={`${COMPONENT_SPACING.button.md} bg-primary text-primary-foreground rounded self-start`}>
            Submit
          </button>
        </form>
      </section>

      {/* Grid example with t-shirt sizes */}
      <section>
        <h3 className="text-lg font-medium mb-sm">T-Shirt Size Examples</h3>
        <div className="flex gap-md items-end">
          <div className="p-xs bg-muted rounded">XS (4px)</div>
          <div className="p-sm bg-muted rounded">SM (8px)</div>
          <div className="p-md bg-muted rounded">MD (16px)</div>
          <div className="p-lg bg-muted rounded">LG (24px)</div>
          <div className="p-xl bg-muted rounded">XL (32px)</div>
        </div>
      </section>
    </div>
  );
}