import { CommonModule } from '@angular/common';
import { Component, computed, inject, input, output } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { UtilityType } from '../../../../models/utility';
import { BillingCycle } from '../../../../models/billing/billing-cycle';
import { UtilityFormPayload } from '../../../../models/forms/utility-form-payload';

@Component({
  selector: 'app-utility-form',
  imports: [CommonModule, ReactiveFormsModule, MatFormFieldModule, MatInputModule, MatButtonModule, MatSelectModule],
  templateUrl: './utility-form.html',
  styleUrl: './utility-form.css',
})
export class UtilityForm {
  utility = input<UtilityType | null>();
  billingCycles = input.required<BillingCycle[]>();
  close = output<void>();
  save = output<UtilityType | Omit<UtilityType, 'id'>>();

  private fb = inject(FormBuilder);

  isEditMode = computed(() => !!this.utility());
  
  utilityForm = this.fb.group({
    name: ['', Validators.required],
    description: ['', Validators.required],
    status: ['Enabled', Validators.required],
    billingCycleId: [''],
  });

  ngOnInit() {
    if (this.isEditMode()) {
      this.utilityForm.patchValue(this.utility()!);
    }
  }

  onSubmit() {
    if (this.utilityForm.invalid) {
      return;
    }

    const formValue = this.utilityForm.getRawValue();
    const utilityPayload: UtilityFormPayload = {
      name: formValue.name!,
      description: formValue.description!,
      status: formValue.status as 'Enabled' | 'Disabled',
    };
    
    if (formValue.billingCycleId && formValue.billingCycleId.trim() !== '') {
      utilityPayload.billingCycleId = formValue.billingCycleId;
    }

    if (!this.isEditMode()) {
      this.save.emit(utilityPayload);
    } else {
      const updatedUtility = { ...this.utility()!, ...utilityPayload };
      this.save.emit(updatedUtility);
    }
  }

  onClose() {
    this.close.emit();
  }
}
