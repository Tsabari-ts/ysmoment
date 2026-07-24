import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { SignalrService } from '../../core/signalr.service';
import {
  DashboardStats,
  EventResponse,
  OrderResponse,
  OrderStatus,
  SIZE_LABELS,
  STATUS_LABELS
} from '../../core/models';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit, OnDestroy {
  eventId = '';
  event?: EventResponse;
  orders: OrderResponse[] = [];
  stats?: DashboardStats;
  selectedOrder?: OrderResponse;
  imageBlobUrl: string | null = null;
  imageLoading = false;
  imageLoadError = false;
  searchQuery = '';
  loading = true;
  sizeLabels = SIZE_LABELS;
  statusLabels = STATUS_LABELS;
  OrderStatus = OrderStatus;

  private sub?: Subscription;
  private imageSub?: Subscription;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private api: ApiService,
    private signalr: SignalrService
  ) {}

  ngOnInit(): void {
    this.eventId = this.route.snapshot.paramMap.get('id') || '';
    this.loadAll();
    this.signalr.connect(this.eventId);
    this.sub = this.signalr.queueUpdated$.subscribe((update) => {
      this.orders = update.orders;
      this.stats = update.stats;
      if (this.selectedOrder) {
        const updated = this.orders.find((o) => o.id === this.selectedOrder!.id);
        if (updated) this.selectOrder(updated, false);
      }
    });
  }

  ngOnDestroy(): void {
    this.sub?.unsubscribe();
    this.imageSub?.unsubscribe();
    this.revokeImageUrl();
    this.signalr.disconnect();
  }

  get hasImage(): boolean {
    if (!this.selectedOrder) return false;
    const s = this.normStatus(this.selectedOrder.status);
    return s === OrderStatus.New || s === OrderStatus.InProgress;
  }

  loadAll(): void {
    this.loading = true;
    this.api.getDashboard(this.eventId).subscribe((data) => {
      this.event = data.event;
      this.orders = data.orders;
      this.stats = data.stats;
      if (data.event.isEnded) {
        this.router.navigate(['/admin/events', this.eventId, 'summary']);
      }
      if (!this.selectedOrder && this.orders.length) this.selectOrder(this.orders[0]);
      this.loading = false;
    });
  }

  selectOrder(order: OrderResponse, reloadImage = true): void {
    this.selectedOrder = { ...order, status: this.normStatus(order.status) };
    if (reloadImage) this.loadOrderImage(this.selectedOrder);
  }

  retryImage(): void {
    if (this.selectedOrder) this.loadOrderImage(this.selectedOrder);
  }

  private loadOrderImage(order: OrderResponse): void {
    this.imageSub?.unsubscribe();
    this.revokeImageUrl();
    this.imageLoadError = false;

    if (!this.hasImage) return;

    this.imageLoading = true;
    this.imageSub = this.api.getOrderImage(order.id).subscribe({
      next: (blob) => {
        this.imageBlobUrl = URL.createObjectURL(blob);
        this.imageLoading = false;
        this.imageLoadError = false;
      },
      error: () => {
        this.imageBlobUrl = null;
        this.imageLoading = false;
        this.imageLoadError = true;
      }
    });
  }

  private revokeImageUrl(): void {
    if (this.imageBlobUrl) {
      URL.revokeObjectURL(this.imageBlobUrl);
      this.imageBlobUrl = null;
    }
  }

  private normStatus(status: OrderStatus | string): OrderStatus {
    if (typeof status === 'number') return status;
    const map: Record<string, OrderStatus> = {
      New: OrderStatus.New,
      InProgress: OrderStatus.InProgress,
      Ready: OrderStatus.Ready,
      Cancelled: OrderStatus.Cancelled,
      PendingUpload: OrderStatus.PendingUpload
    };
    return map[status] ?? (Number(status) as OrderStatus);
  }

  downloadImage(): void {
    if (!this.imageBlobUrl || !this.selectedOrder) return;
    const a = document.createElement('a');
    a.href = this.imageBlobUrl;
    a.download = `order-${this.selectedOrder.orderNumber}.jpg`;
    a.click();
  }

  openImage(): void {
    if (!this.imageBlobUrl) return;
    window.open(this.imageBlobUrl, '_blank');
  }

  printImage(): void {
    if (!this.imageBlobUrl) return;
    const win = window.open('', '_blank');
    if (!win) return;
    win.document.write(`
      <html dir="rtl"><head><title>הדפסת הזמנה #${this.selectedOrder?.orderNumber}</title></head>
      <body style="margin:0;display:flex;justify-content:center;align-items:center;min-height:100vh">
        <img src="${this.imageBlobUrl}" style="max-width:100%;max-height:100vh" onload="window.print()" />
      </body></html>`);
    win.document.close();
  }

  search(): void {
    if (!this.searchQuery.trim()) {
      this.api.getOrders(this.eventId, true).subscribe((orders) => (this.orders = orders));
      return;
    }
    this.api.searchOrders(this.eventId, this.searchQuery).subscribe((orders) => (this.orders = orders));
  }

  setStatus(status: OrderStatus): void {
    if (!this.selectedOrder) return;
    this.api.updateOrderStatus(this.selectedOrder.id, status).subscribe((order) => {
      this.selectOrder(order);
      this.loadAll();
    });
  }

  togglePause(): void {
    if (!this.event) return;
    this.api.updateEventSettings(this.eventId, { ordersPaused: !this.event.ordersPaused }).subscribe((evt) => {
      this.event = evt;
    });
  }

  toggleActive(value: boolean): void {
    this.api.updateEventSettings(this.eventId, { isActive: value }).subscribe((evt) => {
      this.event = evt;
    });
  }

  toggleOrdersOpen(value: boolean): void {
    this.api.updateEventSettings(this.eventId, { ordersOpen: value }).subscribe((evt) => {
      this.event = evt;
    });
  }

  toggleMuteNotifications(value: boolean): void {
    this.api.updateEventSettings(this.eventId, { muteCustomerNotifications: value }).subscribe((evt) => {
      this.event = evt;
    });
  }

  downloadQr(): void {
    if (!this.event?.qrCodeBase64) return;
    const a = document.createElement('a');
    a.href = 'data:image/png;base64,' + this.event.qrCodeBase64;
    a.download = `barcode-${this.event.slug}.png`;
    a.click();
  }

  endEvent(): void {
    if (!confirm('לסיים את האירוע? פעולה זו תשלח הודעות תודה לכל הלקוחות.')) return;
    this.api.endEvent(this.eventId).subscribe(() => {
      this.router.navigate(['/admin/events', this.eventId, 'summary']);
    });
  }

  get pendingOrders(): OrderResponse[] {
    return this.orders.filter((o) => {
      const s = this.normStatus(o.status);
      return s === OrderStatus.New || s === OrderStatus.InProgress || s === OrderStatus.PendingUpload;
    });
  }

  canChangeStatus(status: OrderStatus): boolean {
    if (!this.selectedOrder) return false;
    const current = this.normStatus(this.selectedOrder.status);
    if (current === OrderStatus.New)
      return status === OrderStatus.InProgress || status === OrderStatus.Cancelled;
    if (current === OrderStatus.InProgress)
      return status === OrderStatus.Ready;
    return false;
  }
}
