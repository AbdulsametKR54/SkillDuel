import Link from "next/link";

export default function Footer() {
  return (
    <footer className="w-full border-t border-border bg-card/50 backdrop-blur-sm mt-auto">
      <div className="max-w-7xl mx-auto px-4 py-5 flex flex-col sm:flex-row items-center justify-between gap-3">
        {/* Copyright */}
        <span className="text-muted-foreground text-sm">
          © 2026{" "}
          <span className="text-gradient-accent font-semibold">SkillDuel</span>
          . Tüm hakları saklıdır.
        </span>

        {/* Links */}
        <nav className="flex items-center gap-1" aria-label="Footer navigation">
          <Link
            href="/privacy"
            className="text-sm text-muted-foreground hover:text-primary transition-colors duration-200 px-3 py-1.5 rounded-md hover:bg-primary/10"
          >
            Gizlilik Politikası
          </Link>
          <span className="text-border text-xs select-none">•</span>
          <Link
            href="/terms"
            className="text-sm text-muted-foreground hover:text-primary transition-colors duration-200 px-3 py-1.5 rounded-md hover:bg-primary/10"
          >
            Kullanım Koşulları
          </Link>
        </nav>
      </div>
    </footer>
  );
}
