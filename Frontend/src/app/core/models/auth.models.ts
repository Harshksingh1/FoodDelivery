export interface LoginRequest { email: string; password: string; }
export interface RegisterRequest { fullName: string; email: string; mobile: string; password: string; role: string; }
export interface VerifyOtpRequest { otpSessionToken: string; otp: string; }
export interface AuthResponse { accessToken: string; refreshToken: string; role: string; fullName: string; email: string; userId: string; }
export interface LoginResponse { requiresOtp: boolean; otpSessionToken: string; authData?: AuthResponse; }
export interface User { userId: string; fullName: string; email: string; role: string; }
