/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {
      colors: {
        brand: {
          50: '#f0fdf4',
          100: '#dcfce7',
          200: '#bbf7d0',
          300: '#86efac',
          400: '#4ade80',
          500: '#22c55e',
          600: '#16a34a',
          700: '#15803d',
          800: '#166534',
          900: '#14532d',
        },
        pine: {
          50: '#f2f8f5',
          100: '#dceee6',
          200: '#bcded0',
          300: '#92c7b2',
          400: '#64aa91',
          500: '#448e75',
          600: '#34725e',
          700: '#2a5b4c',
          800: '#1f483c',
          900: '#0f382c',
          950: '#08211a',
        },
        mint: {
          DEFAULT: '#9FE3C5',
          light: '#A7F3D0',
          dark: '#34D399',
        },
      },
    },
  },
  plugins: [],
}
