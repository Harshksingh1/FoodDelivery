import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

export interface Payment { id: string; orderId: string; customerId: string; amount: number; method: string; status: string; transactionId?: string; failureReason?: string; createdAt: string; processedAt?: string; }

@Injectable({ providedIn: 'root' })
export class PaymentService {
  private readonly base = `${environment.apiUrl}/gateway/payments`;

  constructor(private http: HttpClient) {}

  simulate(req: { orderId: string; customerId: string; amount: number; method: string; simulateFailure?: boolean }) {
    return this.http.post<Payment>(`${this.base}/simulate`, req);
  }
  refund(req: { paymentId: string; reason: string }) { return this.http.post<{ message: string }>(`${this.base}/refund`, req); }
  getByOrder(orderId: string) { return this.http.get<Payment>(`${this.base}/order/${orderId}`); }
  getAll() { return this.http.get<Payment[]>(`${this.base}`); }
}
