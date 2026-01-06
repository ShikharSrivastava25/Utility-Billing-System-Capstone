import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output, inject, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';
import { DisplayMeterReading } from '../../../../../models/display/display-meter-reading';
import { MeterReadingService } from '../../../../../services/billing-officer/meterReadingService';

@Component({
  selector: 'app-reading-details-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    MatIconModule
  ],
  templateUrl: './reading-details-dialog.html',
  styleUrl: './reading-details-dialog.css'
})
export class ReadingDetailsDialog implements OnInit {
  @Input() reading: DisplayMeterReading | null = null;
  @Output() closed = new EventEmitter<void>();
  @Output() updated = new EventEmitter<void>();

  private fb = inject(FormBuilder);
  private meterReadingService = inject(MeterReadingService);

  editForm: FormGroup;
  isLoading = false;
  isEditMode = false;

  constructor() {
    this.editForm = this.fb.group({
      currentReading: [null, [Validators.required]]
    });
  }

  ngOnInit() {
    if (this.reading) {
      this.editForm.patchValue({
        currentReading: this.reading.currentReading
      });
      this.editForm.get('currentReading')?.setValidators([
        Validators.required,
        Validators.min(this.reading.previousReading)
      ]);
    }
  }

  toggleEdit() {
    this.isEditMode = !this.isEditMode;
  }

  onSubmit() {
    if (this.editForm.valid && this.reading && this.reading.id) {
      this.isLoading = true;
      this.meterReadingService.updateReading(this.reading.id, this.editForm.value.currentReading).subscribe({
        next: () => {
          this.isLoading = false;
          this.updated.emit();
        },
        error: () => {
          this.isLoading = false;
        }
      });
    }
  }

  close() {
    this.closed.emit();
  }
}
