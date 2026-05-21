"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import Cookies from "js-cookie";
import { Loader2, Swords } from "lucide-react";
import { authApi } from "@/lib/api";

export default function RegisterPage() {
  const router = useRouter();
  const [username, setUsername] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState("");

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsLoading(true);
    setError("");
    try {
      const response = await authApi.register({ username, email, password });
      let authData = response?.data;
      let token = authData?.token;
      let refreshToken = authData?.refreshToken;

      if (!token) {
        try {
          const loginResponse = await authApi.login({ email, password });
          const loginAuthData = loginResponse?.data;
          token = loginAuthData?.token;
          refreshToken = loginAuthData?.refreshToken;
        } catch {
          router.push("/login");
          return;
        }
      }

      if (token) {
        Cookies.set("token", token);
        if (refreshToken) Cookies.set("refreshToken", refreshToken);
        router.push("/lobby");
      } else {
        router.push("/login");
      }
    } catch (err: any) {
      const backendErrors = err.response?.data?.errors;
      if (backendErrors && typeof backendErrors === "object") {
        setError(Object.values(backendErrors).flat().join(", "));
      } else {
        setError(err.response?.data?.message || "Registration failed. Please try again.");
      }
    } finally {
      setIsLoading(false);
    }
  };

  const inputCls =
    "w-full h-11 px-4 rounded-xl bg-input border border-border text-foreground text-sm outline-none transition-all placeholder:text-muted-foreground/50 focus:border-primary focus:ring-2 focus:ring-primary/20 disabled:opacity-50";
  const labelCls = "text-xs font-bold uppercase tracking-widest text-muted-foreground";

  return (
    <div className="flex min-h-screen items-center justify-center bg-background p-4">
      {/* Ambient glow */}
      <div className="pointer-events-none fixed inset-0 overflow-hidden">
        <div className="absolute -top-40 left-1/2 -translate-x-1/2 w-[600px] h-[600px] rounded-full opacity-10 blur-3xl bg-gradient-accent" />
      </div>

      <div className="relative w-full max-w-md">
        {/* Logo */}
        <div className="flex items-center justify-center gap-2 mb-8">
          <div className="bg-gradient-accent p-2 rounded-xl shadow-lg">
            <Swords className="h-6 w-6 text-white" />
          </div>
          <span className="text-2xl font-black tracking-tighter text-gradient-accent">SKILLDUEL</span>
        </div>

        {/* Card */}
        <div className="bg-card border border-border rounded-2xl shadow-2xl p-8 space-y-6">
          <div className="space-y-1 text-center">
            <h1 className="text-2xl font-bold text-foreground">Create an account</h1>
            <p className="text-sm text-muted-foreground">Join SkillDuel and start competing</p>
          </div>

          <form onSubmit={handleSubmit} className="space-y-4">
            <div className="space-y-1.5">
              <label htmlFor="username" className={labelCls}>Username</label>
              <input
                id="username"
                type="text"
                placeholder="player123"
                value={username}
                onChange={(e) => setUsername(e.target.value)}
                required
                disabled={isLoading}
                className={inputCls}
              />
            </div>

            <div className="space-y-1.5">
              <label htmlFor="email" className={labelCls}>Email</label>
              <input
                id="email"
                type="email"
                placeholder="you@example.com"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                required
                disabled={isLoading}
                className={inputCls}
              />
            </div>

            <div className="space-y-1.5">
              <label htmlFor="password" className={labelCls}>Password</label>
              <input
                id="password"
                type="password"
                placeholder="••••••••"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                required
                disabled={isLoading}
                className={inputCls}
              />
            </div>

            {error && (
              <p className="text-sm font-medium text-destructive bg-destructive/10 border border-destructive/20 rounded-lg px-3 py-2">
                {error}
              </p>
            )}

            <button
              id="register-btn"
              type="submit"
              disabled={isLoading}
              className="w-full h-11 rounded-xl bg-gradient-accent text-white font-bold text-sm shadow-lg hover:opacity-90 active:scale-[0.98] transition-all disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center gap-2"
            >
              {isLoading && <Loader2 className="h-4 w-4 animate-spin" />}
              Create Account
            </button>
          </form>

          <p className="text-sm text-center text-muted-foreground">
            Already have an account?{" "}
            <Link href="/login" className="text-primary hover:underline font-semibold">
              Sign in
            </Link>
          </p>
        </div>
      </div>
    </div>
  );
}
