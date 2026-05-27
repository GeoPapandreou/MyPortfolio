import { ArrowRight, Rocket } from "lucide-react";
import { useNavigate } from "react-router-dom";
import chatIcon from "../../assets/chat_icon.png";
import downloadIcon from "../../assets/download_icon.png";
import reviewIcon from "../../assets/review_icon.png";
import shapeIcon from "../../assets/shape_icon.png";
import { useAuth } from "../../hooks/useAuth";

const howItWorksCards = [
  {
    step: "1",
    iconSrc: chatIcon,
    iconAlt: "Questions preview",
    title: "Answer questions",
    text: "Tell us about yourself, your work, and the things you want people to notice."
  },
  {
    step: "2",
    iconSrc: shapeIcon,
    iconAlt: "Portfolio layout preview",
    title: "Your portfolio takes shape",
    text: "Your answers are arranged into a polished portfolio that fits your profession."
  },
  {
    step: "3",
    iconSrc: reviewIcon,
    iconAlt: "Refine your content",
    title: "Refine and rebuild",
    text: "Adjust your answers, try a different style, and build again whenever you want."
  },
  {
    step: "4",
    iconSrc: downloadIcon,
    iconAlt: "Download preview",
    title: "Download and share",
    text: "Take the finished package with you and publish it in the way that suits you best."
  }
];

function HowItWorksArtwork({ iconSrc, iconAlt }) {
  return (
    <div className="relative h-52 overflow-hidden rounded-[28px] bg-[radial-gradient(circle_at_30%_18%,rgba(139,92,246,0.18),transparent_30%),radial-gradient(circle_at_80%_78%,rgba(124,58,237,0.14),transparent_24%)]">
      <img src={iconSrc} alt={iconAlt} className="relative z-10 h-full w-full object-contain" />
    </div>
  );
}

export default function HowItWorksSection() {
  const { isAuthenticated } = useAuth();
  const navigate = useNavigate();

  return (
    <section id="how-it-works" className="mx-auto mt-8 max-w-7xl px-4 sm:px-6 lg:px-8">
      <div className="glass-card dark-grid-panel overflow-hidden p-6 md:p-8">
        <div className="text-center">
          <p className="text-sm font-semibold uppercase tracking-[0.28em] text-fuchsia-400/90">How it works</p>
          <h2 className="mt-3 text-4xl font-semibold text-white md:text-6xl">
            How it{" "}
            <span className="bg-gradient-to-r from-violet-400 via-fuchsia-400 to-rose-400 bg-clip-text text-transparent">works</span>
          </h2>
          <p className="mx-auto mt-5 max-w-2xl text-base leading-8 text-white/64 md:text-[1.35rem] md:leading-9">
            From idea to portfolio in just a few simple steps.
          </p>
        </div>

        <div className="mt-10 grid gap-5 lg:grid-cols-4">
          {howItWorksCards.map((item, index) => (
            <article key={item.title} className="relative rounded-[28px] border border-white/10 bg-[linear-gradient(180deg,rgba(10,16,31,0.94),rgba(7,13,24,0.98))] p-5 shadow-[inset_0_1px_0_rgba(255,255,255,0.04)]">
              <div className="flex items-start">
                <div className="inline-flex items-center rounded-2xl bg-gradient-to-br from-violet-500 to-fuchsia-500 px-4 py-3 text-sm font-semibold text-white shadow-[0_12px_30px_rgba(124,58,237,0.35)]">
                  Step {item.step}
                </div>
              </div>

              <div className="relative mt-6">
                <HowItWorksArtwork iconSrc={item.iconSrc} iconAlt={item.iconAlt} />
                {index < howItWorksCards.length - 1 ? (
                  <div className="pointer-events-none absolute -right-12 top-1/2 z-10 hidden -translate-y-1/2 lg:flex">
                    <div className="grid h-10 w-10 place-items-center rounded-full border border-fuchsia-500/35 bg-[radial-gradient(circle,rgba(124,58,237,0.26),rgba(10,16,31,0.96))] text-fuchsia-300 shadow-[0_0_22px_rgba(168,85,247,0.22)]">
                      <ArrowRight size={18} />
                    </div>
                  </div>
                ) : null}
              </div>

              <p className="mt-6 text-[2rem] font-semibold leading-tight text-white/96">{item.title}</p>
              <p className="mt-4 max-w-xs text-lg leading-9 text-white/64">{item.text}</p>
            </article>
          ))}
        </div>

        <div className="relative mt-8 overflow-hidden rounded-[30px] border border-fuchsia-500/20 bg-[linear-gradient(90deg,rgba(29,10,56,0.82),rgba(8,14,28,0.98)_40%,rgba(10,16,31,0.98))] p-6 shadow-[inset_0_1px_0_rgba(255,255,255,0.04)]">
          <div className="pointer-events-none absolute inset-0 bg-[radial-gradient(circle_at_18%_18%,rgba(168,85,247,0.24),transparent_22%),radial-gradient(circle_at_78%_50%,rgba(59,130,246,0.08),transparent_24%)]" />
          <div className="pointer-events-none absolute -bottom-12 left-0 h-28 w-72 rounded-full border border-fuchsia-400/20 opacity-40 blur-[1px]" />
          <div className="relative flex flex-col items-start justify-between gap-6 md:flex-row md:items-center">
            <div className="flex items-center gap-5">
              <div className="grid h-20 w-20 place-items-center rounded-full border border-white/10 bg-[radial-gradient(circle_at_30%_30%,rgba(124,58,237,0.36),rgba(8,14,28,0.96))] shadow-[0_18px_40px_rgba(76,29,149,0.34)]">
                <Rocket size={34} className="text-violet-300" />
              </div>
              <div>
                <p className="text-2xl font-semibold text-white md:text-3xl">Ready to build your portfolio?</p>
                <p className="mt-2 text-base text-white/64 md:text-lg">Get started for free. No card required.</p>
              </div>
            </div>

            <button
              className="flex min-w-[260px] items-center justify-center rounded-[22px] bg-gradient-to-r from-violet-600 via-fuchsia-500 to-rose-500 px-8 py-5 text-lg font-semibold text-white shadow-[0_18px_45px_rgba(168,85,247,0.28)] transition hover:translate-y-[-1px] hover:shadow-[0_22px_50px_rgba(168,85,247,0.34)]"
              onClick={() => navigate(isAuthenticated ? "/wizard" : "/register")}
            >
              Create your portfolio
              <ArrowRight size={16} className="ml-2" />
            </button>
          </div>
        </div>
      </div>
    </section>
  );
}
