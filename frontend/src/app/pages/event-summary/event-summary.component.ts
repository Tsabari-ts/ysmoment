import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { ApiService } from '../../core/api.service';
import { EventSummary, OrderResponse, OrderStatus, SIZE_LABELS, STATUS_LABELS } from '../../core/models';

@Component({
  selector: 'app-event-summary',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './event-summary.component.html',
  styleUrl: './event-summary.component.scss'
})
export class EventSummaryComponent implements OnInit {
  eventId = '';
  summary?: EventSummary;
  orders: OrderResponse[] = [];
  statusLabels = STATUS_LABELS;
  sizeLabels = SIZE_LABELS;
  OrderStatus = OrderStatus;

  constructor(private route: ActivatedRoute, private api: ApiService) {}

  ngOnInit(): void {
    this.eventId = this.route.snapshot.paramMap.get('id') || '';
    this.api.getSummary(this.eventId).subscribe((s) => (this.summary = s));
    this.api.getOrders(this.eventId, true).subscribe((orders) => (this.orders = orders));
  }
}
