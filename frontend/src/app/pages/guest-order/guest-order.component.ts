import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { interval, Subscription, switchMap } from 'rxjs';
import { ApiService } from '../../core/api.service';
import {
  EVENT_TYPE_LABELS,
  GuestEventResponse,
  MagnetSize,
  OrderResponse,
  OrderStatus,
  SIZE_LABELS,
  STATUS_LABELS
} from '../../core/models';

@Component({
  selector: 'app-guest-order',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './guest-order.component.html',
  styleUrl: './guest-order.component.scss'
})
export class GuestOrderComponent implements OnInit, OnDestroy {
  slug = '';
  event?: GuestEventResponse;
  sizes = SIZE_LABELS;
  eventTypeLabels = EVENT_TYPE_LABELS;

  form = {
    customerName: '',
    phone: '',
    magnetSize: MagnetSize.Medium,
    quantity: 1,
    privacyAccepted: false
  };

  imageFile?: File;
  imagePreview?: string;
  loading = false;
  error = '';
  submitted = false;
  order?: OrderResponse;
  OrderStatus = OrderStatus;
  statusLabels = STATUS_LABELS;
  private pollSub?: Subscription;

  constructor(private route: ActivatedRoute, private api: ApiService) {}

  ngOnInit(): void {
    this.slug = this.route.snapshot.paramMap.get('slug') || '';
    this.api.getGuestEvent(this.slug).subscribe({
      next: (evt) => (this.event = evt),
      error: () => (this.error = 'האירוע לא נמצא')
    });
  }

  ngOnDestroy(): void {
    this.pollSub?.unsubscribe();
  }

  get availableSizes(): { value: MagnetSize; label: string }[] {
    if (!this.event) return [];
    const list: { value: MagnetSize; label: string }[] = [];
    if (this.event.sizeSmallAvailable) list.push({ value: MagnetSize.Small, label: SIZE_LABELS[MagnetSize.Small] });
    if (this.event.sizeMediumAvailable) list.push({ value: MagnetSize.Medium, label: SIZE_LABELS[MagnetSize.Medium] });
    if (this.event.sizeLargeAvailable) list.push({ value: MagnetSize.Large, label: SIZE_LABELS[MagnetSize.Large] });
    return list;
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;
    this.imageFile = file;
    const reader = new FileReader();
    reader.onload = () => (this.imagePreview = reader.result as string);
    reader.readAsDataURL(file);
  }

  submit(): void {
    if (!this.imageFile) {
      this.error = 'יש להעלות תמונה';
      return;
    }
    if (!this.form.privacyAccepted) {
      this.error = 'יש לאשר את מדיניות הפרטיות';
      return;
    }

    const fd = new FormData();
    fd.append('customerName', this.form.customerName);
    fd.append('phone', this.form.phone);
    fd.append('magnetSize', String(this.form.magnetSize));
    fd.append('quantity', String(this.form.quantity));
    fd.append('privacyAccepted', String(this.form.privacyAccepted));
    fd.append('image', this.imageFile);

    this.loading = true;
    this.error = '';
    this.api.createOrder(this.slug, fd).subscribe({
      next: (order) => {
        this.order = order;
        this.submitted = true;
        this.startPolling(order.id);
      },
      error: (err) => {
        this.error = err.error?.message || 'שגיאה בשליחת ההזמנה';
        this.loading = false;
      },
      complete: () => (this.loading = false)
    });
  }

  cancelOrder(): void {
    if (!this.order || !confirm('לבטל את ההזמנה?')) return;
    this.api.cancelOrder(this.order.id).subscribe({
      next: () => {
        this.submitted = false;
        this.order = undefined;
        this.pollSub?.unsubscribe();
      },
      error: (err) => (this.error = err.error?.message || 'לא ניתן לבטל')
    });
  }

  get statusMessage(): string {
    if (!this.order) return '';
    switch (this.order.status) {
      case OrderStatus.New:
        return 'ההזמנה שלך בתור וממתינה לטיפול';
      case OrderStatus.InProgress:
        return 'המגנט שלך בטיפול כרגע! כמעט מוכן';
      case OrderStatus.Ready:
        return 'המגנט שלך מוכן ומחכה לך בלוח! 🎉';
      case OrderStatus.Cancelled:
        return 'ההזמנה בוטלה';
      default:
        return '';
    }
  }

  private startPolling(orderId: string): void {
    this.pollSub = interval(5000)
      .pipe(switchMap(() => this.api.getOrderStatus(orderId)))
      .subscribe((status) => {
        if (this.order) {
          this.order = {
            ...this.order,
            status: status.status,
            positionInQueue: status.positionInQueue,
            estimatedWaitMinutes: status.estimatedWaitMinutes
          };
        }
        if (status.status === OrderStatus.Ready || status.status === OrderStatus.Cancelled) {
          this.pollSub?.unsubscribe();
        }
      });
  }
}
