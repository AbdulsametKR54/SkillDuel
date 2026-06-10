'use client';

import { useState, useEffect } from 'react';
import { adminApi } from '@/lib/api';
import { toast } from 'sonner';

interface Report {
  id: string;
  reporterId: string;
  reporterUsername: string;
  reportedUserId: string;
  reportedUsername: string;
  reason: string;
  chatMessage: string | null;
  createdAt: string;
}

export default function BansAdminPage() {
  const [reports, setReports] = useState<Report[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetchReports();
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
        toast.success('Rapor çözüldü olarak işaretlendi');
        fetchReports(); // Refresh the list
      }
    } catch (error) {
      toast.error('İşlem başarısız');
    }
  };

  if (loading) {
    return (
      <div className="flex justify-center items-center h-64">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-indigo-600"></div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex justify-between items-center">
        <h1 className="text-2xl font-bold text-white">Raporlar ve Ban Yönetimi</h1>
      </div>

      <div className="bg-slate-800 rounded-xl border border-slate-700 overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-left border-collapse">
            <thead>
              <tr className="bg-slate-700/50">
                <th className="p-4 font-medium text-slate-300">Tarih</th>
                <th className="p-4 font-medium text-slate-300">Raporlayan</th>
                <th className="p-4 font-medium text-slate-300">Şikayet Edilen</th>
                <th className="p-4 font-medium text-slate-300">Sebep / Mesaj</th>
                <th className="p-4 font-medium text-slate-300 text-right">Aksiyonlar</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-700/50">
              {reports.length === 0 ? (
                <tr>
                  <td colSpan={5} className="p-8 text-center text-slate-400">
                    Henüz bekleyen bir rapor bulunmuyor.
                  </td>
                </tr>
              ) : (
                reports.map((report) => (
                  <tr key={report.id} className="hover:bg-slate-700/30 transition-colors">
                    <td className="p-4 text-slate-300 whitespace-nowrap">
                      {new Date(report.createdAt).toLocaleString('tr-TR')}
                    </td>
                    <td className="p-4 text-slate-300">
                      {report.reporterUsername}
                    </td>
                    <td className="p-4 font-medium text-red-400">
                      {report.reportedUsername}
                    </td>
                    <td className="p-4">
                      <div className="text-slate-200 font-medium">{report.reason}</div>
                      {report.chatMessage && (
                        <div className="text-sm text-slate-400 bg-slate-900/50 p-2 rounded mt-1 italic">
                          &quot;{report.chatMessage}&quot;
                        </div>
                      )}
                    </td>
                    <td className="p-4 text-right">
                      <div className="flex justify-end gap-2 items-center">
                        <select
                          className="bg-slate-700 border border-slate-600 text-sm rounded px-2 py-1 text-slate-200 outline-none"
                          onChange={(e) => {
                            if (e.target.value) {
                              if (confirm(`${report.reportedUsername} kullanıcısını banlamak istediğinize emin misiniz?`)) {
                                handleBan(report.reportedUserId, e.target.value);
                              }
                              e.target.value = ''; // reset after action
                            }
                          }}
                          defaultValue=""
                        >
                          <option value="" disabled>Ban Seçenekleri</option>
                          <option value="1h">1 Saat</option>
                          <option value="1d">1 Gün</option>
                          <option value="1m">1 Ay</option>
                          <option value="perm">Kalıcı Ban</option>
                        </select>

                        <button
                          onClick={() => handleResolve(report.id)}
                          className="px-3 py-1 bg-green-500/10 text-green-400 hover:bg-green-500/20 border border-green-500/20 rounded-lg text-sm transition-colors"
                        >
                          İptal / Çözüldü
                        </button>
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
  );
}
