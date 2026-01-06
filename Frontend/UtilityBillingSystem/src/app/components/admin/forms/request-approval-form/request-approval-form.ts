import { CommonModule } from '@angular/common';
import { Component, effect, inject, input, output, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { TariffPlan } from '../../../../models/tariff';
import { DisplayRequest } from '../../../../models/display/display-request';

@Component({
  selector: 'app-request-approval-form',
  imports: [CommonModule, ReactiveFormsModule, MatFormFieldModule, MatInputModule, MatButtonModule, MatSelectModule],
  templateUrl: './request-approval-form.html',
  styleUrl: './request-approval-form.css',
})
export class RequestApprovalForm {
  request = input.required<DisplayRequest>();
  tariffs = input.required<TariffPlan[]>();

  close = output<void>();
  save = output<{ tariffId: string, meterNumber: string }>();

  private fb = inject(FormBuilder);
  
  availableTariffs = signal<TariffPlan[]>([]);

  approvalForm = this.fb.nonNullable.group({
    tariffId: ['', Validators.required],
    meterNumber: ['', Validators.required],
  });

  constructor() {
    effect(() => {
      const allTariffs = this.tariffs() ?? [];
      const currentRequest = this.request();

      if (!currentRequest || allTariffs.length === 0) {
        this.availableTariffs.set([]);
        return;
      }

      const filtered = allTariffs.filter(t => t.utilityTypeId === currentRequest.utilityTypeId);
      this.availableTariffs.set(filtered);
    });
  }

  onSubmit() {
    if (this.approvalForm.invalid) {
      this.approvalForm.markAllAsTouched();
      return;
    }
    this.save.emit(this.approvalForm.getRawValue());
  }

  onClose() {
    this.close.emit();
  }
}
