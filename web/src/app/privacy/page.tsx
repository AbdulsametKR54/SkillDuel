import type { Metadata } from "next";
import Link from "next/link";

export const metadata: Metadata = {
  title: "Gizlilik Politikası — SkillDuel",
  description:
    "SkillDuel Gizlilik Politikası: Kişisel verilerinizin nasıl toplandığı, kullanıldığı ve korunduğu hakkında bilgi edinin.",
};

export default function PrivacyPage() {
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
                <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z" />
              </svg>
            </span>
            <h1 className="text-3xl sm:text-4xl font-bold tracking-tight">
              <span className="text-gradient-accent">Gizlilik</span> Politikası
            </h1>
          </div>
          <p className="text-muted-foreground text-sm">
            Son güncelleme: Mayıs 2025 &nbsp;·&nbsp; KVKK uyumlu
          </p>
        </div>
      </div>

      {/* Content */}
      <div className="max-w-3xl mx-auto px-4 py-12 space-y-10">
        {/* 1 */}
        <Section title="1. Veri Sorumlusu">
          <p>
            Bu Gizlilik Politikası, <strong>SkillDuel</strong> platformu
            tarafından 6698 sayılı Kişisel Verilerin Korunması Kanunu
            (KVKK) çerçevesinde hazırlanmıştır. Veri sorumlusu olarak
            iletişim adresimiz:{" "}
            <a
              href="mailto:admin@skillduel.com"
              className="text-primary hover:underline"
            >
              admin@skillduel.com
            </a>
          </p>
        </Section>

        {/* 2 */}
        <Section title="2. Toplanan Veriler">
          <p>
            Platform üzerinden aşağıdaki kişisel ve platform verileri
            işlenmektedir:
          </p>
          <ul className="list-disc list-inside space-y-2 mt-3 text-muted-foreground">
            <li>
              <strong className="text-foreground">E-posta adresi</strong> —
              hesap oluşturma ve kimlik doğrulama amacıyla
            </li>
            <li>
              <strong className="text-foreground">Kullanıcı adı</strong> —
              liderlik tablosu ve profil sayfasında gösterim amacıyla
            </li>
            <li>
              <strong className="text-foreground">Oyun istatistikleri</strong>{" "}
              — kazanılan/kaybedilen maç sayısı, doğru cevap oranı, puan
              geçmişi
            </li>
            <li>
              <strong className="text-foreground">Oturum ve çerez verileri</strong>{" "}
              — güvenli oturum yönetimi için
            </li>
            <li>
              <strong className="text-foreground">IP adresi ve tarayıcı bilgisi</strong>{" "}
              — güvenlik ve suistimal tespiti amacıyla
            </li>
          </ul>
        </Section>

        {/* 3 */}
        <Section title="3. Verilerin Kullanım Amaçları">
          <ul className="list-disc list-inside space-y-2 text-muted-foreground">
            <li>Hesabınızı oluşturmak ve güvenli şekilde yönetmek</li>
            <li>
              Oyun deneyimini kişiselleştirmek ve liderlik tablolarını
              oluşturmak
            </li>
            <li>Platform güvenliğini sağlamak ve hile girişimlerini engellemek</li>
            <li>Yasal yükümlülüklerin yerine getirilmesi</li>
            <li>
              Hizmet kalitesini iyileştirmeye yönelik anonim istatistik
              analizi
            </li>
          </ul>
        </Section>

        {/* 4 */}
        <Section title="4. Üçüncü Taraf Hizmetler">
          <p>
            SkillDuel, soru veritabanı olarak{" "}
            <strong>Open Trivia Database (OpenTDB) API</strong>&apos;sini
            kullanmaktadır. Bu hizmet yalnızca anonim API çağrıları aracılığıyla
            sorgulanır; herhangi bir kişisel veriniz bu servisle paylaşılmaz.
          </p>
          <p className="mt-3">
            OpenTDB gizlilik politikasına{" "}
            <a
              href="https://opentdb.com"
              target="_blank"
              rel="noopener noreferrer"
              className="text-primary hover:underline"
            >
              opentdb.com
            </a>{" "}
            adresinden ulaşabilirsiniz.
          </p>
        </Section>

        {/* 5 */}
        <Section title="5. Veri Saklama Süresi">
          <p>
            Kişisel verileriniz, hesabınız aktif olduğu süre boyunca
            saklanmaktadır. Hesabınızı silmeniz durumunda verileriniz{" "}
            <strong>30 gün</strong> içinde kalıcı olarak sistemlerimizden
            kaldırılır. Yasal yükümlülükler gerektirdiğinde veriler yasal
            saklama süreleri boyunca saklanabilir.
          </p>
        </Section>

        {/* 6 */}
        <Section title="6. Kullanıcı Hakları (KVKK Madde 11)">
          <p>KVKK kapsamında aşağıdaki haklara sahipsiniz:</p>
          <ul className="list-disc list-inside space-y-2 mt-3 text-muted-foreground">
            <li>Kişisel verilerinizin işlenip işlenmediğini öğrenme</li>
            <li>İşlenen verileriniz hakkında bilgi talep etme</li>
            <li>
              Verilerin eksik veya yanlış işlendiği hâllerde düzeltilmesini
              isteme
            </li>
            <li>
              Verilerin silinmesini veya yok edilmesini talep etme
            </li>
            <li>
              Otomatik sistemler aracılığıyla aleyhinize bir kararın oluşmasına
              itiraz etme
            </li>
            <li>
              Verilerin kanuna aykırı işlenmesi nedeniyle oluşan zararın
              giderilmesini talep etme
            </li>
          </ul>
          <p className="mt-4">
            Bu haklarınızı kullanmak için{" "}
            <a
              href="mailto:admin@skillduel.com"
              className="text-primary hover:underline"
            >
              admin@skillduel.com
            </a>{" "}
            adresine e-posta gönderebilirsiniz. Talepleriniz en geç{" "}
            <strong>30 gün</strong> içinde yanıtlanacaktır.
          </p>
        </Section>

        {/* 7 */}
        <Section title="7. Veri Güvenliği">
          <p>
            Verileriniz, yetkisiz erişime karşı endüstri standardı güvenlik
            önlemleri (şifrelenmiş bağlantılar, erişim kontrolü, düzenli
            güvenlik denetimleri) ile korunmaktadır. Güvenlik ihlali
            durumunda ilgili kullanıcılar KVKK&apos;nın öngördüğü süre içinde
            bilgilendirilir.
          </p>
        </Section>

        {/* 8 */}
        <Section title="8. İletişim">
          <p>
            Gizlilik politikamız hakkında soru ve talepleriniz için:{" "}
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
            Bu politika değiştirilebilir. Önemli değişikliklerde kayıtlı
            e-posta adresinize bildirim gönderilecektir.
          </p>
          <div className="mt-4 flex justify-center gap-4 text-sm">
            <Link href="/terms" className="text-primary hover:underline">
              Kullanım Koşulları
            </Link>
            <Link href="/" className="text-muted-foreground hover:text-primary transition-colors">
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
