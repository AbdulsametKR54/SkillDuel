'use client';

import { useState, useEffect } from 'react';
import { adminApi, questionsApi, categoriesApi } from '@/lib/api';
import { 
  Loader2, CheckCircle, XCircle, Swords, ArrowLeft, Trash2, 
  ChevronLeft, ChevronRight, Search, ShieldAlert, UserCheck, 
  UserX, Calendar, Layers, Activity, Mail, Trophy, Award, 
  ChevronDown, ChevronUp, SlidersHorizontal, Trash, Ban, Check
} from 'lucide-react';
import Link from 'next/link';
import { toast } from 'sonner';

interface PendingQuestion {
  id: string;
  text: string;
  options: string[];
  correctOptionIndex: number;
  categoryName: string;
  difficultyLevel: number;
  questionType: number;
  createdAt: string;
  submittedByUsername?: string;
}

interface Question {
  id: string;
  text: string;
  options: string[];
  correctOptionIndex: number;
  categoryId: string;
  difficultyLevel: number;
  questionType: number;
  createdAt: string;
  createdByUser?: { username: string };
  createdByUserId?: string;
  category?: { name: string };
}

interface AdminUser {
  id: string;
  username: string;
  email: string;
  role: string;
  elo: number;
  totalGames: number;
  joinedDate: string;
  status: 'Active' | 'Banned';
}

const diffLabel = (d: number) => d === 0 ? 'Easy' : d === 1 ? 'Medium' : 'Hard';
const diffColor = (d: number) => 
  d === 0 ? 'text-[#3fb950] bg-[#3fb950]/10 border-[#3fb950]/20' : 
  d === 1 ? 'text-primary bg-primary/10 border-primary/20' : 
  'text-destructive bg-destructive/10 border-destructive/20';

