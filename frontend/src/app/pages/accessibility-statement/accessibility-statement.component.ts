import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ACCESSIBILITY_STATEMENT_HTML } from '../../core/legal-content';

@Component({
  selector: 'app-accessibility-statement',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './accessibility-statement.component.html',
  styleUrl: './accessibility-statement.component.scss'
})
export class AccessibilityStatementComponent {
  content = ACCESSIBILITY_STATEMENT_HTML;
}
