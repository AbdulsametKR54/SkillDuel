'use client';

import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { useState, useEffect } from 'react';
import { usersApi } from '@/lib/api';
import { Trophy, History, Swords, LogOut } from 'lucide-react';
import { UserSearch } from '@/components/UserSearch';
import Cookies from 'js-cookie';

interface UserProfile {
  id: string;
  username: string;
  role?: string;
  eloRating: number;
}

export function Navbar() {
  const [user, setUser] = useState<UserProfile | null>(null);
  const router = useRouter();

  useEffect(() => {
    const fetchUser = async () => {
      try {
        const u = await usersApi.me();
        if (u.success) setUser(u.data);
      } catch (e) {
        console.error(e);
      }
    };
    fetchUser();
  }, []);

  const handleLogout = () => {
    Cookies.remove('token');
    Cookies.remove('refreshToken');
    router.push('/login');
  };

  return (
    <header className="sticky top-0 z-10 bg-background border-b border-border px-6 py-4 flex flex-col md:flex-row md:items-center justify-between gap-4">
      <div className="flex items-center gap-2">
        <div className="bg-gradient-accent p-1.5 rounded-lg">
          <Swords className="h-5 w-5 text-white" />
        </div>
        <Link href="/lobby">
          <span className="text-xl font-black tracking-tighter text-gradient-accent">SKILLDUEL</span>
        </Link>
      </div>
      
      <nav className="flex flex-wrap items-center gap-2 bg-card p-1.5 rounded-xl border border-border">
        <Link href="/leaderboard">
          <button className="flex items-center gap-2 px-3 py-1.5 rounded-lg text-sm font-bold text-muted-foreground hover:bg-input hover:text-foreground transition-all">
            <Trophy className="h-4 w-4 text-primary" />
            <span className="hidden sm:inline">Leaderboard</span>
          </button>
        </Link>
        <Link href="/history">
          <button className="flex items-center gap-2 px-3 py-1.5 rounded-lg text-sm font-bold text-muted-foreground hover:bg-input hover:text-foreground transition-all">
            <History className="h-4 w-4 text-primary" />
            <span className="hidden sm:inline">My History</span>
          </button>
        </Link>
        <div className="w-[1px] h-6 bg-border mx-1"></div>
        <UserSearch />
      </nav>

      <div className="flex items-center gap-4">
        {user?.role === 'Admin' && (
          <Link href="/admin">
            <button className="flex items-center gap-2 px-3 py-1.5 rounded-lg text-sm font-bold bg-primary/10 text-primary border border-primary/20 hover:bg-primary/20 transition-all">
              Admin Paneli
            </button>
          </Link>
        )}
        {user && (
          <Link href="/profile" className="hidden md:flex flex-col items-end group cursor-pointer">
            <span className="text-sm font-semibold text-foreground group-hover:text-primary transition-colors">{user.username}</span>
            <div className="flex items-center gap-1 text-xs text-muted-foreground bg-card border border-border px-2 py-0.5 rounded-full">
              <Trophy className="h-3 w-3 text-primary" />
              <span>{user.eloRating} Elo</span>
            </div>
          </Link>
        )}
        <button onClick={handleLogout} title="Logout" className="p-2 rounded-lg text-muted-foreground hover:text-foreground hover:bg-card border border-transparent hover:border-border transition-all">
          <LogOut className="h-5 w-5" />
        </button>
      </div>
    </header>
  );
}
