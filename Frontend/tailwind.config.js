/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ["./src/**/*.{html,ts}"],
  theme: {
    extend: {
      colors: {
        primary: "#FF5200",
        "primary-dark": "#E04800",
        "primary-light": "#FF7340",
        dark: "#1C1C1C",
        "gray-soft": "#F5F5F5",
      },
      fontFamily: {
        sans: ["Inter", "sans-serif"],
      },
      boxShadow: {
        card: "0 2px 16px rgba(0,0,0,0.08)",
        "card-hover": "0 8px 32px rgba(0,0,0,0.14)",
      },
    },
  },
  plugins: [],
};
