import { Component, computed, inject, input, output } from '@angular/core';
import { TariffPlan } from '../../../../models/tariff';
import { UtilityType } from '../../../../models/utility';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';

@Component({
  selector: 'app-tariff-form',
  imports: [CommonModule, ReactiveFormsModule, MatFormFieldModule, MatInputModule, MatButtonModule, MatSelectModule],
  templateUrl: './tariff-form.html',
  styleUrl: './tariff-form.css',
})
export class TariffForm {
  tariff = input<TariffPlan | null>();
  utilities = input.required<UtilityType[]>();
  close = output<void>();
  save = output<Omit<TariffPlan, 'id' | 'createdAt' | 'isActive'> | TariffPlan>();

  private fb = inject(FormBuilder);

  isEditMode = computed(() => !!this.tariff());
  
  tariffForm = this.fb.nonNullable.group({
    name: ['', Validators.required],
    utilityTypeId: ['', Validators.required],
    baseRate: [0, [Validators.required, Validators.min(0)]],
    fixedCharge: [0, [Validators.required, Validators.min(0)]],
    taxPercentage: [0, [Validators.required, Validators.min(0), Validators.max(100)]],
  });

  ngOnInit() {
    if (this.isEditMode() && this.tariff()) {
      this.tariffForm.patchValue(this.tariff()!);
    }
  }

  onSubmit() {
    if (this.tariffForm.invalid) {
      this.tariffForm.markAllAsTouched();
      return;
    }

    const formValue = this.tariffForm.getRawValue();

    if (this.isEditMode()) {
      const updatedTariff = { ...this.tariff()!, ...formValue };
      this.save.emit(updatedTariff);
    } else {
      const newTariffPayload = {
        name: formValue.name!,
        utilityTypeId: formValue.utilityTypeId!,
        baseRate: formValue.baseRate!,
        fixedCharge: formValue.fixedCharge!,
        taxPercentage: formValue.taxPercentage!,
      };
      this.save.emit(newTariffPayload);
    }
  }

  onClose() {
    this.close.emit();
  }
}
