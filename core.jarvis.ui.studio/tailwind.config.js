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
  		fontFamily: {
  			sans: [
  				'var(--font-custom)'
  			],
  			custom: [
  				'var(--font-custom)'
  			],
  			mono: [
  				'var(--font-source-code-pro)'
  			]
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
    require('@tailwindcss/container-queries'),
  ],
}

