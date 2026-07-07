import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-accessibility-statement',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './accessibility-statement.component.html',
  styleUrl: './accessibility-statement.component.scss'
})
export class AccessibilityStatementComponent {}
