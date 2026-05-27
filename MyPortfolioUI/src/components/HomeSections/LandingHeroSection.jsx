import { ArrowRight, Check, Sparkles } from "lucide-react";
import { Link, useNavigate } from "react-router-dom";
import heroExampleImage from "../../assets/hero-example.png";
import { useAuth } from "../../hooks/useAuth";

const heroHighlights = [
  { title: "Complete website", text: "Everything arranged in one polished package" },
  { title: "Tailored to you", text: "Built around your profession and your story" },
  { title: "Free to start", text: "Begin with a few simple answers" }
];

export default function LandingHeroSection() {
  const { isAuthenticated } = useAuth();
  const navigate = useNavigate();

  return (
    <section id="features" className="mx-auto max-w-7xl px-4 pt-4 sm:px-6 lg:px-8">
      <div className="glass-card dark-grid-panel relative overflow-hidden p-8 md:p-10 xl:p-12">
        <div className="pointer-events-none absolute inset-0 bg-[radial-gradient(circle_at_80%_18%,rgba(124,58,237,0.28),transparent_26%),radial-gradient(circle_at_72%_82%,rgba(56,189,248,0.10),transparent_30%)]" />
        <div className="relative grid gap-12 xl:grid-cols-[0.82fr_1.18fr] xl:items-center">
          <div className="xl:pr-4">
            <div className="inline-flex items-center rounded-full border border-white/10 bg-white/[0.04] px-4 py-2 text-sm text-white/78">
              <Sparkles size={15} className="mr-2 text-violet-400" />
              Smart portfolio builder
            </div>

            <h1 className="mt-10 max-w-xl text-5xl font-semibold leading-[0.92] text-white md:text-7xl">
              Your portfolio,
              <span className="block bg-gradient-to-r from-violet-400 via-fuchsia-400 to-violet-500 bg-clip-text text-transparent">
                beautifully arranged.
              </span>
            </h1>

            <p className="mt-6 max-w-lg text-base leading-8 text-white/66 md:text-lg">
              Answer a few clear questions and receive a polished portfolio website shaped around your work, your style, and the people you want to reach.
            </p>

            <div className="mt-8 flex flex-col gap-3 sm:flex-row">
              <button className="primary-button" onClick={() => navigate(isAuthenticated ? "/wizard" : "/register")}>
                Create your portfolio
                <ArrowRight size={16} className="ml-2" />
              </button>
              <Link to="/examples" className="secondary-button">
                See examples
              </Link>
            </div>

            <div className="mt-10 grid gap-4 sm:grid-cols-3">
              {heroHighlights.map((item) => (
                <div key={item.title} className="text-sm text-white/64">
                  <div className="mb-3 flex items-center gap-2 text-white">
                    <Check size={15} className="text-emerald-400" />
                    <span className="font-medium">{item.title}</span>
                  </div>
                  <p>{item.text}</p>
                </div>
              ))}
            </div>
          </div>

          <div className="relative xl:pl-2">
            <div className="absolute -inset-6 rounded-[40px] bg-[radial-gradient(circle_at_65%_8%,rgba(124,58,237,0.24),transparent_30%),radial-gradient(circle_at_15%_85%,rgba(56,189,248,0.12),transparent_32%)] blur-3xl" />
            <img
              src={heroExampleImage}
              alt="Example portfolio preview"
              className="relative w-full object-contain shadow-[0_30px_90px_rgba(0,0,0,0.42)]"
            />
          </div>
        </div>
      </div>
    </section>
  );
}
