'use client';

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import { usersApi } from '@/lib/api';
import { Swords, LogOut, Trophy, History, User as UserIcon, Edit3, CheckCircle2, XCircle, Loader2, ShieldCheck, BarChart3, CalendarDays, ArrowLeft } from 'lucide-react';
import Cookies from 'js-cookie';

interface UserProfile {
  id: string; username: string; email: string;
  eloRating: number; totalWins: number; totalLosses: number; totalGames: number; createdAt: string;
}

function hashUsername(name: string): number {
  let h = 0;
  for (let i = 0; i < name.length; i++) h = (Math.imul(31, h) + name.charCodeAt(i)) | 0;
  return Math.abs(h);
}

// Amber-red gradient palette tones for avatars
const AVATAR_PALETTE = [
  ['#f59e0b', '#ef4444'], ['#f59e0b', '#f97316'], ['#ef4444', '#ec4899'],
  ['#f97316', '#f59e0b'], ['#fbbf24', '#f59e0b'], ['#ef4444', '#f59e0b'],
  ['#f59e0b', '#dc2626'], ['#fb923c', '#ef4444'],
] as const;

function getAvatarColors(name: string): [string, string] {
  const p = AVATAR_PALETTE[hashUsername(name) % AVATAR_PALETTE.length];
  return [p[0], p[1]];
}

const USERNAME_REGEX = /^[a-zA-Z0-9_]+$/;

interface ToastProps { message: string; type: 'success' | 'error' }
function Toast({ message, type }: ToastProps) {
  return (
    <div className={`fixed bottom-6 left-1/2 -translate-x-1/2 z-50 flex items-center gap-3 px-5 py-3 rounded-2xl shadow-2xl border text-sm font-semibold animate-in slide-in-from-bottom-4 fade-in duration-300 ${type === 'success' ? 'bg-[#0d1117] border-[#3fb950]/40 text-[#3fb950]' : 'bg-[#0d1117] border-destructive/40 text-destructive'}`}>
      {type === 'success' ? <CheckCircle2 className="h-5 w-5 shrink-0" /> : <XCircle className="h-5 w-5 shrink-0" />}
      {message}
    </div>
  );
}

interface StatData {
  categoryName: string;
  categorySlug: string;
  correctAnswersCount: number;
  totalAnswersCount: number;
  successRate: number;
}

