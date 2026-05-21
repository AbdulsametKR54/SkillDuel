import { NextResponse } from 'next/server';
import type { NextRequest } from 'next/server';

export function middleware(request: NextRequest) {
  const token = request.cookies.get('token')?.value;
  const { pathname } = request.nextUrl;

  // Korunan rotalar listesi
  const protectedRoutes = ['/lobby', '/duel', '/history', '/leaderboard', '/admin'];
  
  // Mevcut yolun korunan bir rota olup olmadığını kontrol et
  const isProtectedRoute = protectedRoutes.some(route => 
    pathname === route || pathname.startsWith(`${route}/`)
  );

  // 1. Durum: Token yok ve korunan bir rotaya erişilmeye çalışılıyor
  if (!token && isProtectedRoute) {
    const loginUrl = new URL('/login', request.url);
    // Giriş sonrası geri dönmek için orijinal URL'i kaydedebiliriz (isteğe bağlı)
    // loginUrl.searchParams.set('callbackUrl', pathname);
    return NextResponse.redirect(loginUrl);
  }

  // 2. Durum: Token var ve giriş/kayıt sayfalarına erişilmeye çalışılıyor
  const isAuthPage = pathname === '/login' || pathname === '/register';
  if (token && isAuthPage) {
    return NextResponse.redirect(new URL('/lobby', request.url));
  }

  return NextResponse.next();
}

// Middleware'in hangi yollarda çalışacağını belirle
export const config = {
  matcher: [
    '/lobby/:path*',
    '/duel/:path*',
    '/history/:path*',
    '/leaderboard/:path*',
    '/admin/:path*',
    '/login',
    '/register',
  ],
};
