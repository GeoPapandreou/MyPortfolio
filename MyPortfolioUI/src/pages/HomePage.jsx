import LandingHeroSection from "../components/HomeSections/LandingHeroSection";
import ExamplesSection from "../components/HomeSections/ExamplesSection";
import HowItWorksSection from "../components/HomeSections/HowItWorksSection";

export default function HomePage() {
  return (
    <div className="pb-24">
      <LandingHeroSection />
      <ExamplesSection />
      <HowItWorksSection />
    </div>
  );
}