function RadarChart({ stats }: { stats: StatData[] }) {
  const chartStats = [...stats];
  
  // A radar chart needs at least 5 axes. If the user has fewer than 5, pad with default ones.
  const minimumAxes = 5;
  const defaultList = [
    { categoryName: 'Science', successRate: 0, correctAnswersCount: 0, totalAnswersCount: 0 },
    { categoryName: 'History', successRate: 0, correctAnswersCount: 0, totalAnswersCount: 0 },
    { categoryName: 'Geography', successRate: 0, correctAnswersCount: 0, totalAnswersCount: 0 },
    { categoryName: 'Sports', successRate: 0, correctAnswersCount: 0, totalAnswersCount: 0 },
    { categoryName: 'Entertainment', successRate: 0, correctAnswersCount: 0, totalAnswersCount: 0 },
  ];

  while (chartStats.length < minimumAxes) {
    const missingName = defaultList[chartStats.length].categoryName;
    if (!chartStats.some(s => s.categoryName.toLowerCase().includes(missingName.toLowerCase()))) {
      chartStats.push({
        categoryName: missingName,
        categorySlug: missingName.toLowerCase(),
        correctAnswersCount: 0,
        totalAnswersCount: 0,
        successRate: 0
      });
    } else {
      chartStats.push({
        categoryName: missingName + " II",
        categorySlug: missingName.toLowerCase() + "-ii",
        correctAnswersCount: 0,
        totalAnswersCount: 0,
        successRate: 0
      });
    }
  }

  const width = 300;
  const height = 300;
  const cx = width / 2;
  const cy = height / 2;
  const r = 90; // max radius for 100% success rate
  const N = chartStats.length;

  // Grid levels (20%, 40%, 60%, 80%, 100%)
  const gridLevels = [0.2, 0.4, 0.6, 0.8, 1.0];

  // Helper to get coordinates
  const getCoordinates = (index: number, value: number) => {
    const angle = (index * 2 * Math.PI) / N - Math.PI / 2;
    const currentRadius = (value / 100) * r;
    return {
      x: cx + currentRadius * Math.cos(angle),
      y: cy + currentRadius * Math.sin(angle),
    };
  };

  // Generate grid points for each level
  const getGridPoints = (level: number) => {
    const points = [];
    for (let i = 0; i < N; i++) {
      const p = getCoordinates(i, level * 100);
      points.push(`${p.x},${p.y}`);
    }
    return points.join(' ');
  };

  // Generate user polygon points
  const userPoints = chartStats.map((stat, i) => {
    const p = getCoordinates(i, stat.successRate);
    return `${p.x},${p.y}`;
  }).join(' ');

  const [hoveredStat, setHoveredStat] = useState<StatData | null>(null);
  const [hoveredPos, setHoveredPos] = useState<{ x: number, y: number } | null>(null);

  return (
    <div className="flex flex-col items-center justify-center p-6 bg-card border border-border rounded-3xl relative overflow-visible h-full shadow-lg">
      <h3 className="text-xs font-black uppercase tracking-widest text-muted-foreground mb-4">Category Mastery</h3>
      
      <div className="relative w-[300px] h-[300px] select-none">
        <svg width={width} height={height} className="overflow-visible">
          {/* Definitions for gradients and drop-shadow glow */}
          <defs>
            <linearGradient id="radar-gradient" x1="0%" y1="0%" x2="100%" y2="100%">
              <stop offset="0%" stopColor="#f59e0b" stopOpacity="0.45" stop-opacity="0.45" />
              <stop offset="100%" stopColor="#ef4444" stopOpacity="0.45" stop-opacity="0.45" />
            </linearGradient>
            <linearGradient id="radar-stroke" x1="0%" y1="0%" x2="100%" y2="100%">
              <stop offset="0%" stopColor="#f59e0b" />
              <stop offset="100%" stopColor="#ef4444" />
            </linearGradient>
          </defs>

          {/* Grid lines */}
          {gridLevels.map((level, idx) => (
            <polygon
              key={idx}
              points={getGridPoints(level)}
              fill="none"
              stroke="currentColor"
              className="text-border/30"
              strokeWidth="1"
              strokeDasharray={idx === gridLevels.length - 1 ? "0" : "3 3"}
            />
          ))}

          {/* Axis lines */}
          {chartStats.map((_, i) => {
            const p = getCoordinates(i, 100);
            return (
              <line
                key={i}
                x1={cx}
                y1={cy}
                x2={p.x}
                y2={p.y}
                stroke="currentColor"
                className="text-border/20"
                strokeWidth="1"
              />
            );
          })}

          {/* User Stat Area Polygon */}
          <polygon
            points={userPoints}
            fill="url(#radar-gradient)"
            stroke="url(#radar-stroke)"
            strokeWidth="3"
            style={{ filter: 'drop-shadow(0px 0px 8px rgba(245, 158, 11, 0.45))' }}
          />

          {/* Category Labels */}
          {chartStats.map((stat, i) => {
            const angle = (i * 2 * Math.PI) / N - Math.PI / 2;
            const labelDist = r + 18;
            const lx = cx + labelDist * Math.cos(angle);
            const ly = cy + labelDist * Math.sin(angle);

            let textAnchor = 'middle';
            const cos = Math.cos(angle);
            if (cos > 0.1) textAnchor = 'start';
            else if (cos < -0.1) textAnchor = 'end';

            let dy = '0.35em';
            const sin = Math.sin(angle);
            if (sin > 0.8) dy = '0.8em';
            else if (sin < -0.8) dy = '-0.1em';

            const displayName = stat.categoryName.length > 15 
              ? stat.categoryName.slice(0, 13) + '..' 
              : stat.categoryName;

            return (
              <text
                key={i}
                x={lx}
                y={ly}
                textAnchor={textAnchor}
                dy={dy}
                className="text-[9px] font-black uppercase tracking-tighter fill-muted-foreground select-none"
              >
                {displayName}
              </text>
            );
          })}

          {/* Data interactive dots */}
          {chartStats.map((stat, i) => {
            const p = getCoordinates(i, stat.successRate);
            const isHovered = hoveredStat?.categoryName === stat.categoryName;
            return (
              <circle
                key={i}
                cx={p.x}
                cy={p.y}
                r={isHovered ? 6 : 4}
                fill={isHovered ? '#ffffff' : '#f59e0b'}
                stroke="#161b22"
                strokeWidth={1.5}
                className="cursor-pointer transition-all duration-200"
                onMouseEnter={() => {
                  setHoveredStat(stat);
                  setHoveredPos(p);
                }}
                onMouseLeave={() => {
                  setHoveredStat(null);
                  setHoveredPos(null);
                }}
              />
            );
          })}
        </svg>

        {/* Floating HTML tooltip */}
        {hoveredStat && hoveredPos && (
          <div
            className="absolute z-50 bg-[#0d1117]/95 border border-border/80 p-2.5 rounded-xl shadow-2xl flex flex-col gap-1 min-w-[130px] pointer-events-none animate-in fade-in zoom-in-95 duration-150"
            style={{
              left: `${hoveredPos.x}px`,
              top: `${hoveredPos.y - 8}px`,
              transform: 'translate(-50%, -100%)',
            }}
          >
            <span className="text-[9px] font-black uppercase text-primary tracking-wide truncate">
              {hoveredStat.categoryName}
            </span>
            <div className="flex justify-between items-center text-[11px] font-semibold text-foreground">
              <span>Mastery:</span>
              <span className="font-black text-primary">{hoveredStat.successRate}%</span>
            </div>
            <div className="flex justify-between items-center text-[9px] text-muted-foreground">
              <span>Correct:</span>
              <span>{hoveredStat.correctAnswersCount} / {hoveredStat.totalAnswersCount}</span>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}

export default function ProfilePage() {
  const router = useRouter();
  const [user, setUser] = useState<UserProfile | null>(null);
  const [stats, setStats] = useState<StatData[]>([]);
  const [loading, setLoading] = useState(true);
  const [newUsername, setNewUsername] = useState('');
  const [clientError, setClientError] = useState('');
  const [saving, setSaving] = useState(false);
  const [toast, setToast] = useState<ToastProps | null>(null);

  const showToast = (message: string, type: 'success' | 'error') => {
    setToast({ message, type });
    setTimeout(() => setToast(null), 3500);
  };

  useEffect(() => {
    Promise.all([usersApi.me(), usersApi.stats()])
      .then(([meRes, statsRes]) => { 
        setUser(meRes.data); 
        setNewUsername(meRes.data.username); 
        setStats(statsRes.data || []);
      })
      .catch(() => router.push('/login'))
      .finally(() => setLoading(false));
  }, [router]);

  const validateUsername = (val: string): string => {
    if (!val.trim()) return 'Username is required.';
    if (val.length < 3) return 'Must be at least 3 characters.';
    if (val.length > 20) return 'Must be at most 20 characters.';
    if (!USERNAME_REGEX.test(val)) return 'Letters, numbers and underscores only.';
    return '';
  };

  const handleUsernameChange = (val: string) => { setNewUsername(val); setClientError(validateUsername(val)); };

  const handleSave = async (e: React.FormEvent) => {
    e.preventDefault();
    const err = validateUsername(newUsername);
    if (err) { setClientError(err); return; }
    setSaving(true);
    try {
      const res = await usersApi.updateMe({ newUsername });
      if (res.success) { setUser(res.data); showToast('Username updated!', 'success'); }
      else showToast(res.message ?? 'Update failed.', 'error');
    } catch (err: any) {
      showToast(err?.response?.data?.message ?? 'Something went wrong.', 'error');
    } finally { setSaving(false); }
  };

  const handleLogout = () => { Cookies.remove('token'); Cookies.remove('refreshToken'); router.push('/login'); };

  if (loading) return <div className="flex items-center justify-center min-h-screen bg-background"><Loader2 className="h-12 w-12 animate-spin text-primary" /></div>;
  if (!user) return null;

  const [avatarFrom, avatarTo] = getAvatarColors(user.username);
  const initials = user.username.slice(0, 2).toUpperCase();
  const winRate = user.totalWins + user.totalLosses === 0 ? '0.0' : ((user.totalWins / (user.totalWins + user.totalLosses)) * 100).toFixed(1);
  const memberSince = new Date(user.createdAt).toLocaleDateString('en-US', { year: 'numeric', month: 'long', day: 'numeric' });

  const inputCls = (err: boolean) => `w-full h-11 px-4 rounded-xl bg-input border text-sm font-semibold outline-none transition-all placeholder:text-muted-foreground/40 ${err ? 'border-destructive focus:border-destructive' : 'border-border focus:border-primary focus:ring-2 focus:ring-primary/20'}`;

  return (
    <div className="min-h-screen bg-background text-foreground flex flex-col">
      {/* Top Bar */}
      <header className="sticky top-0 z-10 border-b border-border bg-background/80 backdrop-blur-xl px-6 py-4 flex items-center justify-between">
        <Link href="/lobby" className="flex items-center gap-2">
          <div className="bg-gradient-accent p-1.5 rounded-lg"><Swords className="h-5 w-5 text-white" /></div>
          <span className="text-xl font-black tracking-tighter text-gradient-accent">SKILLDUEL</span>
        </Link>

        <nav className="hidden lg:flex items-center gap-1 bg-card p-1 rounded-xl border border-border">
          <Link href="/leaderboard"><button className="flex items-center gap-2 px-3 py-1.5 rounded-lg text-sm font-bold text-muted-foreground hover:bg-input hover:text-foreground transition-all"><Trophy className="h-4 w-4 text-primary" />Leaderboard</button></Link>
          <Link href="/history"><button className="flex items-center gap-2 px-3 py-1.5 rounded-lg text-sm font-bold text-muted-foreground hover:bg-input hover:text-foreground transition-all"><History className="h-4 w-4 text-primary" />My History</button></Link>
          <button className="flex items-center gap-2 px-3 py-1.5 rounded-lg text-sm font-bold bg-primary/10 text-primary"><UserIcon className="h-4 w-4" />Profile</button>
        </nav>

        <button onClick={handleLogout} title="Logout" className="p-2 rounded-lg text-muted-foreground hover:text-foreground hover:bg-card border border-transparent hover:border-border transition-all">
          <LogOut className="h-5 w-5" />
        </button>
      </header>

      {/* Main */}
      <main className="flex-1 max-w-4xl mx-auto w-full px-4 py-10 space-y-8">
        <Link href="/lobby" className="inline-flex items-center gap-2 text-sm text-muted-foreground hover:text-foreground transition-colors">
          <ArrowLeft className="h-4 w-4" />Back to Lobby
        </Link>

        {/* Hero / identity */}
        <div className="relative overflow-hidden rounded-3xl border border-border bg-card p-8 flex flex-col sm:flex-row items-center sm:items-start gap-6">
          <div className="pointer-events-none absolute -top-24 -right-24 w-72 h-72 rounded-full opacity-20 blur-3xl" style={{ background: `radial-gradient(circle, ${avatarFrom}, transparent 70%)` }} />

          {/* Amber-toned avatar */}
          <div className="shrink-0 w-24 h-24 rounded-2xl flex items-center justify-center text-3xl font-black text-white shadow-xl" style={{ background: `linear-gradient(135deg, ${avatarFrom}, ${avatarTo})` }}>
            {initials}
          </div>

          <div className="flex-1 flex flex-col items-center sm:items-start gap-1 text-center sm:text-left">
            <h1 className="text-3xl font-black tracking-tight text-foreground">{user.username}</h1>
            <p className="text-muted-foreground text-sm">{user.email}</p>
            <div className="mt-2 flex flex-wrap justify-center sm:justify-start gap-3 text-xs">
              <span className="flex items-center gap-1.5 bg-input border border-border rounded-full px-3 py-1 font-semibold text-muted-foreground">
                <CalendarDays className="h-3.5 w-3.5" />Member since {memberSince}
              </span>
              <span className="flex items-center gap-1.5 bg-primary/10 border border-primary/25 rounded-full px-3 py-1 font-semibold text-primary">
                <Trophy className="h-3.5 w-3.5" />{user.eloRating} Elo
              </span>
            </div>
          </div>
        </div>

        {/* Stats & Radar Mastery Layout */}
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6 items-stretch">
          {/* Numeric Stats */}
          <div className="lg:col-span-2 rounded-3xl border border-border bg-card p-8 flex flex-col justify-between">
            <div>
              <div className="flex items-center gap-3 mb-6">
                <div className="p-2 bg-primary/10 rounded-xl"><BarChart3 className="h-5 w-5 text-primary" /></div>
                <h2 className="text-xl font-bold text-foreground">Statistics</h2>
              </div>
              <div className="grid grid-cols-2 sm:grid-cols-3 gap-3">
                {[
                  { label: 'Elo Rating', value: user.eloRating, cls: 'bg-input border-border text-foreground' },
                  { label: 'Win Rate',   value: `${winRate}%`,  cls: 'bg-input border-border text-foreground' },
                  { label: 'Wins',       value: user.totalWins,    cls: 'bg-[#3fb950]/10 border-[#3fb950]/20 text-[#3fb950]' },
                  { label: 'Losses',     value: user.totalLosses,  cls: 'bg-destructive/10 border-destructive/20 text-destructive' },
                  { label: 'Total Games',value: user.totalGames,   cls: 'bg-input border-border text-foreground' },
                ].map(({ label, value, cls }) => (
                  <div key={label} className={`flex flex-col items-center justify-center gap-1 p-5 rounded-2xl border ${cls}`}>
                    <span className="text-[10px] font-black uppercase tracking-[0.15em] text-muted-foreground">{label}</span>
                    <span className="text-3xl font-black tabular-nums">{value}</span>
                  </div>
                ))}
              </div>
            </div>
          </div>

          {/* Radar Chart */}
          <div className="lg:col-span-1">
            <RadarChart stats={stats} />
          </div>
        </div>

        {/* Edit username */}
        <section className="rounded-3xl border border-border bg-card p-8">
          <div className="flex items-center gap-3 mb-6">
            <div className="p-2 bg-primary/10 rounded-xl"><Edit3 className="h-5 w-5 text-primary" /></div>
            <h2 className="text-xl font-bold text-foreground">Edit Profile</h2>
          </div>

          <form onSubmit={handleSave} className="space-y-5 max-w-sm">
            <div className="space-y-1.5">
              <label className="text-xs font-bold uppercase tracking-widest text-muted-foreground">Current Username</label>
              <div className="h-11 px-4 rounded-xl bg-input border border-border flex items-center text-sm text-muted-foreground font-semibold">{user.username}</div>
            </div>

            <div className="space-y-1.5">
              <label htmlFor="new-username" className="text-xs font-bold uppercase tracking-widest text-muted-foreground">New Username</label>
              <input id="new-username" type="text" value={newUsername} onChange={e => handleUsernameChange(e.target.value)} placeholder="e.g. SkillDueler_99" maxLength={20} className={inputCls(!!clientError)} />
              {clientError && (
                <p className="text-xs text-destructive flex items-center gap-1.5 animate-in fade-in slide-in-from-top-1 duration-200">
                  <XCircle className="h-3.5 w-3.5 shrink-0" />{clientError}
                </p>
              )}
              <p className="text-xs text-muted-foreground">3–20 characters · letters, numbers and underscores only</p>
            </div>

            <button id="save-username-btn" type="submit" disabled={saving || !!clientError || newUsername === user.username}
              className="flex items-center justify-center gap-2 w-full h-11 rounded-xl bg-gradient-accent text-white font-bold text-sm hover:opacity-90 active:scale-[0.98] transition-all disabled:opacity-40 disabled:cursor-not-allowed">
              {saving ? <><Loader2 className="h-4 w-4 animate-spin" />Saving…</> : <><ShieldCheck className="h-4 w-4" />Save Username</>}
            </button>
          </form>
        </section>
      </main>

      {toast && <Toast message={toast.message} type={toast.type} />}
    </div>
  );
}
