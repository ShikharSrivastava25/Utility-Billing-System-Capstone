import { CommonModule } from '@angular/common';
import { Component, computed, inject, input, output } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { BillingCycle } from '../../../../models/billing/billing-cycle';

@Component({
  selector: 'app-billing-cycle-form',
  imports: [CommonModule, ReactiveFormsModule, MatFormFieldModule, MatInputModule, MatButtonModule, MatSelectModule],
  templateUrl: './billing-cycle-form.html',
  styleUrl: './billing-cycle-form.css',
})
export class BillingCycleForm {
  cycle = input<BillingCycle | null>();
  close = output<void>();
  save = output<BillingCycle | Omit<BillingCycle, 'id'>>();

  private fb = inject(FormBuilder);

  isEditMode = computed(() => !!this.cycle());
  
  cycleForm = this.fb.group({
    name: ['', Validators.required],
    generationDay: [1, [Validators.required, Validators.min(1), Validators.max(28)]],
    dueDateOffset: [15, [Validators.required, Validators.min(0)]],
    gracePeriod: [5, [Validators.required, Validators.min(0)]],
    isActive: [true, Validators.required]
  });

  ngOnInit() {
    if (this.isEditMode() && this.cycle()) {
      this.cycleForm.patchValue(this.cycle()!);
    }
  }

  onSubmit() {
    if (this.cycleForm.invalid) {
      this.cycleForm.markAllAsTouched();
      return;
    }

    const formValue = this.cycleForm.getRawValue();
    const cyclePayload = {
      name: formValue.name!,
      generationDay: formValue.generationDay!,
      dueDateOffset: formValue.dueDateOffset!,
      gracePeriod: formValue.gracePeriod!,
      isActive: formValue.isActive!,
    };

    if (this.isEditMode()) {
      const updatedCycle = { ...this.cycle()!, ...cyclePayload };
      this.save.emit(updatedCycle);
    } else {
      this.save.emit(cyclePayload);
    }
  }

  onClose() {
    this.close.emit();
  }
}
