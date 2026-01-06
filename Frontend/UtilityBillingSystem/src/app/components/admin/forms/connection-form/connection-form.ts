import { ChangeDetectionStrategy, Component, computed, inject, input, OnInit, output, signal, effect } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { TariffPlan } from '../../../../models/tariff';
import { Connection } from '../../../../models/connection';
import { User } from '../../../../models/user';
import { UtilityType } from '../../../../models/utility';

@Component({
  selector: 'app-connection-form',
  imports: [CommonModule, ReactiveFormsModule, MatFormFieldModule, MatInputModule, MatButtonModule, MatSelectModule],
  templateUrl: './connection-form.html',
  styleUrl: './connection-form.css',
})
export class ConnectionForm implements OnInit {
  connection = input<Connection | null>();
  consumers = input.required<User[]>();
  utilities = input.required<UtilityType[]>();
  tariffs = input.required<TariffPlan[]>();

  close = output<void>();
  save = output<Connection | Omit<Connection, 'id'>>();

  private fb = inject(FormBuilder);
  
  isEditMode = computed(() => !!this.connection());
  availableTariffs = signal<TariffPlan[]>([]);

  connectionForm = this.fb.group({
    userId: ['', Validators.required],
    utilityTypeId: ['', Validators.required],
    tariffId: ['', Validators.required],
    meterNumber: ['', Validators.required],
    status: ['Active', Validators.required],
  });

  constructor() {
    effect(() => {
      const tariffs = this.tariffs();
      if (tariffs) {
        const currentUtilityId = this.connectionForm.get('utilityTypeId')?.value;
        this.filterTariffs(currentUtilityId);
      }
    });
  }

  ngOnInit() {
    this.connectionForm.get('utilityTypeId')?.valueChanges.subscribe(utilityId => {
      this.filterTariffs(utilityId);
      this.connectionForm.get('tariffId')?.reset('');
    });

    if (this.isEditMode() && this.connection()) {
      const conn = this.connection()!;
      this.filterTariffs(conn.utilityTypeId);
      this.connectionForm.patchValue(conn);
    }
  }

  filterTariffs(utilityId: string | null | undefined) {
    const allTariffs = this.tariffs();
    if (!utilityId || !allTariffs) {
      this.availableTariffs.set([]);
      return;
    }
    const filtered = allTariffs.filter(t => t.utilityTypeId === utilityId);
    this.availableTariffs.set(filtered);
  }

  onSubmit() {
    if (this.connectionForm.invalid) {
      this.connectionForm.markAllAsTouched();
      return;
    }

    const formValue = this.connectionForm.getRawValue();
    const connectionPayload = {
      userId: formValue.userId!,
      utilityTypeId: formValue.utilityTypeId!,
      tariffId: formValue.tariffId!,
      meterNumber: formValue.meterNumber!,
      status: formValue.status as 'Active' | 'Inactive',
    };

    if (this.isEditMode()) {
      const updatedConnection = { ...this.connection()!, ...connectionPayload };
      this.save.emit(updatedConnection);
    } else {
      this.save.emit(connectionPayload);
    }
  }

  onClose() {
    this.close.emit();
  }
}
