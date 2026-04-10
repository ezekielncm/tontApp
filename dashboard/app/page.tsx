import Link from "next/link";

const features = [
  {
    title: "Gestion transparente",
    description:
      "Suivez chaque cotisation en temps réel. Plus d'oublis ni de litiges grâce à un historique complet.",
    icon: "📊",
  },
  {
    title: "Paiements sécurisés",
    description:
      "Intégration Orange Money, Wave et autres. Vos transactions sont chiffrées et traçables.",
    icon: "🔒",
  },
  {
    title: "Notifications automatiques",
    description:
      "Rappels de cotisation, relances automatiques et messages personnalisés pour ne rien rater.",
    icon: "🔔",
  },
];

const pricing = [
  {
    name: "Gratuit",
    price: "0 FCFA",
    period: "/mois",
    features: [
      "1 tontine active",
      "Jusqu'à 10 membres",
      "Notifications push",
      "Historique basique",
    ],
    cta: "Commencer",
    highlighted: false,
  },
  {
    name: "Premium",
    price: "2 500 FCFA",
    period: "/mois",
    features: [
      "Tontines illimitées",
      "Membres illimités",
      "SMS & WhatsApp",
      "Export PDF",
      "Score de crédit",
      "Support prioritaire",
    ],
    cta: "Essai gratuit 30j",
    highlighted: true,
  },
];

export default function LandingPage() {
  return (
    <main className="min-h-screen bg-white">
      {/* Hero */}
      <section className="bg-gradient-to-br from-green-800 to-green-600 text-white">
        <nav className="max-w-6xl mx-auto px-6 py-4 flex items-center justify-between">
          <span className="text-xl font-bold">TontinesApp</span>
          <Link
            href="/login"
            className="rounded-lg bg-white/20 px-4 py-2 text-sm font-medium hover:bg-white/30 transition"
          >
            Connexion
          </Link>
        </nav>

        <div className="max-w-4xl mx-auto px-6 py-24 text-center">
          <h1 className="text-4xl md:text-5xl font-extrabold tracking-tight mb-6">
            Gérez vos tontines
            <br />
            en toute sérénité
          </h1>
          <p className="text-lg md:text-xl text-green-100 mb-10 max-w-2xl mx-auto">
            L&apos;application qui digitalise vos tontines : transparence totale,
            paiements sécurisés et suivi en temps réel.
          </p>
          <div className="flex flex-col sm:flex-row gap-4 justify-center">
            <Link
              href="/login"
              className="rounded-xl bg-orange-500 px-8 py-3 text-lg font-semibold hover:bg-orange-600 transition shadow-lg"
            >
              Commencer gratuitement
            </Link>
            <a
              href="#fonctionnalites"
              className="rounded-xl border-2 border-white/40 px-8 py-3 text-lg font-semibold hover:bg-white/10 transition"
            >
              En savoir plus
            </a>
          </div>
        </div>
      </section>

      {/* Features */}
      <section id="fonctionnalites" className="max-w-6xl mx-auto px-6 py-20">
        <h2 className="text-3xl font-bold text-center text-gray-900 mb-12">
          Pourquoi TontinesApp ?
        </h2>
        <div className="grid md:grid-cols-3 gap-8">
          {features.map((f) => (
            <div
              key={f.title}
              className="rounded-2xl border border-gray-200 p-8 hover:shadow-lg transition"
            >
              <span className="text-4xl mb-4 block">{f.icon}</span>
              <h3 className="text-xl font-semibold text-gray-900 mb-2">
                {f.title}
              </h3>
              <p className="text-gray-600 leading-relaxed">{f.description}</p>
            </div>
          ))}
        </div>
      </section>

      {/* Pricing */}
      <section className="bg-gray-50 py-20">
        <div className="max-w-4xl mx-auto px-6">
          <h2 className="text-3xl font-bold text-center text-gray-900 mb-12">
            Tarifs simples
          </h2>
          <div className="grid md:grid-cols-2 gap-8">
            {pricing.map((p) => (
              <div
                key={p.name}
                className={`rounded-2xl p-8 ${
                  p.highlighted
                    ? "bg-green-800 text-white shadow-xl scale-105"
                    : "bg-white border border-gray-200"
                }`}
              >
                <h3 className="text-xl font-semibold mb-2">{p.name}</h3>
                <div className="flex items-baseline gap-1 mb-6">
                  <span className="text-3xl font-bold">{p.price}</span>
                  <span
                    className={
                      p.highlighted ? "text-green-200" : "text-gray-500"
                    }
                  >
                    {p.period}
                  </span>
                </div>
                <ul className="space-y-3 mb-8">
                  {p.features.map((feat) => (
                    <li key={feat} className="flex items-center gap-2">
                      <span>✓</span>
                      <span>{feat}</span>
                    </li>
                  ))}
                </ul>
                <Link
                  href="/login"
                  className={`block text-center rounded-xl px-6 py-3 font-semibold transition ${
                    p.highlighted
                      ? "bg-orange-500 hover:bg-orange-600 text-white"
                      : "bg-green-800 hover:bg-green-700 text-white"
                  }`}
                >
                  {p.cta}
                </Link>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* Footer */}
      <footer className="bg-gray-900 text-gray-400 py-10">
        <div className="max-w-6xl mx-auto px-6 flex flex-col md:flex-row items-center justify-between gap-4">
          <span className="font-semibold text-white">TontinesApp</span>
          <p className="text-sm">
            © {new Date().getFullYear()} TontinesApp. Tous droits réservés.
          </p>
          <a
            href="https://wa.me/221771234567"
            target="_blank"
            rel="noopener noreferrer"
            className="text-green-400 hover:text-green-300 transition text-sm"
          >
            💬 Support WhatsApp
          </a>
        </div>
      </footer>
    </main>
  );
}

