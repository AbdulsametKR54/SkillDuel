import type { Metadata } from "next";
import Link from "next/link";

export const metadata: Metadata = {
  title: "Kullanım Koşulları — SkillDuel",
  description:
    "SkillDuel Kullanım Koşulları: Platformu kullanmadan önce lütfen bu koşulları okuyun.",
};

export default function TermsPage() {
  return (
    <main className="min-h-screen bg-background text-foreground">
      {/* Hero */}
      <div className="bg-gradient-to-br from-card via-background to-background border-b border-border">
        <div className="max-w-3xl mx-auto px-4 py-14 sm:py-20">
          <Link
            href="/lobby"
            className="inline-flex items-center gap-2 text-muted-foreground hover:text-primary transition-colors text-sm mb-8"
          >
            <svg
              xmlns="http://www.w3.org/2000/svg"
              width="16"
              height="16"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              strokeWidth="2"
              strokeLinecap="round"
              strokeLinejoin="round"
            >
              <path d="m15 18-6-6 6-6" />
            </svg>
            Ana Sayfaya Dön
          </Link>

          <div className="flex items-center gap-3 mb-4">
            <span className="inline-flex items-center justify-center w-10 h-10 rounded-xl bg-primary/10 text-primary">
              <svg
                xmlns="http://www.w3.org/2000/svg"
                width="20"
                height="20"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                strokeWidth="2"
                strokeLinecap="round"
                strokeLinejoin="round"
              >
                <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" />
                <polyline points="14 2 14 8 20 8" />
                <line x1="16" y1="13" x2="8" y2="13" />
                <line x1="16" y1="17" x2="8" y2="17" />
                <polyline points="10 9 9 9 8 9" />
              </svg>
            </span>
            <h1 className="text-3xl sm:text-4xl font-bold tracking-tight">
              <span className="text-gradient-accent">Kullanım</span> Koşulları
            </h1>
          </div>
          <p className="text-muted-foreground text-sm">
            Son güncelleme: Mayıs 2025 &nbsp;·&nbsp; Lütfen okuyunuz
          </p>
        </div>
      </div>

      {/* Content */}
      <div className="max-w-3xl mx-auto px-4 py-12 space-y-10">
        {/* Intro */}
        <div className="bg-card border border-border rounded-xl p-5 text-sm text-muted-foreground leading-relaxed">
          SkillDuel&apos;i kullanarak aşağıdaki koşulları kabul etmiş sayılırsınız.
          Bu koşulları kabul etmiyorsanız platformu kullanmayınız. Koşulları
          dilediğimiz zaman güncelleme hakkımızı saklı tutarız; güncel sürüm
          her zaman bu sayfada yayımlanır.
        </div>

        {/* 1 */}
        <Section title="1. Hizmetin Amacı">
          <p>
            SkillDuel, kullanıcıların çeşitli kategorilerde bilgi yarışması
            oyunları oynayabileceği çevrimiçi bir rekabetçi trivia
            platformudur. Hizmet yalnızca eğlence ve sosyal rekabet amaçlıdır;
            gerçek para ödülü ya da kumar unsuru içermemektedir.
          </p>
        </Section>

        {/* 2 */}
        <Section title="2. Hesap Oluşturma ve Kullanıcı Sorumlulukları">
          <p>
            Platforma kayıt olabilmek için 13 yaşını doldurmuş olmanız
            gerekmektedir. Hesabınıza ait kullanıcı adı, şifre ve e-posta
            bilgilerinin gizliliğini korumak tamamen sizin
            sorumluluğunuzdadır.
          </p>
          <ul className="list-disc list-inside space-y-2 mt-3">
            <li>Her kullanıcı yalnızca bir hesap oluşturabilir.</li>
            <li>
              Başkasının kimliğine bürünmek veya yanıltıcı bilgi vermek
              yasaktır.
            </li>
            <li>
              Hesabınızın yetkisiz kullanımını fark ettiğinizde derhal{" "}
              <a
                href="mailto:admin@skillduel.com"
                className="text-primary hover:underline"
              >
                admin@skillduel.com
              </a>{" "}
              adresine bildiriniz.
            </li>
          </ul>
        </Section>

        {/* 3 */}
        <Section title="3. Kabul Edilebilir Kullanım">
          <p>Platform kullanırken aşağıdakilere uymakla yükümlüsünüz:</p>
          <ul className="list-disc list-inside space-y-2 mt-3">
            <li>Platformu yalnızca yasal amaçlarla kullanmak</li>
            <li>Diğer kullanıcılara saygılı davranmak</li>
            <li>
              Platform altyapısına zarar verici eylemlerden kaçınmak
            </li>
            <li>Telif hakkı ihlali yaratacak içerik paylaşmamak</li>
          </ul>
        </Section>

        {/* 4 */}
        <Section title="4. Yasaklanan Davranışlar">
          <p>
            Aşağıdaki davranışlar kesinlikle yasaktır ve hesabın askıya
            alınması ya da kalıcı olarak kapatılmasıyla sonuçlanabilir:
          </p>
          <div className="mt-3 space-y-2">
            <ProhibitedItem
              icon="🤖"
              label="Hile / Bot kullanımı"
              desc="Otomatik araçlar, betikler veya üçüncü taraf yazılımlarla haksız avantaj elde etmek"
            />
            <ProhibitedItem
              icon="🤝"
              label="Elo manipülasyonu"
              desc="Birden fazla hesap veya anlaşmalı oyunlarla puan/sıralama manipüle etmek"
            />
            <ProhibitedItem
              icon="🚫"
              label="Taciz ve hakaret"
              desc="Diğer kullanıcılara yönelik küfür, tehdit, ayrımcılık içeren her türlü iletişim"
            />
            <ProhibitedItem
              icon="🛡️"
              label="Güvenlik açıklarını istismar etmek"
              desc="Platformun API veya altyapısında hata ya da açık aramak ve bunları kullanmak"
            />
            <ProhibitedItem
              icon="📢"
              label="İstenmeyen reklam / spam"
              desc="Onay alınmadan reklam, referans linki veya toplu mesaj göndermek"
            />
          </div>
        </Section>

        {/* 5 */}
        <Section title="5. Hesap Askıya Alma ve Sonlandırma">
          <p>
            SkillDuel, bu koşulları ihlal ettiği tespit edilen hesapları
            önceden bildirimde bulunmaksızın geçici veya kalıcı olarak
            askıya alma ya da kapatma hakkını saklı tutar. Ağır ihlallerde
            (hile, taciz vb.) hesap anında kalıcı olarak kapatılabilir.
          </p>
          <p className="mt-3">
            Hesabınızın haksız yere askıya alındığını düşünüyorsanız{" "}
            <a
              href="mailto:admin@skillduel.com"
              className="text-primary hover:underline"
            >
              admin@skillduel.com
            </a>{" "}
            adresine itiraz edebilirsiniz.
          </p>
        </Section>

        {/* 6 */}
        <Section title="6. Fikri Mülkiyet">
          <p>
            Platform üzerindeki tüm içerik, tasarım, marka ve yazılım
            SkillDuel&apos;e aittir ve telif hakkıyla korunmaktadır. Kullanıcılar
            bu içerikleri önceden yazılı izin almaksızın kopyalayamaz,
            dağıtamaz veya ticarileştiremez.
          </p>
          <p className="mt-3">
            Kullanıcı tarafından platforma gönderilen içerikler (kullanıcı
            adı, soru önerileri vb.) için SkillDuel&apos;e dünya genelinde,
            ücretsiz, devredilebilir bir lisans verilmiş sayılır.
          </p>
        </Section>

        {/* 7 */}
        <Section title="7. Sorumluluk Reddi">
          <p>
            SkillDuel hizmeti <strong>&quot;olduğu gibi&quot;</strong> sunulmaktadır.
            Platform kesintisiz veya hatasız çalışacağı konusunda herhangi
            bir garanti verilmemektedir.
          </p>
          <ul className="list-disc list-inside space-y-2 mt-3">
            <li>
              Teknik arızalar, sunucu kesintileri veya veri kaybından
              dolayı sorumluluk kabul edilmez.
            </li>
            <li>
              Üçüncü taraf API (OpenTDB) kaynaklı hata veya içerik
              sorunlarından SkillDuel sorumlu tutulamaz.
            </li>
            <li>
              Kullanıcılar arası anlaşmazlıklarda SkillDuel taraf
              tutmakla yükümlü değildir.
            </li>
          </ul>
        </Section>

        {/* 8 */}
        <Section title="8. Uygulanacak Hukuk">
          <p>
            Bu koşullar Türk hukukuna tabidir. Anlaşmazlıklarda{" "}
            <strong>İstanbul</strong> mahkemeleri ve icra daireleri yetkilidir.
          </p>
        </Section>

        {/* 9 */}
        <Section title="9. İletişim">
          <p>
            Kullanım koşullarına ilişkin soru ve talepleriniz için:{" "}
            <a
              href="mailto:admin@skillduel.com"
              className="text-primary hover:underline font-medium"
            >
              admin@skillduel.com
            </a>
          </p>
        </Section>

        {/* Divider */}
        <div className="border-t border-border pt-8 text-center">
          <p className="text-muted-foreground text-sm">
            Platformu kullanmaya devam ederek bu koşulların tamamını kabul
            etmiş sayılırsınız.
          </p>
          <div className="mt-4 flex justify-center gap-4 text-sm">
            <Link href="/privacy" className="text-primary hover:underline">
              Gizlilik Politikası
            </Link>
            <Link
              href="/"
              className="text-muted-foreground hover:text-primary transition-colors"
            >
              Ana Sayfa
            </Link>
          </div>
        </div>
      </div>
    </main>
  );
}

function Section({
  title,
  children,
}: {
  title: string;
  children: React.ReactNode;
}) {
  return (
    <section className="space-y-3">
      <h2 className="text-lg font-semibold text-foreground border-l-2 border-primary pl-3">
        {title}
      </h2>
      <div className="text-muted-foreground leading-relaxed pl-3 space-y-2">
        {children}
      </div>
    </section>
  );
}

function ProhibitedItem({
  icon,
  label,
  desc,
}: {
  icon: string;
  label: string;
  desc: string;
}) {
  return (
    <div className="flex items-start gap-3 bg-destructive/5 border border-destructive/20 rounded-lg px-4 py-3">
      <span className="text-xl leading-none mt-0.5">{icon}</span>
      <div>
        <p className="text-sm font-semibold text-foreground">{label}</p>
        <p className="text-xs text-muted-foreground mt-0.5">{desc}</p>
      </div>
    </div>
  );
}
