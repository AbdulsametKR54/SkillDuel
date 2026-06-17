'use client';

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { adminApi, usersApi } from '@/lib/api';
import { toast } from 'sonner';
import { Navbar } from '@/components/Navbar';

interface Report {
  id: string;
  reporterId: string;
  reporterUsername: string;
  reportedUserId: string;
  reportedUsername: string;
  reason: string;
  chatMessage: string | null;
  createdAt: string;
  isResolved: boolean;
}

export default function BansAdminPage() {
  const router = useRouter();
  const [reports, setReports] = useState<Report[]>([]);
  const [loading, setLoading] = useState(true);
  const [activeTab, setActiveTab] = useState<'pending' | 'resolved'>('pending');

  const [modalState, setModalState] = useState<{ type: 'ban' | 'reject' | 'undo', report: Report | null }>({ type: 'ban', report: null });
  const [banDuration, setBanDuration] = useState('1h');

  useEffect(() => {
    const init = async () => {
      try {
        const u = await usersApi.me();
        if (u.data.role !== 'Admin') {
          router.push('/lobby');
          return;
        }
        fetchReports();
      } catch {
        router.push('/lobby');
      }
    };
    init();
  }, []);

  const fetchReports = async () => {
    try {
      setLoading(true);
      const res = await adminApi.getReports();
      if (res.success) {
        setReports(res.data);
      }
    } catch (error) {
      toast.error('Raporlar yüklenirken bir hata oluştu');
    } finally {
      setLoading(false);
    }
  };

  const handleBan = async (userId: string, duration: string) => {
    try {
      const res = await adminApi.banUserWithDuration({ userId, duration });
      if (res.success) {
        toast.success('Kullanıcı başarıyla banlandı');
        fetchReports(); // Refresh the list
      }
    } catch (error) {
      toast.error('Ban işlemi başarısız');
    }
  };

  const handleResolve = async (id: string) => {
    try {
      const res = await adminApi.resolveReport(id);
      if (res.success) {
        toast.success('Rapor reddedildi ve kapatıldı');
        fetchReports(); // Refresh the list
      }
    } catch (error) {
      toast.error('İşlem başarısız');
    }
  };

  const handleUndo = async (id: string) => {
    try {
      const res = await adminApi.undoReport(id);
      if (res.success) {
        toast.success('Karar iptal edildi ve ban kaldırıldı');
        fetchReports(); // Refresh the list
      }
    } catch (error) {
      toast.error('İşlem başarısız');
    }
  };

  if (loading) {
    return (
      <div className="flex justify-center items-center h-[60vh]">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary"></div>
      </div>
    );
  }

  return (
    <div>
      <Navbar />
      <main className="flex-1 w-full max-w-7xl mx-auto px-4 sm:px-6 py-8">
        <div className="space-y-6 animate-in fade-in duration-300">
      <div className="flex justify-between items-center">
        <h1 className="text-3xl font-black tracking-tight text-foreground">Raporlar ve Ban Yönetimi</h1>
        <div className="flex gap-2">
          <button 
            onClick={() => setActiveTab('pending')}
            className={`px-4 py-2 rounded-lg font-bold text-sm transition-all ${activeTab === 'pending' ? 'bg-primary text-white' : 'bg-input text-muted-foreground hover:text-foreground hover:bg-input/80'}`}
          >
            Bekleyenler
          </button>
          <button 
            onClick={() => setActiveTab('resolved')}
            className={`px-4 py-2 rounded-lg font-bold text-sm transition-all ${activeTab === 'resolved' ? 'bg-primary text-white' : 'bg-input text-muted-foreground hover:text-foreground hover:bg-input/80'}`}
          >
            Geçmiş
          </button>
        </div>
      </div>

      <div className="bg-card rounded-2xl border border-border shadow-lg overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-left border-collapse">
            <thead>
              <tr className="bg-input/50 border-b border-border">
                <th className="p-4 font-bold text-xs uppercase tracking-widest text-muted-foreground">Tarih</th>
                <th className="p-4 font-bold text-xs uppercase tracking-widest text-muted-foreground">Raporlayan</th>
                <th className="p-4 font-bold text-xs uppercase tracking-widest text-muted-foreground">Şikayet Edilen</th>
                <th className="p-4 font-bold text-xs uppercase tracking-widest text-muted-foreground">Sebep / Mesaj</th>
                <th className="p-4 font-bold text-xs uppercase tracking-widest text-muted-foreground text-right">Aksiyonlar</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border">
              {reports.filter(r => (activeTab === 'pending' ? !r.isResolved : r.isResolved)).length === 0 ? (
                <tr>
                  <td colSpan={5} className="p-12 text-center">
                    <p className="text-muted-foreground font-medium">Henüz bu kategoride bir rapor bulunmuyor.</p>
                  </td>
                </tr>
              ) : (
                reports.filter(r => (activeTab === 'pending' ? !r.isResolved : r.isResolved)).map((report) => (
                  <tr key={report.id} className="hover:bg-input/50 transition-colors group">
                    <td className="p-4 text-sm font-medium text-muted-foreground whitespace-nowrap">
                      {new Date(report.createdAt).toLocaleString('tr-TR')}
                    </td>
                    <td className="p-4 text-sm font-bold text-foreground">
                      {report.reporterUsername}
                    </td>
                    <td className="p-4">
                      <div className="flex items-center gap-2">
                        <span className="font-bold text-red-500">{report.reportedUsername}</span>
                      </div>
                    </td>
                    <td className="p-4">
                      <div className="space-y-1">
                        <p className="text-sm font-bold text-foreground">{report.reason}</p>
                        {report.chatMessage && (
                          <div className="bg-input/50 p-2 rounded-lg border border-border/50">
                            <span className="text-xs text-muted-foreground mr-2">Mesaj:</span>
                            <span className="text-sm font-mono text-foreground">&quot;{report.chatMessage}&quot;</span>
                          </div>
                        )}
                      </div>
                    </td>
                    <td className="p-4 text-right">
                      <div className="flex items-center justify-end gap-2">
                        {!report.isResolved ? (
                          <>
                            <button
                              onClick={() => setModalState({ type: 'ban', report })}
                              className="px-3 py-1.5 bg-red-500 hover:bg-red-600 text-white text-xs font-bold rounded-lg transition-all"
                            >
                              Süreli Banla
                            </button>
                            <button
                              onClick={() => setModalState({ type: 'reject', report })}
                              className="px-3 py-1.5 bg-input hover:bg-input/80 text-foreground text-xs font-bold rounded-lg transition-all"
                            >
                              Reddet
                            </button>
                          </>
                        ) : (
                          <button
                            onClick={() => setModalState({ type: 'undo', report })}
                            className="px-3 py-1.5 bg-blue-500 hover:bg-blue-600 text-white text-xs font-bold rounded-lg transition-all"
                          >
                            Kararı İptal Et
                          </button>
                        )}
                      </div>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>
      </div>
      </main>

      {/* Action Modals */}
      {modalState.report && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-background/80 backdrop-blur-sm animate-in fade-in duration-200">
          <div className="bg-card border border-border shadow-2xl rounded-2xl p-6 w-full max-w-md animate-in zoom-in-95 duration-200">
            {modalState.type === 'ban' && (
              <>
                <h3 className="text-xl font-black mb-2 text-red-500">Kullanıcıyı Banla</h3>
                <p className="text-sm text-muted-foreground mb-4">
                  <strong className="text-foreground">{modalState.report.reportedUsername}</strong> isimli kullanıcıyı banlamak üzeresiniz. Lütfen süreyi seçin.
                </p>
                <div className="space-y-4 mb-6">
                  <div>
                    <label className="text-xs font-bold uppercase tracking-widest text-muted-foreground block mb-2">Ban Süresi</label>
                    <select
                      value={banDuration}
                      onChange={(e) => setBanDuration(e.target.value)}
                      className="w-full h-12 bg-input border border-border rounded-xl px-4 text-sm font-bold text-foreground outline-none focus:border-red-500 transition-colors"
                    >
                      <option value="1h">1 Saat</option>
                      <option value="1d">1 Gün</option>
                      <option value="7d">1 Hafta</option>
                      <option value="1m">1 Ay</option>
                      <option value="perm">Kalıcı (Sınırsız)</option>
                    </select>
                  </div>
                </div>
                <div className="flex gap-3">
                  <button onClick={() => setModalState({ type: 'ban', report: null })} className="flex-1 py-3 rounded-xl bg-input font-bold hover:bg-input/80 transition-all">İptal</button>
                  <button onClick={() => { handleBan(modalState.report!.reportedUserId, banDuration); setModalState({ type: 'ban', report: null }); }} className="flex-1 py-3 rounded-xl bg-red-500 text-white font-bold hover:bg-red-600 transition-all">Banla</button>
                </div>
              </>
            )}

            {modalState.type === 'reject' && (
              <>
                <h3 className="text-xl font-black mb-2">Raporu Reddet</h3>
                <p className="text-sm text-muted-foreground mb-6">
                  Bu raporu reddedip kapatmak istediğinize emin misiniz? Şikayet edilen kullanıcıya herhangi bir ceza uygulanmayacaktır.
                </p>
                <div className="flex gap-3">
                  <button onClick={() => setModalState({ type: 'reject', report: null })} className="flex-1 py-3 rounded-xl bg-input font-bold hover:bg-input/80 transition-all">İptal</button>
                  <button onClick={() => { handleResolve(modalState.report!.id); setModalState({ type: 'reject', report: null }); }} className="flex-1 py-3 rounded-xl bg-foreground text-background font-bold hover:opacity-90 transition-all">Evet, Reddet</button>
                </div>
              </>
            )}

            {modalState.type === 'undo' && (
              <>
                <h3 className="text-xl font-black mb-2 text-blue-500">Kararı İptal Et</h3>
                <p className="text-sm text-muted-foreground mb-6">
                  Verilen kararı iptal edip şikayeti tekrar incelemeye almak istediğinize emin misiniz? Eğer kullanıcıya bir ban atıldıysa, <strong>bu ban derhal kaldırılacaktır.</strong>
                </p>
                <div className="flex gap-3">
                  <button onClick={() => setModalState({ type: 'undo', report: null })} className="flex-1 py-3 rounded-xl bg-input font-bold hover:bg-input/80 transition-all">İptal</button>
                  <button onClick={() => { handleUndo(modalState.report!.id); setModalState({ type: 'undo', report: null }); }} className="flex-1 py-3 rounded-xl bg-blue-500 text-white font-bold hover:bg-blue-600 transition-all">Kararı İptal Et</button>
                </div>
              </>
            )}
          </div>
        </div>
      )}
    </div>
  );
}
