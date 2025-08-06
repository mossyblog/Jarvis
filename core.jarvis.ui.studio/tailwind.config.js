/** @type {import('tailwindcss').Config} */
export default {
  darkMode: ["class"],
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  theme: {
    container: {
      center: true,
      padding: '2rem',
      screens: {
        '2xl': '1400px'
      }
    },
    extend: {
      spacing: {
        // Base unit: 8px
        'unit': '8px',
        // T-shirt sizes (multiples of 8)
        'xs': '8px',   // 1 * 8
        'sm': '16px',  // 2 * 8
        'md': '24px',  // 3 * 8
        'lg': '32px',  // 4 * 8
        'xl': '40px',  // 5 * 8
        '2xl': '48px', // 6 * 8
        '3xl': '64px', // 8 * 8
        '4xl': '128px', // 16 * 8
        '5xl': '160px', // 20 * 8
        '6xl': '256px', // 32 * 8
        '7xl': '320px', // 40 * 8
        '8xl': '384px'  // 48 * 8

      },
      fontSize: {
        // Font sizes following 8px grid where possible
        'xs': ['12px', '16px'],
        'sm': ['14px', '20px'],
        'base': ['16px', '24px'],
        'lg': ['18px', '28px'],
        'xl': ['20px', '28px'],
        '2xl': ['24px', '32px'],
        '3xl': ['30px', '36px'],
        '4xl': ['36px', '40px'],
        '5xl': ['48px', '48px'],
        '6xl': ['60px', '60px'],
        '7xl': ['72px', '72px'],
        '8xl': ['96px', '96px'],
        '9xl': ['128px', '128px'],
      },
      borderRadius: {
        'none': '0',
        'sm': '4px',
        'DEFAULT': '8px',
        'md': '8px',
        'lg': '12px',
        'xl': '16px',
        '2xl': '24px',
        '3xl': '32px',
        'full': '9999px',
      },
      minHeight: {
        // Min heights following 8px grid
        '8': '32px',
        '10': '40px',
        '12': '48px',
        '14': '56px',
        '16': '64px',
        '20': '80px',
        '24': '96px',
        '28': '112px',
        '32': '128px',
        '36': '144px',
        '40': '160px',
        '48': '192px',
        '56': '224px',
        '64': '256px',
        '72': '288px',
        '80': '320px',
        '96': '384px',
      },
      maxWidth: {
        // Max widths following 8px grid
        'xs': '320px',
        'sm': '384px',
        'md': '448px',
        'lg': '512px',
        'xl': '576px',
        '2xl': '672px',
        '3xl': '768px',
        '4xl': '896px',
        '5xl': '1024px',
        '6xl': '1152px',
        '7xl': '1280px',
      },
  		fontFamily: {
  			sans: 'var(--font-custom)',
  			custom: 'var(--font-custom)',
  			mono: 'var(--font-mono)'
  		},
  		colors: {
  			border: {
  				DEFAULT: 'hsl(var(--border))',
  				stronger: 'hsl(var(--border-stronger))'
  			},
  			input: 'hsl(var(--input))',
  			ring: 'hsl(var(--ring))',
  			background: 'hsl(var(--background))',
  			foreground: {
  				DEFAULT: 'hsl(var(--foreground))',
  				light: 'hsl(var(--foreground-light))',
  				lighter: 'hsl(var(--foreground-lighter))'
  			},
  			primary: {
  				DEFAULT: 'hsl(var(--primary))',
  				foreground: 'hsl(var(--primary-foreground))'
  			},
  			secondary: {
  				DEFAULT: 'hsl(var(--secondary))',
  				foreground: 'hsl(var(--secondary-foreground))'
  			},
  			destructive: {
  				DEFAULT: 'hsl(var(--destructive))',
  				foreground: 'hsl(var(--destructive-foreground))'
  			},
  			muted: {
  				DEFAULT: 'hsl(var(--muted))',
  				foreground: 'hsl(var(--muted-foreground))'
  			},
  			accent: {
  				DEFAULT: 'hsl(var(--accent))',
  				foreground: 'hsl(var(--accent-foreground))'
  			},
  			popover: {
  				DEFAULT: 'hsl(var(--popover))',
  				foreground: 'hsl(var(--popover-foreground))'
  			},
  			card: {
  				DEFAULT: 'hsl(var(--card))',
  				foreground: 'hsl(var(--card-foreground))'
  			},
  			'dash-sidebar': 'hsl(var(--dash-sidebar))',
  			'default': 'hsl(var(--default))',
  			'brand': 'hsl(var(--brand))',
  			'brand-600': 'hsl(var(--brand-600))',
  			'gray-100': 'hsl(var(--gray-100))',
  			'gray-200': 'hsl(var(--gray-200))',
  			'gray-300': 'hsl(var(--gray-300))',
  			'gray-400': 'hsl(var(--gray-400))',
  			'gray-500': 'hsl(var(--gray-500))',
  			'gray-600': 'hsl(var(--gray-600))',
  			'gray-700': 'hsl(var(--gray-700))',
  			'gray-800': 'hsl(var(--gray-800))',
  			'gray-900': 'hsl(var(--gray-900))',
  			sidebar: {
  				DEFAULT: 'hsl(var(--sidebar-background))',
  				foreground: 'hsl(var(--sidebar-foreground))',
  				primary: 'hsl(var(--sidebar-primary))',
  				'primary-foreground': 'hsl(var(--sidebar-primary-foreground))',
  				accent: 'hsl(var(--sidebar-accent))',
  				'accent-foreground': 'hsl(var(--sidebar-accent-foreground))',
  				border: 'hsl(var(--sidebar-border))',
  				ring: 'hsl(var(--sidebar-ring))'
  			}
  		},
  		borderRadius: {
  			lg: 'var(--radius)',
  			md: 'calc(var(--radius) - 2px)',
  			sm: 'calc(var(--radius) - 4px)'
  		},
  		keyframes: {
  			'accordion-down': {
  				from: {
  					height: '0'
  				},
  				to: {
  					height: 'var(--radix-accordion-content-height)'
  				}
  			},
  			'accordion-up': {
  				from: {
  					height: 'var(--radix-accordion-content-height)'
  				},
  				to: {
  					height: '0'
  				}
  			},
  			'fadeIn': {
  				from: {
  					opacity: '0',
  					transform: 'translateX(-10px)'
  				},
  				to: {
  					opacity: '1',
  					transform: 'translateX(0)'
  				}
  			}
  		},
  		animation: {
  			'accordion-down': 'accordion-down 0.2s ease-out',
  			'accordion-up': 'accordion-up 0.2s ease-out',
  			'fadeIn': 'fadeIn 0.3s ease-out'
  		},
  		transitionProperty: {
  			width: 'width'
  		},
  		width: {
  			'14': '3.5rem',
  			'64': '16rem'
  		}
  	}
  },
  plugins: [
    require('tailwindcss-animate'),
    require('@tailwindcss/container-queries'),
  ],
}
