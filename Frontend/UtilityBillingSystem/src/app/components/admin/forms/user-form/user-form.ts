import { CommonModule } from '@angular/common';
import { Component, computed, inject, input, output } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { Role, User } from '../../../../models/user';
import { CreateUserPayload } from '../../../../models/forms/user-form-payload';

@Component({
  selector: 'app-user-form',
  imports: [CommonModule, ReactiveFormsModule, MatFormFieldModule, MatInputModule, MatButtonModule, MatSelectModule],
  templateUrl: './user-form.html',
  styleUrl: './user-form.css',
})
export class UserForm {
  user = input<User | null>();
  close = output<void>();
  save = output<User | Omit<User, 'id'>>();

  private fb = inject(FormBuilder);

  isEditMode = computed(() => !!this.user());
  roles = Object.values(Role);
  
  userForm = this.fb.group({
    name: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    role: [Role.Consumer, Validators.required],
    status: ['Active', Validators.required],
    password: [''],
  });

  ngOnInit() {
    if (this.isEditMode()) {
      this.userForm.patchValue(this.user()!);
    } else {
      this.userForm.get('password')?.setValidators(Validators.required);
    }
  }

  onSubmit() {
    if (this.userForm.invalid) {
      return;
    }

    const formValue = this.userForm.getRawValue();

    if (!this.isEditMode()) {
      const createPayload: CreateUserPayload = {
        name: formValue.name!,
        email: formValue.email!,
        password: formValue.password!,
        role: formValue.role!,
        status: formValue.status as 'Active' | 'Inactive',
      };
      this.save.emit(createPayload);
    } else {
      const userPayload = {
        name: formValue.name!,
        email: formValue.email!,
        role: formValue.role!,
        status: formValue.status as 'Active' | 'Inactive',
      };
      const updatedUser = { ...this.user()!, ...userPayload };
      this.save.emit(updatedUser);
    }
  }

  onClose() {
    this.close.emit();
  }
}
