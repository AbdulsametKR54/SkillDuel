import axios from 'axios';
import Cookies from 'js-cookie';

const API_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';

const api = axios.create({
  baseURL: API_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Auth interceptor
api.interceptors.request.use((config) => {
  const token = Cookies.get('token');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Refresh token interceptor
api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;
    
    // Prevent infinite loop if the refresh endpoint itself returns 401
    if (error.response?.status === 401 && originalRequest.url !== '/api/Auth/refresh' && !originalRequest._retry) {
      originalRequest._retry = true;
      
      const token = Cookies.get('token');
      const refreshToken = Cookies.get('refreshToken');
      
      if (token && refreshToken) {
        try {
          const response = await axios.post(`${API_URL}/api/Auth/refresh`, {
            accessToken: token,
            refreshToken: refreshToken
          });
          
          if (response.data?.success) {
            const newToken = response.data.data.token;
            const newRefreshToken = response.data.data.refreshToken;
            
            Cookies.set('token', newToken);
            Cookies.set('refreshToken', newRefreshToken);
            
            originalRequest.headers.Authorization = `Bearer ${newToken}`;
            return api(originalRequest);
          }
        } catch (refreshError) {
          // If refresh fails, fall through to logout
          console.error("Refresh token failed", refreshError);
        }
      }
      
      // Token expired/invalid and refresh failed or no refresh token available
      Cookies.remove('token');
      Cookies.remove('refreshToken');
      if (typeof window !== 'undefined') {
        window.location.href = '/login';
      }
    }
    
    return Promise.reject(error);
  }
);

export const authApi = {
  register: (data: any) => api.post('/api/Auth/register', data).then(r => r.data),
  login: (data: any) => api.post('/api/Auth/login', data).then(r => r.data),
  refresh: (data: any) => api.post('/api/Auth/refresh', data).then(r => r.data),
  logout: () => api.post('/api/Auth/logout').then(r => r.data),
};

export const questionsApi = {
  list: (params?: any) => api.get('/api/Questions', { params }).then(r => r.data),
  create: (data: any) => api.post('/api/Questions', data).then(r => r.data),
  update: (id: string, data: any) => api.put(`/api/Questions/${id}`, data).then(r => r.data),
  delete: (id: string) => api.delete(`/api/Questions/${id}`).then(r => r.data),
  suggest: (data: any) => api.post('/api/Questions/suggest', data).then(r => r.data),
};

export const adminApi = {
  getPendingQuestions: () => api.get('/api/admin/questions/pending').then(r => r.data),
  approveQuestion: (id: string) => api.put(`/api/admin/questions/${id}/approve`).then(r => r.data),
  rejectQuestion: (id: string) => api.put(`/api/admin/questions/${id}/reject`).then(r => r.data),
  getUsers: (page = 1, search = '') => api.get('/api/admin/users', { params: { page, search } }).then(r => r.data),
  banUser: (id: string) => api.put(`/api/admin/users/${id}/ban`).then(r => r.data),
  unbanUser: (id: string) => api.put(`/api/admin/users/${id}/unban`).then(r => r.data),
  updateUserRole: (id: string, role: string) => api.put(`/api/admin/users/${id}/role`, { role }).then(r => r.data),
};

export const categoriesApi = {
  list: () => api.get('/api/Categories').then(r => r.data),
  create: (data: any) => api.post('/api/Categories', data).then(r => r.data),
  delete: (id: string) => api.delete(`/api/Categories/${id}`).then(r => r.data),
};

export const gameApi = {
  getSession: (id: string) => api.get(`/api/Game/session/${id}`).then(r => r.data),
  getHistory: (page = 1, pageSize = 10) => api.get('/api/Games/history', { params: { page, pageSize } }).then(r => r.data),
};

export const roomsApi = {
  list: (params?: { page?: number; pageSize?: number; searchName?: string; categoryId?: string; roundCount?: number }) => api.get('/api/Rooms', { params }).then(r => r.data),
  get: (code: string) => api.get(`/api/Rooms/${code}`).then(r => r.data),
  create: (data: any) => api.post('/api/Rooms', data).then(r => r.data),
  join: (code: string, data: any) => api.post(`/api/Rooms/${code}/join`, data).then(r => r.data),
  delete: (code: string) => api.delete(`/api/Rooms/${code}`).then(r => r.data),
  leave: (code: string) => api.post(`/api/Rooms/${code}/leave`).then(r => r.data),
  kick: (code: string, userId: string) => api.post(`/api/Rooms/${code}/kick/${userId}`).then(r => r.data),
  updateSettings: (code: string, data: any) => api.put(`/api/Rooms/${code}/settings`, data).then(r => r.data),
  delegateAdmin: (code: string, userId: string) => api.post(`/api/Rooms/${code}/delegate-admin/${userId}`).then(r => r.data),
};


export const leaderboardApi = {
  getLeaderboard: (limit = 50) => api.get('/api/Leaderboard', { params: { limit } }).then(r => r.data),
};

export const usersApi = {
  me: () => api.get('/api/Users/me').then(r => r.data),
  updateMe: (data: { newUsername: string }) => api.put('/api/Users/me', data).then(r => r.data),
  stats: () => api.get('/api/Users/me/stats').then(r => r.data),
};

export const friendsApi = {
  list: () => api.get('/api/Friends').then(r => r.data),
  requests: () => api.get('/api/Friends/requests').then(r => r.data),
  sendRequest: (username: string) => api.post('/api/Friends/request', { username }).then(r => r.data),
  acceptRequest: (id: string) => api.post(`/api/Friends/request/${id}/accept`).then(r => r.data),
  declineRequest: (id: string) => api.post(`/api/Friends/request/${id}/decline`).then(r => r.data),
  removeFriend: (id: string) => api.delete(`/api/Friends/${id}`).then(r => r.data),
};

export default api;
