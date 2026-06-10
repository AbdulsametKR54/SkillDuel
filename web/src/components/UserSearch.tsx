import React, { useState, useEffect, useRef } from 'react';
import { usersApi, friendsApi, reportsApi } from '@/lib/api';
import { Search, UserPlus, Flag, Loader2 } from 'lucide-react';
import { toast } from 'sonner';

export function UserSearch() {
  const [query, setQuery] = useState('');
  const [results, setResults] = useState<any[]>([]);
  const [loading, setLoading] = useState(false);
  const [isOpen, setIsOpen] = useState(false);
  const wrapperRef = useRef<HTMLDivElement>(null);

  // Report modal state
  const [reportTarget, setReportTarget] = useState<{ id: string, username: string } | null>(null);
  const [reportReason, setReportReason] = useState('');

  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (wrapperRef.current && !wrapperRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    }
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  useEffect(() => {
    const timer = setTimeout(async () => {
      if (query.length >= 3) {
        setLoading(true);
        try {
          const res = await usersApi.search(query);
          setResults(res.items || []);
          setIsOpen(true);
        } catch (e) {
          console.error("Search failed", e);
        } finally {
          setLoading(false);
        }
      } else {
        setResults([]);
        setIsOpen(false);
      }
    }, 500);
    return () => clearTimeout(timer);
  }, [query]);

  const handleAddFriend = async (username: string) => {
    try {
      await friendsApi.sendRequest(username);
      toast.success("Arkadaşlık isteği gönderildi!");
      setIsOpen(false);
    } catch (e: any) {
      toast.error(e.response?.data?.message || "İstek gönderilemedi");
    }
  };

  const handleReport = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!reportTarget || !reportReason) return;
    try {
      await reportsApi.create({
        reportedUserId: reportTarget.id,
        reason: reportReason,
      });
      toast.success("Şikayet gönderildi.");
      setReportTarget(null);
      setIsOpen(false);
    } catch (e: any) {
      toast.error("Şikayet gönderilemedi.");
    }
  };

  return (
    <div className="relative z-50 flex items-center" ref={wrapperRef}>
      <div className="relative flex items-center">
        <Search className="absolute left-3 h-4 w-4 text-muted-foreground pointer-events-none" />
        <input
          type="text"
          placeholder="Kullanıcı ara..."
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          onFocus={() => { if (results.length > 0) setIsOpen(true); }}
          className="w-64 h-9 pl-9 pr-4 rounded-xl bg-input border border-border text-sm outline-none transition-all focus:border-primary focus:ring-1 focus:ring-primary/20"
        />
        {loading && <Loader2 className="absolute right-3 h-4 w-4 animate-spin text-muted-foreground" />}
      </div>

      {isOpen && results.length > 0 && (
        <div className="absolute top-full mt-2 w-72 bg-card border border-border rounded-xl shadow-xl overflow-hidden">
          <div className="max-h-[300px] overflow-y-auto">
            {results.map(user => (
              <div key={user.id} className="flex items-center justify-between p-3 border-b border-border hover:bg-input transition-colors last:border-0">
                <div className="flex flex-col">
                  <span className="font-bold text-sm text-foreground">{user.username}</span>
                  <span className="text-[10px] font-bold text-muted-foreground uppercase tracking-widest">{user.eloRating} Elo</span>
                </div>
                <div className="flex items-center gap-1">
                  <button 
                    onClick={() => handleAddFriend(user.username)}
                    className="p-1.5 text-primary hover:bg-primary/10 rounded-lg transition-colors"
                    title="Arkadaş Ekle"
                  >
                    <UserPlus className="h-4 w-4" />
                  </button>
                  <button 
                    onClick={() => setReportTarget({ id: user.id, username: user.username })}
                    className="p-1.5 text-muted-foreground hover:text-red-500 hover:bg-red-500/10 rounded-lg transition-colors"
                    title="Şikayet Et"
                  >
                    <Flag className="h-4 w-4" />
                  </button>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Report Modal */}
      {reportTarget && (
        <div className="fixed inset-0 z-[100] flex items-center justify-center bg-background/80 backdrop-blur-sm animate-in fade-in duration-200">
          <div className="bg-card border border-border rounded-3xl p-8 max-w-md w-full shadow-2xl relative">
            <h2 className="text-2xl font-black mb-6 tracking-tight flex items-center gap-2 text-foreground">
              <Flag className="h-6 w-6 text-red-500" /> Şikayet Et
            </h2>
            <form onSubmit={handleReport} className="space-y-6">
              <div className="space-y-2">
                <label className="text-xs font-bold uppercase tracking-widest text-muted-foreground">Şikayet Edilen</label>
                <div className="w-full h-10 bg-input/50 border border-border rounded-xl px-4 text-sm font-bold text-foreground flex items-center">
                  {reportTarget.username}
                </div>
              </div>
              <div className="space-y-2">
                <label className="text-xs font-bold uppercase tracking-widest text-muted-foreground">Sebep</label>
                <textarea 
                  value={reportReason} 
                  onChange={e => setReportReason(e.target.value)}
                  placeholder="Lütfen şikayet sebebini açıklayın..."
                  className="w-full h-24 bg-input border border-border rounded-xl p-4 text-sm font-bold text-foreground outline-none focus:border-red-500/50 transition-colors resize-none"
                  required
                />
              </div>
              <div className="flex gap-3">
                <button 
                  type="button" 
                  onClick={() => setReportTarget(null)}
                  className="flex-1 h-12 rounded-xl border border-border text-muted-foreground font-bold hover:bg-input transition-all"
                >
                  İptal
                </button>
                <button 
                  type="submit"
                  className="flex-1 h-12 rounded-xl bg-red-500 text-white font-bold hover:bg-red-600 transition-all shadow-lg shadow-red-500/20"
                >
                  Gönder
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
