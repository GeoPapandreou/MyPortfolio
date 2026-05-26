export default {
  content: ["./index.html", "./src/**/*.{js,jsx}"],
  theme: {
    extend: {
      colors: {
        ink: "#061425",
        mist: "#EAF2FF",
        glow: "#4DD0E1",
        amber: "#F4A261",
        coral: "#FF7F6A",
        slate: "#102338"
      },
      fontFamily: {
        display: ["Fraunces", "serif"],
        body: ["Space Grotesk", "sans-serif"]
      },
      boxShadow: {
        card: "0 24px 80px rgba(4, 12, 24, 0.36)"
      },
      backgroundImage: {
        "hero-grid":
          "radial-gradient(circle at top, rgba(77, 208, 225, 0.18), transparent 34%), linear-gradient(135deg, rgba(255, 127, 106, 0.14), transparent 42%)"
      }
    }
  },
  plugins: []
};
