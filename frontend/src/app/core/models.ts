export enum EventType {
  Wedding = 0,
  BarMitzvah = 1,
  BatMitzvah = 2,
  Birthday = 3,
  Corporate = 4,
  Other = 5
}

export enum MagnetSize {
  Small = 0,
  Medium = 1,
  Large = 2
}

export enum OrderStatus {
  New = 0,
  InProgress = 1,
  Ready = 2,
  Cancelled = 3
}

export interface EventResponse {
  id: string;
  slug: string;
  name: string;
  hostNames: string;
  eventType: EventType;
  date: string;
  sizeSmallAvailable: boolean;
  sizeMediumAvailable: boolean;
  sizeLargeAvailable: boolean;
  maxCopies: number;
  averagePrepTimeMinutes: number;
  isActive: boolean;
  ordersOpen: boolean;
  ordersPaused: boolean;
  isEnded: boolean;
  guestUrl: string;
  qrCodeBase64?: string;
}

export interface GuestEventResponse {
  name: string;
  hostNames: string;
  eventType: EventType;
  sizeSmallAvailable: boolean;
  sizeMediumAvailable: boolean;
  sizeLargeAvailable: boolean;
  maxCopies: number;
  ordersOpen: boolean;
  ordersPaused: boolean;
  isEnded: boolean;
}

export interface OrderResponse {
  id: string;
  eventId: string;
  orderNumber: number;
  customerName: string;
  phone: string;
  imageUrl?: string;
  magnetSize: MagnetSize;
  quantity: number;
  status: OrderStatus;
  createdAt: string;
  positionInQueue?: number;
  estimatedWaitMinutes?: number;
  waitMinutes: number;
}

export interface PublicOrderView {
  publicToken: string;
  eventId: string;
  eventSlug: string;
  eventName: string;
  hostNames: string;
  eventType: EventType;
  orderNumber: number;
  customerName: string;
  magnetSize: MagnetSize;
  quantity: number;
  status: OrderStatus;
  createdAt: string;
  positionInQueue?: number;
  estimatedWaitMinutes?: number;
  canEdit: boolean;
  canCancel: boolean;
  minutesAgo: number;
  eventEnded: boolean;
  ordersPaused: boolean;
}

export interface OrderTokenSummary {
  publicToken: string;
  orderNumber: number;
  status: OrderStatus;
  magnetSize: MagnetSize;
  quantity: number;
  createdAt: string;
  minutesAgo: number;
}

export interface DashboardStats {
  printedCount: number;
  pendingCount: number;
  cancelledCount: number;
  averageWaitMinutes: number;
  currentLoad: string;
}

export interface EventSummary {
  totalOrders: number;
  totalMagnets: number;
  printedCount: number;
  cancelledCount: number;
  pendingCount: number;
  averageWaitMinutes: number;
  peakHours: { hour: string; orderCount: number }[];
}

export const EVENT_TYPE_LABELS: Record<EventType, string> = {
  [EventType.Wedding]: 'חתונה',
  [EventType.BarMitzvah]: 'בר מצווה',
  [EventType.BatMitzvah]: 'בת מצווה',
  [EventType.Birthday]: 'יום הולדת',
  [EventType.Corporate]: 'אירוע עסקי',
  [EventType.Other]: 'אחר'
};

export const SIZE_LABELS: Record<MagnetSize, string> = {
  [MagnetSize.Small]: 'קטן',
  [MagnetSize.Medium]: 'בינוני',
  [MagnetSize.Large]: 'גדול'
};

export const STATUS_LABELS: Record<OrderStatus, string> = {
  [OrderStatus.New]: 'חדש',
  [OrderStatus.InProgress]: 'בטיפול',
  [OrderStatus.Ready]: 'מוכן',
  [OrderStatus.Cancelled]: 'בוטל'
};
