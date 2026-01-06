import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output, inject, OnInit, OnChanges, SimpleChanges } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MeterReadingService } from '../../../../../services/billing-officer/meterReadingService';
import { ConnectionForReading } from '../../../../../models/billing-officer/connection-for-reading.dto';
import { MeterReadingRequest } from '../../../../../models/billing-officer/meter-reading';

@Component({
  selector: 'app-meter-reading-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './meter-reading-form.html',
  styleUrl: './meter-reading-form.css'
})
export class MeterReadingForm implements OnInit, OnChanges {
  @Input() connection: ConnectionForReading | null = null;
  @Output() saved = new EventEmitter<void>();
  @Output() cancelled = new EventEmitter<void>();

  private fb = inject(FormBuilder);
  private meterReadingService = inject(MeterReadingService);

  readingForm: FormGroup;
  isLoading = false;
  previousReading = 0;
  maxDate = new Date(); // Today's date - no future dates allowed

  constructor() {
    this.readingForm = this.fb.group({
      consumerName: [{ value: '', disabled: true }],
      utilityName: [{ value: '', disabled: true }],
      meterNumber: [{ value: '', disabled: true }],
      previousReading: [{ value: 0, disabled: true }],
      currentReading: [null, [Validators.required, Validators.min(0)]],
      readingDate: [new Date(), Validators.required]
    });
  }

  ngOnInit() {}

  ngOnChanges(changes: SimpleChanges) {
    if (changes['connection'] && this.connection) {
      this.loadConnectionData();
    }
  }

  loadConnectionData() {
    if (!this.connection) return;

    this.isLoading = true;
    const previousReading = this.connection.previousReading ?? 0;
    this.previousReading = previousReading;
    
    this.readingForm.patchValue({
      consumerName: this.connection.consumerName,
      utilityName: this.connection.utilityName,
      meterNumber: this.connection.meterNumber,
      previousReading: previousReading
    });

    this.readingForm.get('currentReading')?.setValidators([
      Validators.required, 
      Validators.min(previousReading)
    ]);
    this.readingForm.get('currentReading')?.updateValueAndValidity();
    this.isLoading = false;
  }

  onSubmit() {
    if (this.readingForm.valid && this.connection) {
      const formValue = this.readingForm.getRawValue();
      const request: MeterReadingRequest = {
        connectionId: this.connection.id,
        currentReading: formValue.currentReading,
        readingDate: formValue.readingDate.toISOString().split('T')[0]
      };

      this.isLoading = true;
      this.meterReadingService.createReading(request).subscribe({
        next: () => {
          this.isLoading = false;
          this.saved.emit();
        },
        error: (err) => {
          this.isLoading = false;
          // Error is handled by service/notification
        }
      });
    }
  }

  onCancel() {
    this.cancelled.emit();
  }
}
