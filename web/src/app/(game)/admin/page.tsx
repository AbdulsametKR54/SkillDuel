'use client';

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { usersApi } from '@/lib/api';
import Link from 'next/link';
import { Shield, MessageSquare, Flag, Loader2 } from 'lucide-react';
import { Navbar } from '@/components/Navbar';

export default function AdminDashboardPage() {
  const router = useRouter();
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const init = async () => {
      try {
        const u = await usersApi.me();
        if (u.data.role !== 'Admin') {
          router.push('/lobby');
          return;
        }
        setLoading(false);
      } catch {
        router.push('/lobby');
      }
    };
    init();
  }, [router]);

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-[60vh]">
        <Loader2 className="h-12 w-12 animate-spin text-primary" />
      </div>
    );
  }

  return (
    <div>
      <Navbar />
      <main className="flex-1 w-full max-w-7xl mx-auto px-4 sm:px-6 py-8">
        <div className="max-w-4xl mx-auto space-y-8 animate-in fade-in duration-300">
          <div className="flex items-center gap-4 border-b border-border pb-6">
            <div className="p-3 bg-primary/10 rounded-2xl">
              <Shield className="h-8 w-8 text-primary" />
            </div>
            <div>
              <h1 className="text-3xl font-black tracking-tight text-foreground">Admin Paneli</h1>
              <p className="text-muted-foreground">Sistem yönetimi, soru onayı ve rapor denetimi.</p>
            </div>
          </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        <Link href="/admin/questions">
          <div className="bg-card border border-border hover:border-primary/50 hover:bg-input transition-all rounded-2xl p-6 group cursor-pointer flex items-center justify-between">
            <div className="flex items-center gap-4">
              <div className="h-12 w-12 bg-blue-500/10 text-blue-500 rounded-xl flex items-center justify-center group-hover:scale-110 transition-transform">
                <MessageSquare className="h-6 w-6" />
              </div>
              <div>
                <h2 className="text-lg font-bold text-foreground">Soru Havuzu</h2>
                <p className="text-sm text-muted-foreground">Kullanıcı sorularını incele ve onayla.</p>
              </div>
            </div>
          </div>
        </Link>
        <Link href="/admin/bans">
          <div className="bg-card border border-border hover:border-destructive/50 hover:bg-destructive/5 transition-all rounded-2xl p-6 group cursor-pointer flex items-center justify-between">
            <div className="flex items-center gap-4">
              <div className="h-12 w-12 bg-red-500/10 text-red-500 rounded-xl flex items-center justify-center group-hover:scale-110 transition-transform">
                <Flag className="h-6 w-6" />
              </div>
              <div>
                <h2 className="text-lg font-bold text-foreground">Raporlar & Banlar</h2>
                <p className="text-sm text-muted-foreground">Kullanıcı şikayetlerini incele ve ceza ver.</p>
              </div>
            </div>
          </div>
        </Link>
      </div>
      </div>
      </main>
    </div>
  );
}
