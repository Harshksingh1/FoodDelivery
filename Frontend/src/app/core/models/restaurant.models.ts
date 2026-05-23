export interface Restaurant { id: string; name: string; address: string; city: string; cuisineType: string; imageUrl?: string; rating: number; isOpen: boolean; prepTimeMinutes: number; minOrderAmount: number; }
export interface MenuItem { id: string; restaurantId: string; restaurantName: string; name: string; description: string; category: string; price: number; imageUrl?: string; isVeg: boolean; isAvailable: boolean; rating: number; }
export interface Cart { id: string; customerId: string; restaurantId: string; restaurantName: string; updatedAt: string; items: CartItem[]; }
export interface CartItem { id: string; cartId: string; menuItemId: string; name: string; unitPrice: number; quantity: number; }
export interface Order { id: string; customerId: string; restaurantId: string; restaurantName: string; customerName: string; customerMobile: string; status: string; deliveryAddress: string; subTotal: number; discountAmount: number; deliveryFee: number; gstAmount: number; totalAmount: number; paymentMethod: string; createdAt: string; estimatedDeliveryAt?: string; deliveryAgentId?: string; items: OrderItem[]; history: StatusHistory[]; }
export interface OrderItem { menuItemId: string; name: string; unitPrice: number; quantity: number; totalPrice: number; }
export interface StatusHistory { status: string; note?: string; changedAt: string; changedBy: string; }