export default function AdminQuestionsPage() {
  const [activeTab, setActiveTab] = useState<'pending' | 'all' | 'users'>('pending');
  
  // Pending Questions State
  const [pendingQuestions, setPendingQuestions] = useState<PendingQuestion[]>([]);
  const [pendingLoading, setPendingLoading] = useState(false);
  const [expandedPending, setExpandedPending] = useState<Set<string>>(new Set());

  // All Questions State
  const [allQuestions, setAllQuestions] = useState<Question[]>([]);
  const [allQuestionsLoading, setAllQuestionsLoading] = useState(false);
  const [categories, setCategories] = useState<{ id: string; name: string }[]>([]);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  
  // Filters
  const [filterCategory, setFilterCategory] = useState<string>('');
  const [filterDifficulty, setFilterDifficulty] = useState<string>('');
  const [filterType, setFilterType] = useState<string>('');

  // User Management State
  const [users, setUsers] = useState<AdminUser[]>([]);
  const [usersLoading, setUsersLoading] = useState(false);
  const [userPage, setUserPage] = useState(1);
  const [userTotalPages, setUserTotalPages] = useState(1);
  const [userSearch, setUserSearch] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');

  // Load Categories on mount
  useEffect(() => {
    categoriesApi.list().then(res => setCategories(res.data || [])).catch(console.error);
  }, []);

  // Debounce user search input
  useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedSearch(userSearch);
      setUserPage(1);
    }, 400);
    return () => clearTimeout(timer);
  }, [userSearch]);

  const fetchPending = async () => {
    setPendingLoading(true);
    try {
      const res = await adminApi.getPendingQuestions();
      if (res.success) {
        setPendingQuestions(res.data || []);
      }
    } catch (err) {
      console.error(err);
      toast.error('Bekleyen sorular yüklenemedi.');
    } finally {
      setPendingLoading(false);
    }
  };

  const fetchAll = async (p: number) => {
    setAllQuestionsLoading(true);
    try {
      const res = await questionsApi.list({ 
        page: p, 
        pageSize: 10,
        categoryId: filterCategory || undefined,
        difficulty: filterDifficulty || undefined,
        questionType: filterType || undefined
      });
      setAllQuestions(res.items || []);
      setTotalPages(res.totalPages || 1);
    } catch (err) {
      console.error(err);
      toast.error('Sorular yüklenemedi.');
    } finally {
      setAllQuestionsLoading(false);
    }
  };

  const fetchUsers = async (p: number, search: string) => {
    setUsersLoading(true);
    try {
      const res = await adminApi.getUsers(p, search);
      setUsers(res.users || []);
      // Res contains totalCount, we divide by page size (10) to get total pages
      const total = res.totalCount || 0;
      setUserTotalPages(Math.max(1, Math.ceil(total / 10)));
    } catch (err) {
      console.error(err);
      toast.error('Kullanıcılar yüklenemedi.');
    } finally {
      setUsersLoading(false);
    }
  };

  // Main Effect
  useEffect(() => {
    if (activeTab === 'pending') {
      fetchPending();
    } else if (activeTab === 'all') {
      fetchAll(page);
    } else if (activeTab === 'users') {
      fetchUsers(userPage, debouncedSearch);
    }
  }, [activeTab, page, userPage, debouncedSearch, filterCategory, filterDifficulty, filterType]);

  // Actions
  const handleApprove = async (id: string) => {
    // Optimistic Update
    const questionToApprove = pendingQuestions.find(q => q.id === id);
    setPendingQuestions(prev => prev.filter(q => q.id !== id));
    toast.success('Soru başarıyla onaylandı.');

    try {
      await adminApi.approveQuestion(id);
    } catch (err) {
      console.error('Failed to approve', err);
      toast.error('Onaylama işlemi sırasında bir hata oluştu.');
      // Revert optimistic update
      if (questionToApprove) {
        setPendingQuestions(prev => [questionToApprove, ...prev]);
      }
    }
  };

  const handleReject = async (id: string) => {
    if (!confirm('Bu soruyu reddetmek istediğinizden emin misiniz?')) return;
    
    // Optimistic Update
    const questionToReject = pendingQuestions.find(q => q.id === id);
    setPendingQuestions(prev => prev.filter(q => q.id !== id));
    toast.info('Soru reddedildi.');

    try {
      await adminApi.rejectQuestion(id);
    } catch (err) {
      console.error('Failed to reject', err);
      toast.error('Reddetme işlemi sırasında bir hata oluştu.');
      if (questionToReject) {
        setPendingQuestions(prev => [questionToReject, ...prev]);
      }
    }
  };

  const handleDelete = async (id: string) => {
    if (!confirm('Bu soruyu kalıcı olarak silmek istediğinizden emin misiniz?')) return;
    
    // Optimistic Update
    const oldQuestions = [...allQuestions];
    setAllQuestions(prev => prev.filter(q => q.id !== id));
    toast.success('Soru kalıcı olarak silindi.');

    try {
      await questionsApi.delete(id);
    } catch (err) {
      console.error('Failed to delete', err);
      toast.error('Silme işlemi başarısız oldu.');
      setAllQuestions(oldQuestions);
    }
  };

  const handleBan = async (id: string) => {
    // Optimistic Update
    setUsers(prev => prev.map(u => u.id === id ? { ...u, status: 'Banned' } : u));
    toast.warning('Kullanıcı banlandı.');

    try {
      await adminApi.banUser(id);
    } catch (err) {
      console.error('Failed to ban user', err);
      toast.error('Ban işlemi başarısız oldu.');
      // Revert
      setUsers(prev => prev.map(u => u.id === id ? { ...u, status: 'Active' } : u));
    }
  };

  const handleUnban = async (id: string) => {
    // Optimistic Update
    setUsers(prev => prev.map(u => u.id === id ? { ...u, status: 'Active' } : u));
    toast.success('Kullanıcı banı kaldırıldı.');

    try {
      await adminApi.unbanUser(id);
    } catch (err) {
      console.error('Failed to unban user', err);
      toast.error('Ban kaldırma işlemi başarısız oldu.');
      // Revert
      setUsers(prev => prev.map(u => u.id === id ? { ...u, status: 'Banned' } : u));
    }
  };

  const handleMakeAdmin = async (id: string) => {
    if (!confirm('Bu kullanıcıyı Admin yapmak istediğinize emin misiniz?')) return;
    
    // Optimistic Update
    setUsers(prev => prev.map(u => u.id === id ? { ...u, role: 'Admin' } : u));
    toast.success('Kullanıcı rolü Admin olarak güncellendi.');

    try {
      await adminApi.updateUserRole(id, 'Admin');
    } catch (err) {
      console.error('Failed to make admin', err);
      toast.error('Rol güncelleme başarısız oldu.');
      // Revert
      setUsers(prev => prev.map(u => u.id === id ? { ...u, role: 'User' } : u));
    }
  };

  const handleRemoveAdmin = async (id: string) => {
    if (!confirm('Bu kullanıcının yetkisini normal kullanıcıya (\'User\') düşürmek istediğinizden emin misiniz?')) return;
    
    // Optimistic Update
    setUsers(prev => prev.map(u => u.id === id ? { ...u, role: 'User' } : u));
    toast.success('Kullanıcı yetkisi normal kullanıcı (User) olarak güncellendi.');

    try {
      await adminApi.updateUserRole(id, 'User');
    } catch (err) {
      console.error('Failed to remove admin role', err);
      toast.error('Rol güncelleme başarısız oldu.');
      // Revert
      setUsers(prev => prev.map(u => u.id === id ? { ...u, role: 'Admin' } : u));
    }
  };

  const togglePendingExpand = (id: string) => {
    setExpandedPending(prev => {
      const next = new Set(prev);
      if (next.has(id)) {
        next.delete(id);
      } else {
        next.add(id);
      }
      return next;
    });
  };

  const getCategoryName = (id: string) => categories.find(c => c.id === id)?.name || 'Unknown';

  const tabCls = (active: boolean) =>
    `pb-3 px-4 font-bold border-b-2 transition-colors text-sm uppercase tracking-wider flex items-center gap-2 ${
      active 
        ? 'border-primary text-primary shadow-[0_4px_12px_rgba(251,191,36,0.05)]' 
        : 'border-transparent text-muted-foreground hover:text-foreground'
    }`;

  return (
    <div className="min-h-screen bg-background text-foreground flex flex-col font-sans">
      {/* Top Bar */}
      <header className="sticky top-0 z-10 bg-card border-b border-border px-6 py-4 flex items-center justify-between shadow-lg">
        <div className="flex items-center gap-3">
          <Link href="/lobby">
            <button className="p-2.5 rounded-xl bg-input border border-border text-muted-foreground hover:text-foreground hover:border-primary/40 transition-all active:scale-95">
              <ArrowLeft className="h-4 w-4" />
            </button>
          </Link>
          <div className="flex items-center gap-2">
            <div className="bg-gradient-accent p-2 rounded-lg"><Swords className="h-4 w-4 text-white" /></div>
            <span className="text-lg font-black tracking-tighter text-gradient-accent">SKILLDUEL</span>
          </div>
        </div>
        <div className="flex items-center gap-2 text-xs font-black uppercase bg-primary/10 border border-primary/20 px-3 py-1.5 rounded-full text-primary">
          <Activity className="h-3.5 w-3.5 animate-pulse" /> Yönetim Paneli
        </div>
      </header>

      <main className="flex-1 max-w-6xl w-full mx-auto p-6 md:p-10 space-y-8">
        {/* Title Section */}
        <div className="flex flex-col md:flex-row justify-between items-start md:items-center gap-4 bg-card p-6 rounded-3xl border border-border">
          <div>
            <h2 className="text-3xl font-black tracking-tight text-foreground flex items-center gap-2">
              Yönetim Kontrol Paneli
            </h2>
            <p className="text-sm text-muted-foreground mt-1">Soruları onaylayın, soru havuzunu düzenleyin ve kullanıcı durumlarını yönetin.</p>
          </div>
          <Link href="/lobby">
            <button className="px-5 py-2.5 rounded-xl bg-gradient-accent text-white font-black text-xs uppercase tracking-wider shadow-lg hover:opacity-90 active:scale-95 transition-all">
              OYUN LOBİSİNE DÖN
            </button>
          </Link>
        </div>

        {/* Tabs Selection */}
        <div className="flex gap-2 border-b border-border overflow-x-auto scrollbar-hide">
          <button className={tabCls(activeTab === 'pending')} onClick={() => { setActiveTab('pending'); setPage(1); }}>
            <ShieldAlert className="h-4 w-4" />
            Bekleyen Sorular
            {pendingQuestions.length > 0 && (
              <span className="bg-destructive text-white text-[10px] font-black px-2 py-0.5 rounded-full animate-pulse">{pendingQuestions.length}</span>
            )}
          </button>
          <button className={tabCls(activeTab === 'all')} onClick={() => { setActiveTab('all'); setPage(1); }}>
            <Layers className="h-4 w-4" />
            Tüm Sorular
          </button>
          <button className={tabCls(activeTab === 'users')} onClick={() => { setActiveTab('users'); setUserPage(1); }}>
            <UserCheck className="h-4 w-4" />
            Kullanıcı Yönetimi
          </button>
        </div>

        {/* Tab CONTENT 1: Pending Questions */}
        {activeTab === 'pending' && (
          <div className="space-y-4">
            {pendingLoading ? (
              <div className="flex flex-col justify-center items-center py-20 gap-3">
                <Loader2 className="h-10 w-10 animate-spin text-primary" />
                <span className="text-xs text-muted-foreground font-semibold">Sorular yükleniyor...</span>
              </div>
            ) : pendingQuestions.length === 0 ? (
              <div className="bg-card border border-dashed border-border rounded-3xl p-16 text-center text-muted-foreground flex flex-col items-center justify-center gap-3">
                <div className="h-12 w-12 rounded-full bg-input flex items-center justify-center"><Check className="h-6 w-6 text-primary" /></div>
                <div>
                  <h3 className="font-bold text-foreground">İncelenecek Soru Yok</h3>
                  <p className="text-xs text-muted-foreground mt-1">Şu anda onay bekleyen herhangi bir soru bulunmuyor.</p>
                </div>
              </div>
            ) : (
              <div className="space-y-4">
                {pendingQuestions.map(q => {
                  const isExpanded = expandedPending.has(q.id);
                  return (
                    <div key={q.id} className="bg-card border border-border rounded-2xl hover:border-primary/30 transition-all overflow-hidden flex flex-col">
                      <div className="p-6 flex flex-col md:flex-row justify-between items-start md:items-center gap-4">
                        <div className="flex-1 space-y-3">
                          <div className="flex flex-wrap items-center gap-2">
                            <span className="text-[10px] font-black uppercase tracking-widest bg-input border border-border px-2.5 py-1 rounded-lg text-muted-foreground">{q.categoryName}</span>
                            <span className={`text-[10px] font-black uppercase tracking-widest px-2.5 py-1 rounded-lg border ${diffColor(q.difficultyLevel)}`}>{diffLabel(q.difficultyLevel)}</span>
                            <span className="text-[10px] font-black uppercase tracking-widest bg-input border border-border px-2.5 py-1 rounded-lg text-muted-foreground">
                              {q.questionType === 1 ? 'Çoktan Seçmeli' : 'Doğru/Yanlış'}
                            </span>
                          </div>
                          <h3 className="text-base font-bold text-foreground pr-4 leading-relaxed">{q.text}</h3>
                          
                          {/* Collapsed view indicator or collapsed options preview */}
                          {!isExpanded && (
                            <button 
                              onClick={() => togglePendingExpand(q.id)}
                              className="text-xs font-bold text-primary flex items-center gap-1 hover:underline pt-1"
                            >
                              Seçenekleri Göster ({q.options?.length || 0}) <ChevronDown className="h-3 w-3" />
                            </button>
                          )}
                        </div>

                        {/* Fast actions on header */}
                        <div className="flex gap-2 self-stretch md:self-center justify-end md:border-l md:border-border md:pl-6">
                          <button 
                            onClick={() => handleApprove(q.id)}
                            className="flex items-center justify-center gap-1.5 px-4 h-10 rounded-xl bg-gradient-accent text-white font-black text-xs uppercase tracking-wider hover:opacity-90 active:scale-95 transition-all"
                            title="Onayla"
                          >
                            <CheckCircle className="w-4 h-4" /> Onayla
                          </button>
                          <button 
                            onClick={() => handleReject(q.id)}
                            className="flex items-center justify-center gap-1.5 px-4 h-10 rounded-xl bg-destructive/10 border border-destructive/20 text-destructive font-black text-xs uppercase tracking-wider hover:bg-destructive hover:text-white active:scale-95 transition-all"
                            title="Reddet"
                          >
                            <XCircle className="w-4 h-4" /> Reddet
                          </button>
                        </div>
                      </div>

                      {/* Expandable Options Panel */}
                      {isExpanded && (
                        <div className="bg-input/40 border-t border-border p-6 space-y-4">
                          <div className="flex justify-between items-center">
                            <h4 className="text-xs font-black uppercase tracking-widest text-muted-foreground">Soru Seçenekleri</h4>
                            <button 
                              onClick={() => togglePendingExpand(q.id)}
                              className="text-xs font-bold text-muted-foreground flex items-center gap-1 hover:text-foreground"
                            >
                              Kapat <ChevronUp className="h-3 w-3" />
                            </button>
                          </div>

                          <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                            {q.options.map((opt, idx) => {
                              const isCorrect = idx === q.correctOptionIndex;
                              return (
                                <div 
                                  key={idx} 
                                  className={`p-3.5 rounded-xl border text-sm flex items-center justify-between transition-all ${
                                    isCorrect 
                                      ? 'bg-[#3fb950]/15 border-[#3fb950]/35 text-[#3fb950] font-bold shadow-[0_0_12px_rgba(63,185,80,0.05)]' 
                                      : 'bg-card border-border text-muted-foreground'
                                  }`}
                                >
                                  <span>{opt}</span>
                                  {isCorrect && <span className="bg-[#3fb950] text-white text-[10px] font-black px-2 py-0.5 rounded-full uppercase">DOĞRU</span>}
                                </div>
                              );
                            })}
                          </div>

                          {/* Extra info footer inside collapsed panel */}
                          <div className="flex flex-wrap items-center justify-between gap-2 pt-2 border-t border-border/60 text-xs text-muted-foreground">
                            <div className="flex items-center gap-1">
                              <Calendar className="h-3.5 w-3.5" />
                              <span>Gönderim Tarihi: <span className="font-bold text-foreground">{new Date(q.createdAt).toLocaleString()}</span></span>
                            </div>
                            <div>
                              <span>Gönderen: <span className="font-bold text-primary">@{q.submittedByUsername || 'Sistem'}</span></span>
                            </div>
                          </div>
                        </div>
                      )}
                    </div>
                  );
                })}
              </div>
            )}
          </div>
        )}

        {/* Tab CONTENT 2: All Questions */}
        {activeTab === 'all' && (
          <div className="space-y-6">
            {/* Filters Header Bar */}
            <div className="bg-card border border-border rounded-2xl p-6 space-y-4">
              <div className="flex items-center gap-2 text-sm font-black uppercase tracking-widest text-muted-foreground">
                <SlidersHorizontal className="h-4 w-4 text-primary" /> Filtreler & Arama
              </div>
              <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                {/* Category Filter */}
                <div className="flex flex-col gap-1.5">
                  <label className="text-[10px] font-black uppercase tracking-widest text-muted-foreground">Kategori</label>
                  <select 
                    value={filterCategory} 
                    onChange={(e) => { setFilterCategory(e.target.value); setPage(1); }}
                    className="w-full bg-input border border-border rounded-xl px-3 py-2.5 text-sm font-bold text-foreground outline-none focus:border-primary/50 transition-all"
                  >
                    <option value="">Tüm Kategoriler</option>
                    {categories.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
                  </select>
                </div>

                {/* Difficulty Filter */}
                <div className="flex flex-col gap-1.5">
                  <label className="text-[10px] font-black uppercase tracking-widest text-muted-foreground">Zorluk Derecesi</label>
                  <select 
                    value={filterDifficulty} 
                    onChange={(e) => { setFilterDifficulty(e.target.value); setPage(1); }}
                    className="w-full bg-input border border-border rounded-xl px-3 py-2.5 text-sm font-bold text-foreground outline-none focus:border-primary/50 transition-all"
                  >
                    <option value="">Tüm Zorluklar</option>
                    <option value="easy">Easy</option>
                    <option value="medium">Medium</option>
                    <option value="hard">Hard</option>
                  </select>
                </div>

                {/* Question Type Filter */}
                <div className="flex flex-col gap-1.5">
                  <label className="text-[10px] font-black uppercase tracking-widest text-muted-foreground">Soru Türü</label>
                  <select 
                    value={filterType} 
                    onChange={(e) => { setFilterType(e.target.value); setPage(1); }}
                    className="w-full bg-input border border-border rounded-xl px-3 py-2.5 text-sm font-bold text-foreground outline-none focus:border-primary/50 transition-all"
                  >
                    <option value="">Tüm Soru Türleri</option>
                    <option value="multiple">Çoktan Seçmeli</option>
                    <option value="truefalse">Doğru/Yanlış</option>
                  </select>
                </div>
              </div>
            </div>

            {/* Questions Table Container */}
            <div className="bg-card border border-border rounded-3xl overflow-hidden shadow-lg">
              <div className="overflow-x-auto">
                <table className="w-full text-left border-collapse">
                  <thead>
                    <tr className="bg-input/60 border-b border-border">
                      <th className="p-4 font-black uppercase tracking-widest text-xs text-muted-foreground">Soru Metni</th>
                      <th className="p-4 font-black uppercase tracking-widest text-xs text-muted-foreground">Kategori</th>
                      <th className="p-4 font-black uppercase tracking-widest text-xs text-muted-foreground">Zorluk</th>
                      <th className="p-4 font-black uppercase tracking-widest text-xs text-muted-foreground">Tür</th>
                      <th className="p-4 font-black uppercase tracking-widest text-xs text-muted-foreground">Kaynak</th>
                      <th className="p-4 font-black uppercase tracking-widest text-xs text-muted-foreground">Yazar</th>
                      <th className="p-4 font-black uppercase tracking-widest text-xs text-muted-foreground text-right">İşlem</th>
                    </tr>
                  </thead>
                  <tbody>
                    {allQuestionsLoading ? (
                      <tr>
                        <td colSpan={7} className="p-16 text-center">
                          <Loader2 className="h-10 w-10 animate-spin text-primary mx-auto" />
                          <span className="text-xs text-muted-foreground font-semibold mt-2 block">Sorular yükleniyor...</span>
                        </td>
                      </tr>
                    ) : allQuestions.length === 0 ? (
                      <tr>
                        <td colSpan={7} className="p-16 text-center text-muted-foreground">
                          Aranan filtrelerde onaylanmış soru bulunamadı.
                        </td>
                      </tr>
                    ) : (
                      allQuestions.map(q => {
                        const isUserCreated = !!q.createdByUserId;
                        const source = isUserCreated ? 'DB (Kullanıcı)' : 'API (Sistem)';
                        const createdBy = q.createdByUser?.username || 'Sistem';
                        
                        return (
                          <tr key={q.id} className="border-b border-border hover:bg-input/30 transition-colors">
                            <td className="p-4 font-bold text-foreground max-w-xs truncate" title={q.text}>{q.text}</td>
                            <td className="p-4 text-xs font-semibold text-muted-foreground">{q.category?.name || 'Kategori Yok'}</td>
                            <td className="p-4">
                              <span className={`text-[10px] font-black uppercase tracking-wider px-2 py-0.5 rounded-lg border ${diffColor(q.difficultyLevel)}`}>
                                {diffLabel(q.difficultyLevel)}
                              </span>
                            </td>
                            <td className="p-4 text-xs font-semibold text-muted-foreground">
                              {q.questionType === 1 ? 'Çoktan Seçmeli' : 'Doğru/Yanlış'}
                            </td>
                            <td className="p-4">
                              <span className={`text-[9px] font-black uppercase tracking-widest px-2 py-0.5 rounded-full border ${
                                isUserCreated ? 'bg-primary/10 border-primary/30 text-primary' : 'bg-muted-foreground/10 border-border text-muted-foreground'
                              }`}>
                                {source}
                              </span>
                            </td>
                            <td className="p-4 text-xs font-bold text-muted-foreground">@{createdBy}</td>
                            <td className="p-4 text-right">
                              <button 
                                onClick={() => handleDelete(q.id)} 
                                className="p-2 bg-input rounded-xl text-muted-foreground hover:text-destructive hover:bg-destructive/15 border border-transparent hover:border-destructive/30 transition-all active:scale-90"
                                title="Soruyu Sil"
                              >
                                <Trash2 className="h-4 w-4" />
                              </button>
                            </td>
                          </tr>
                        );
                      })
                    )}
                  </tbody>
                </table>
              </div>

              {/* Table Pagination */}
              {totalPages > 1 && (
                <div className="p-4 border-t border-border flex items-center justify-between bg-input/20">
                  <span className="text-xs text-muted-foreground font-medium">
                    Sayfa <span className="font-bold text-foreground">{page}</span> / <span className="font-bold text-foreground">{totalPages}</span>
                  </span>
                  <div className="flex gap-1.5">
                    <button 
                      onClick={() => setPage(p => Math.max(1, p - 1))} 
                      disabled={page === 1} 
                      className="p-2 rounded-xl bg-card border border-border disabled:opacity-30 hover:border-primary/40 transition-all"
                    >
                      <ChevronLeft className="h-4 w-4" />
                    </button>
                    <button 
                      onClick={() => setPage(p => Math.min(totalPages, p + 1))} 
                      disabled={page === totalPages} 
                      className="p-2 rounded-xl bg-card border border-border disabled:opacity-30 hover:border-primary/40 transition-all"
                    >
                      <ChevronRight className="h-4 w-4" />
                    </button>
                  </div>
                </div>
              )}
            </div>
          </div>
        )}

        {/* Tab CONTENT 3: User Management */}
        {activeTab === 'users' && (
          <div className="space-y-6">
            {/* Search and Management Filter */}
            <div className="bg-card border border-border rounded-2xl p-6 flex flex-col md:flex-row justify-between items-center gap-4">
              <div className="relative w-full md:max-w-md">
                <Search className="absolute left-3.5 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                <input
                  type="text"
                  placeholder="Kullanıcı adı veya e-posta ile arayın..."
                  value={userSearch}
                  onChange={(e) => setUserSearch(e.target.value)}
                  className="w-full bg-input border border-border rounded-xl pl-10 pr-4 py-2.5 text-sm font-bold text-foreground placeholder:text-muted-foreground outline-none focus:border-primary/50 transition-all"
                />
              </div>
              <div className="text-xs text-muted-foreground font-black uppercase bg-input border border-border px-3 py-1.5 rounded-full">
                Sistemdeki Toplam Kullanıcı Listesi
              </div>
            </div>

            {/* Users Grid/Table */}
            <div className="bg-card border border-border rounded-3xl overflow-hidden shadow-lg">
              <div className="overflow-x-auto">
                <table className="w-full text-left border-collapse">
                  <thead>
                    <tr className="bg-input/60 border-b border-border">
                      <th className="p-4 font-black uppercase tracking-widest text-xs text-muted-foreground">Kullanıcı</th>
                      <th className="p-4 font-black uppercase tracking-widest text-xs text-muted-foreground">E-Posta</th>
                      <th className="p-4 font-black uppercase tracking-widest text-xs text-muted-foreground">Rol</th>
                      <th className="p-4 font-black uppercase tracking-widest text-xs text-muted-foreground">Elo</th>
                      <th className="p-4 font-black uppercase tracking-widest text-xs text-muted-foreground">Toplam Oyun</th>
                      <th className="p-4 font-black uppercase tracking-widest text-xs text-muted-foreground">Kayıt Tarihi</th>
                      <th className="p-4 font-black uppercase tracking-widest text-xs text-muted-foreground">Durum</th>
                      <th className="p-4 font-black uppercase tracking-widest text-xs text-muted-foreground text-right">İşlemler</th>
                    </tr>
                  </thead>
                  <tbody>
                    {usersLoading ? (
                      <tr>
                        <td colSpan={8} className="p-16 text-center">
                          <Loader2 className="h-10 w-10 animate-spin text-primary mx-auto" />
                          <span className="text-xs text-muted-foreground font-semibold mt-2 block">Kullanıcılar yükleniyor...</span>
                        </td>
                      </tr>
                    ) : users.length === 0 ? (
                      <tr>
                        <td colSpan={8} className="p-16 text-center text-muted-foreground">
                          Arama kriterlerine uygun kullanıcı bulunamadı.
                        </td>
                      </tr>
                    ) : (
                      users.map(u => {
                        const isBanned = u.status === 'Banned';
                        const isAdmin = u.role === 'Admin';
                        
                        return (
                          <tr key={u.id} className="border-b border-border hover:bg-input/30 transition-colors">
                            <td className="p-4 font-bold text-foreground">@{u.username}</td>
                            <td className="p-4 text-xs font-semibold text-muted-foreground flex items-center gap-1.5">
                              <Mail className="h-3.5 w-3.5 text-muted-foreground/60" /> {u.email}
                            </td>
                            <td className="p-4">
                              <span className={`text-[10px] font-black uppercase tracking-wider px-2.5 py-0.5 rounded-lg border ${
                                isAdmin 
                                  ? 'text-primary bg-primary/10 border-primary/30 shadow-[0_0_8px_rgba(251,191,36,0.05)]' 
                                  : 'text-muted-foreground bg-input border-border'
                              }`}>
                                {u.role}
                              </span>
                            </td>
                            <td className="p-4 text-xs font-black text-foreground flex items-center gap-1">
                              <Trophy className="h-3.5 w-3.5 text-yellow-500" /> {u.elo}
                            </td>
                            <td className="p-4 text-xs font-semibold text-muted-foreground">
                              {u.totalGames} Maç
                            </td>
                            <td className="p-4 text-xs font-semibold text-muted-foreground">
                              {new Date(u.joinedDate).toLocaleDateString()}
                            </td>
                            <td className="p-4">
                              <span className={`h-2.5 w-2.5 rounded-full inline-block mr-1.5 ${
                                isBanned ? 'bg-destructive shadow-[0_0_8px_#ea4335]' : 'bg-[#3fb950] shadow-[0_0_8px_#3fb950]'
                              }`} />
                              <span className={`text-xs font-bold uppercase tracking-wider ${
                                isBanned ? 'text-destructive' : 'text-[#3fb950]'
                              }`}>
                                {u.status}
                              </span>
                            </td>
                            <td className="p-4">
                              <div className="flex justify-end gap-2">
                                {/* Ban / Unban Toggle Button */}
                                {isBanned ? (
                                  <button
                                    onClick={() => handleUnban(u.id)}
                                    className="px-3 py-1.5 bg-[#3fb950]/15 hover:bg-[#3fb950] hover:text-white border border-[#3fb950]/30 text-[#3fb950] text-[10px] font-black uppercase tracking-wider rounded-xl transition-all active:scale-95"
                                  >
                                    Banı Kaldır
                                  </button>
                                ) : (
                                  <button
                                    onClick={() => handleBan(u.id)}
                                    className="px-3 py-1.5 bg-destructive/15 hover:bg-destructive hover:text-white border border-destructive/30 text-destructive text-[10px] font-black uppercase tracking-wider rounded-xl transition-all active:scale-95"
                                  >
                                    Yasakla
                                  </button>
                                )}

                                {/* Make Admin / Remove Admin Button */}
                                {isAdmin ? (
                                  <button
                                    onClick={() => handleRemoveAdmin(u.id)}
                                    className="px-3 py-1.5 bg-destructive/10 hover:bg-destructive hover:text-white border border-destructive/20 text-destructive text-[10px] font-black uppercase tracking-wider rounded-xl transition-all active:scale-95"
                                  >
                                    Adminliği Kaldır
                                  </button>
                                ) : (
                                  <button
                                    onClick={() => handleMakeAdmin(u.id)}
                                    className="px-3 py-1.5 bg-primary/10 hover:bg-primary hover:text-black border border-primary/20 text-primary text-[10px] font-black uppercase tracking-wider rounded-xl transition-all active:scale-95"
                                  >
                                    Admin Yap
                                  </button>
                                )}
                              </div>
                            </td>
                          </tr>
                        );
                      })
                    )}
                  </tbody>
                </table>
              </div>

              {/* User Pagination */}
              {userTotalPages > 1 && (
                <div className="p-4 border-t border-border flex items-center justify-between bg-input/20">
                  <span className="text-xs text-muted-foreground font-medium">
                    Sayfa <span className="font-bold text-foreground">{userPage}</span> / <span className="font-bold text-foreground">{userTotalPages}</span>
                  </span>
                  <div className="flex gap-1.5">
                    <button 
                      onClick={() => setUserPage(p => Math.max(1, p - 1))} 
                      disabled={userPage === 1} 
                      className="p-2 rounded-xl bg-card border border-border disabled:opacity-30 hover:border-primary/40 transition-all"
                    >
                      <ChevronLeft className="h-4 w-4" />
                    </button>
                    <button 
                      onClick={() => setUserPage(p => Math.min(userTotalPages, p + 1))} 
                      disabled={userPage === userTotalPages} 
                      className="p-2 rounded-xl bg-card border border-border disabled:opacity-30 hover:border-primary/40 transition-all"
                    >
                      <ChevronRight className="h-4 w-4" />
                    </button>
                  </div>
                </div>
              )}
            </div>
          </div>
        )}
      </main>
    </div>
  );
}
